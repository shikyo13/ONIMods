using System.Collections.Generic;
using UnityEngine;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.UI
{
    public enum ExpressionType
    {
        Neutral, Happy, Angry, Suffocate, Cold, Hot,
        Hungry, Tired, Sick, Sparkle, Uncomfortable, Dead, Productive
    }

    /// <summary>
    /// Maps dupe alert/stress state to facial expression frame indices.
    /// Frame data is discovered at runtime from head_master_swap_kanim —
    /// each face animation defines which eye/mouth accessory frames to use.
    /// </summary>
    public static class ExpressionResolver
    {
        public struct ExpressionFrames
        {
            public int EyeFrame;
            public int MouthFrame;
            public float EyeTransX, EyeTransY;   // m02, m12 from snapto_eyes
            public float MouthTransX, MouthTransY; // m02, m12 from snapto_mouth
        }

        private static readonly KAnimHashedString SNAPTO_EYES = "snapto_eyes";
        private static readonly KAnimHashedString SNAPTO_MOUTH = "snapto_mouth";

        private static Dictionary<HashedString, ExpressionFrames> faceFrames;
        private static int blinkEyeFrame = -1;

        /// <summary>
        /// Highest-priority alert determines expression; falls back to stress tier.
        /// </summary>
        public static ExpressionType Resolve(AlertType alert, StressTier tier)
        {
            switch (alert)
            {
                case AlertType.Incapacitated: return ExpressionType.Dead;
                case AlertType.Suffocating:   return ExpressionType.Suffocate;
                case AlertType.LowHP:         return ExpressionType.Angry;
                case AlertType.Scalding:      return ExpressionType.Hot;
                case AlertType.Hypothermia:   return ExpressionType.Cold;
                case AlertType.Overstressed:  return ExpressionType.Angry;
                case AlertType.Diseased:      return ExpressionType.Sick;
                case AlertType.Starving:      return ExpressionType.Hungry;
                case AlertType.Overjoyed:     return ExpressionType.Sparkle;
                case AlertType.Idle:          return ExpressionType.Tired;
                case AlertType.Stuck:         return ExpressionType.Uncomfortable;
                case AlertType.Irradiated:    return ExpressionType.Sick;
                case AlertType.BladderUrgent: return ExpressionType.Uncomfortable;
                default:
                    switch (tier)
                    {
                        case StressTier.Calm:     return ExpressionType.Happy;
                        case StressTier.Mild:     return ExpressionType.Neutral;
                        case StressTier.Stressed: return ExpressionType.Uncomfortable;
                        case StressTier.High:
                        case StressTier.Critical: return ExpressionType.Angry;
                        default: return ExpressionType.Neutral;
                    }
            }
        }

        public static ExpressionFrames GetFrames(ExpressionType expr)
        {
            EnsureDiscovered();
            var face = GetFace(expr);
            if (face != null && faceFrames != null
                && faceFrames.TryGetValue(face.hash, out var frames))
                return frames;
            return new ExpressionFrames { EyeFrame = 0, MouthFrame = 0 };
        }

        /// <summary>
        /// Returns the closed-eye frame index for blink animation,
        /// discovered from the Sleep face. Returns -1 if undiscovered.
        /// </summary>
        public static int GetBlinkFrame()
        {
            EnsureDiscovered();
            return blinkEyeFrame;
        }

        private static Face GetFace(ExpressionType expr)
        {
            var faces = Db.Get()?.Faces;
            if (faces == null) return null;
            switch (expr)
            {
                case ExpressionType.Neutral:       return faces.Neutral;
                case ExpressionType.Happy:         return faces.Happy;
                case ExpressionType.Angry:         return faces.Angry;
                case ExpressionType.Suffocate:     return faces.Suffocate;
                case ExpressionType.Cold:          return faces.Cold;
                case ExpressionType.Hot:           return faces.Hot;
                case ExpressionType.Hungry:        return faces.Hungry;
                case ExpressionType.Tired:         return faces.Tired;
                case ExpressionType.Sick:          return faces.Sick;
                case ExpressionType.Sparkle:       return faces.Sparkle;
                case ExpressionType.Uncomfortable: return faces.Uncomfortable;
                case ExpressionType.Dead:          return faces.Dead;
                case ExpressionType.Productive:    return faces.Productive;
                default:                           return faces.Neutral;
            }
        }

        /// <summary>
        /// Parses head_master_swap_kanim to discover which eye/mouth accessory
        /// frame each face animation references. Called lazily on first use.
        /// </summary>
        private static void EnsureDiscovered()
        {
            if (faceFrames != null) return;

            KAnimFile shapesFile;
            try { shapesFile = Assets.GetAnim("head_master_swap_kanim"); }
            catch { return; }
            if (shapesFile == null) return;

            var data = shapesFile.GetData();
            if (data == null) return;

            KBatchGroupData batchData;
            try { batchData = KAnimBatchManager.Instance()?.GetBatchGroupData(data.animBatchTag); }
            catch { return; }
            if (batchData == null) return;

            faceFrames = new Dictionary<HashedString, ExpressionFrames>();

            for (int i = 0; i < data.animCount; i++)
            {
                var anim = data.GetAnim(i);
                if (!anim.TryGetFrame(data.build.batchTag, 0, out var frame))
                    continue;

                int eyeFrame = -1, mouthFrame = -1;
                float eyeTransX = 0, eyeTransY = 0;
                float mouthTransX = 0, mouthTransY = 0;
                for (int j = 0; j < frame.numElements; j++)
                {
                    var elem = batchData.GetFrameElement(frame.firstElementIdx + j);
                    if (eyeFrame < 0 && elem.symbol == SNAPTO_EYES)
                    {
                        eyeFrame = elem.frame;
                        eyeTransX = elem.transform.m02;
                        eyeTransY = elem.transform.m12;
                    }
                    else if (mouthFrame < 0 && elem.symbol == SNAPTO_MOUTH)
                    {
                        mouthFrame = elem.frame;
                        mouthTransX = elem.transform.m02;
                        mouthTransY = elem.transform.m12;
                    }
                    if (eyeFrame >= 0 && mouthFrame >= 0) break;
                }
                if (eyeFrame < 0) eyeFrame = 0;
                if (mouthFrame < 0) mouthFrame = 0;

                faceFrames[anim.hash] = new ExpressionFrames
                {
                    EyeFrame = eyeFrame,
                    MouthFrame = mouthFrame,
                    EyeTransX = eyeTransX,
                    EyeTransY = eyeTransY,
                    MouthTransX = mouthTransX,
                    MouthTransY = mouthTransY
                };
            }

            // Discover blink frame from Sleep face (closed eyes)
            var faces = Db.Get()?.Faces;
            if (faces != null)
            {
                if (faces.Sleep != null
                    && faceFrames.TryGetValue(faces.Sleep.hash, out var sleepFrames))
                    blinkEyeFrame = sleepFrames.EyeFrame;
                else if (faces.Dead != null
                    && faceFrames.TryGetValue(faces.Dead.hash, out var deadFrames))
                    blinkEyeFrame = deadFrames.EyeFrame;
            }
        }

        public static void ClearCache()
        {
            faceFrames = null;
            blinkEyeFrame = -1;
        }
    }
}
