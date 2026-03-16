using System.Reflection;
using UnityEngine;

namespace DuplicantStatusBar.Core
{
    /// <summary>
    /// Reflection wrappers for ImageConversion extension methods (LoadImage, EncodeToPNG).
    /// Unity 6's ImageConversionModule.dll references netstandard 2.1 which causes
    /// CS1705 (unsuppressible) when compiling against net471. Calling via reflection
    /// avoids the assembly reference entirely — works on both Unity 2020 and Unity 6.
    /// </summary>
    internal static class ImageConversionHelper
    {
        private static MethodInfo _loadImage;
        private static MethodInfo _encodeToPNG;
        private static bool _searched;

        private static void EnsureResolved()
        {
            if (_searched) return;
            _searched = true;

            var type = System.Type.GetType(
                "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
            if (type == null) return;

            _loadImage = type.GetMethod("LoadImage",
                new System.Type[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });
            _encodeToPNG = type.GetMethod("EncodeToPNG",
                new System.Type[] { typeof(Texture2D) });
        }

        internal static bool LoadImage(Texture2D tex, byte[] data)
        {
            EnsureResolved();
            if (_loadImage != null)
                return (bool)_loadImage.Invoke(null, new object[] { tex, data, false });
            return false;
        }

        internal static byte[] EncodeToPNG(Texture2D tex)
        {
            EnsureResolved();
            if (_encodeToPNG != null)
                return (byte[])_encodeToPNG.Invoke(null, new object[] { tex });
            return null;
        }
    }
}
