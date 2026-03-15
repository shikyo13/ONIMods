namespace DuplicantStatusBar.Localization
{
    public static class STRINGS
    {
        public static class DUPLICANTSTATUSBAR
        {
            public static class UI
            {
                public static LocString HEADER = "Dupes";
                public static LocString TOOLTIP_TASK = "Task:";
                public static LocString TOOLTIP_IDLE = "Idle";
                public static LocString TOOLTIP_STRESS = "Stress:";
                public static LocString TOOLTIP_HEALTH = "Health:";
                public static LocString TOOLTIP_BREATH = "Breath:";
                public static LocString TOOLTIP_BODYTEMP = "Body Temp:";
                public static LocString TOOLTIP_BLADDER = "Bladder:";
            }

            public static class ALERTS
            {
                public static LocString SUFFOCATING = "Suffocating";
                public static LocString LOWHEALTH = "Low Health";
                public static LocString SCALDING = "Scalding";
                public static LocString HYPOTHERMIA = "Hypothermia";
                public static LocString IRRADIATED = "Irradiated";
                public static LocString STARVING = "Starving";
                public static LocString OVERSTRESSED = "Overstressed";
                public static LocString BLADDERURGENT = "Bladder Urgent";
                public static LocString DISEASED = "Diseased";
                public static LocString OVERJOYED = "Overjoyed";
                public static LocString STUCK = "Stuck";
                public static LocString IDLE = "Idle";
                public static LocString INCAPACITATED = "Incapacitated";
            }

            public static class OPTIONS
            {
                public static class CATEGORIES
                {
                    public static LocString GENERAL = "General";
                    public static LocString APPEARANCE = "Appearance";
                    public static LocString STRESSTIERS = "Stress Tiers";
                    public static LocString ALERTS = "Alerts";
                }

                public static class SORTMODE
                {
                    public static LocString STRESSDESCENDING = "Stress (highest first)";
                    public static LocString ALPHABETICAL = "Alphabetical";
                    public static LocString ROLE = "Job Role";
                }

                public static class VIEWMODE
                {
                    public static LocString PORTRAITS = "Portraits";
                    public static LocString INITIALS = "Initials Only";
                }

                public static class SORTORDER
                {
                    public static LocString NAME = "Sort Order";
                    public static LocString DESC = "How to sort duplicants in the bar.";
                }

                public static class PORTRAITSIZE
                {
                    public static LocString NAME = "Portrait Size";
                    public static LocString DESC = "Size of each portrait in pixels.";
                }

                public static class MAXDUPESPERROW
                {
                    public static LocString NAME = "Max Dupes Per Row";
                    public static LocString DESC = "Maximum portraits per row (0 = auto-fit to screen width).";
                }

                public static class MAXBARWIDTH
                {
                    public static LocString NAME = "Max Bar Width (%)";
                    public static LocString DESC = "Maximum bar width as percentage of screen (triggers row wrapping).";
                }

                public static class MAXROWS
                {
                    public static LocString NAME = "Max Rows";
                    public static LocString DESC = "Maximum visible rows before scrolling (0 = unlimited).";
                }

                public static class BAROPACITY
                {
                    public static LocString NAME = "Bar Opacity (%)";
                    public static LocString DESC = "Opacity of the status bar background.";
                }

                public static class DISPLAYMODE
                {
                    public static LocString NAME = "Display Mode";
                    public static LocString DESC = "Show portraits or initial letters.";
                }

                public static class FACEEXPRESSIONS
                {
                    public static LocString NAME = "Face Expressions";
                    public static LocString DESC = "Show dynamic facial expressions based on dupe status.";
                }

                public static class CALMTHRESHOLD
                {
                    public static LocString NAME = "Calm Threshold";
                    public static LocString DESC = "Stress below this % is Calm (green).";
                }

                public static class MILDTHRESHOLD
                {
                    public static LocString NAME = "Mild Threshold";
                    public static LocString DESC = "Stress below this % is Mild (lime).";
                }

                public static class STRESSEDTHRESHOLD
                {
                    public static LocString NAME = "Stressed Threshold";
                    public static LocString DESC = "Stress below this % is Stressed (yellow).";
                }

                public static class HIGHTHRESHOLD
                {
                    public static LocString NAME = "High Threshold";
                    public static LocString DESC = "Stress below this % is High (orange).";
                }

                public static class ALERTSUFFOCATING
                {
                    public static LocString NAME = "Suffocating Alert";
                    public static LocString DESC = "Show badge when dupe is suffocating.";
                }

                public static class ALERTLOWHP
                {
                    public static LocString NAME = "Low HP Alert";
                    public static LocString DESC = "Show badge when dupe health is low.";
                }

                public static class ALERTSCALDING
                {
                    public static LocString NAME = "Scalding Alert";
                    public static LocString DESC = "Show badge when dupe is overheating.";
                }

                public static class ALERTHYPOTHERMIA
                {
                    public static LocString NAME = "Hypothermia Alert";
                    public static LocString DESC = "Show badge when dupe is freezing.";
                }

                public static class ALERTDISEASED
                {
                    public static LocString NAME = "Disease Alert";
                    public static LocString DESC = "Show badge when dupe is sick.";
                }

                public static class ALERTOVERSTRESSED
                {
                    public static LocString NAME = "Overstressed Alert";
                    public static LocString DESC = "Show badge at critical stress.";
                }

                public static class ALERTOVERJOYED
                {
                    public static LocString NAME = "Overjoyed Alert";
                    public static LocString DESC = "Show rainbow border and tooltip during joy reactions.";
                }

                public static class ALERTIRRADIATED
                {
                    public static LocString NAME = "Irradiated Alert";
                    public static LocString DESC = "Show badge when dupe has radiation sickness.";
                }

                public static class ALERTSTARVING
                {
                    public static LocString NAME = "Starving Alert";
                    public static LocString DESC = "Show badge when dupe is starving.";
                }

                public static class ALERTBLADDER
                {
                    public static LocString NAME = "Bladder Alert";
                    public static LocString DESC = "Show badge when dupe urgently needs a bathroom.";
                }

                public static class ALERTSTUCK
                {
                    public static LocString NAME = "Stuck Alert";
                    public static LocString DESC = "Show badge when dupe cannot reach the printing pod.";
                }

                public static class ALERTIDLE
                {
                    public static LocString NAME = "Idle Alert";
                    public static LocString DESC = "Show badge when dupe has no task for an extended period.";
                }

                public static class ALERTINCAPACITATED
                {
                    public static LocString NAME = "Incapacitated Alert";
                    public static LocString DESC = "Show badge when dupe is incapacitated (bleeding out).";
                }
            }
        }
    }
}
