using UnityEngine;

namespace Lumination
{
    public static class UI
    {
        public static Color defaultOnColor = new Color(0.0f, 1f, 0.0f, 0.5f);
        public static Color defaultPluginBgColor = new Color32(193, 168, 203, 255);

        public static Color black = UnityEngine.Color.black;
        public static Color gray = new Color(0.4f, 0.4f, 0.4f);
        public static Color lightGray = new Color(0.75f, 0.75f, 0.75f);
        public static Color lightGreen = new Color(0.75f, 1f, 0.75f);
        public static Color lightPink = new Color(1f, 0.925f, 0.925f);
        public static Color offGrayRed = new Color(0.45f, 0.4f, 0.4f);
        public static Color violet = new Color(1f, 0.5f, 1f);
        public static Color offGrayViolet = new Color(0.72f, 0.68f, 0.72f);
        public static Color pink = new Color(1f, 0.75f, 0.75f);
        public static Color turquoise = new Color(0.5f, 1f, 1f);
        public static Color white = UnityEngine.Color.white;

        public static string TitleTextStyle(string title)
        {
            return Size("\n", 24) + Bold(Size(title, 36));
        }

        public static string FormatUsage(string title, string subtitle)
        {
            return Size("\n", 8) + Bold(Size(title, 32)) + "\n\n" + Size(subtitle, 28);
        }

        public static string LightButtonLabel(string uid, bool on, bool selected = false)
        {
            string onOrOff = on ? $"{Size("  ", 26)}ON" : "OFF";
            string label = $"  {onOrOff}   {Truncate(uid, 28)}";
            return Bold(Color(label, selected ? violet : offGrayViolet));
        }

        public static string SelectTargetButtonLabel(string targetString)
        {
            if(string.IsNullOrEmpty(targetString))
            {
                return Size("Select Target", 32);
            }

            return Size("Selected Target", 32) + Italic(Size("\n" + targetString, 28));
        }

        public static string LineBreak()
        {
            return "\n" + Size("\n", 12);
        }

        public static string Color(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }

        public static string Bold(string text)
        {
            return $"<b>{text}</b>";
        }

        public static string Italic(string text)
        {
            return $"<i>{text}</i>";
        }

        public static string Size(string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }

        public static string Capitalize(string text)
        {
            if(string.IsNullOrEmpty(text))
            {
                return "";
            }

            return char.ToUpper((text[0])) + (text.Length > 1 ? text.Substring(1) : "");
        }

        private static string Truncate(string value, int maxLength)
        {
            if(string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Length <= maxLength ? value : $"{value.Substring(0, maxLength-3)}...";
        }
    }
}
