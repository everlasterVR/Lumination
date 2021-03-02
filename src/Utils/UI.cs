using UnityEngine;

namespace Illumination
{
    public static class UI
    {
        public static Color defaultOnColor = new Color(0.0f, 1f, 0.0f, 0.5f);
        public static Color defaultPluginBgColor = new Color32(193, 168, 203, 255);

        public static Color black = UnityEngine.Color.black;
        public static Color lightGray = new Color(0.75f, 0.75f, 0.75f);
        public static Color lightGreen = new Color(0.75f, 1f, 0.75f);
        public static Color pink = new Color(1f, 0.75f, 0.75f);
        public static Color turquoise = new Color(0.5f, 1f, 1f);
        public static Color white = UnityEngine.Color.white;

        public static string LightButtonLabel(string uid, bool selected = false)
        {
            return Bold(Color($"{(selected ? "■" : "  ")}    {uid}", selected ? turquoise : lightGray));
        }

        public static string SelectTargetButtonLabel(string targetString)
        {
            return $"Select target to aim at\n{Italic(Size(targetString, 26))}";
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
    }
}
