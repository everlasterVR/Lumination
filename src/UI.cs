using UnityEngine;

namespace Illumination
{
    public static class UI
    {
        public static Color black = Color.black;
        public static Color blue = new Color(0.33f, 0.33f, 1f);
        public static Color lightGray = new Color(0.75f, 0.75f, 0.75f);
        public static Color white = Color.white;

        public static string ColorText(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }

        public static string BoldText(string text)
        {
            return $"<b>{text}</b>";
        }

        public static string ItalicText(string text)
        {
            return $"<i>{text}</i>";
        }
    }
}
