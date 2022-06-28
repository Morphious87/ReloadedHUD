using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;
using SGUI;

namespace ReloadedHUD
{
	public static class MorphUtils
	{
		public static void LogError(string message, Exception e = null)
        {
			if (e != null)
				ETGModConsole.Log($"<color=#FF0000>{message}\n{e}</color>");
			else
				ETGModConsole.Log($"<color=#FF0000>{message}</color>");
		}

		public static string FormatWithSpaces(this string s)
        {
			return Regex.Replace(s, "([a-z])([A-Z])", "$1 $2");
		}

		public static string RainbowizeString(string orig)
        {
			StringBuilder sb = new StringBuilder();
			for (var i = 0; i < orig.Length; i++)
            {
				var c = orig[i];
				var hue = Mathf.InverseLerp(0, orig.Length, i);
				var col = Color.HSVToRGB(hue, 1, 1);
				var colString = ColorUtility.ToHtmlStringRGB(col);
				var charString = $"<color=#{colString}>{c}</color>";
				sb.Append(charString);
            }
			return sb.ToString();
        }

        public static void LogRainbow(string text)
        {
            var container = new SGroup() { Size = new Vector2(20000, 32), AutoLayoutPadding = 0 };
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == ' ')
                {
                    container.Children.Add(new SRect(Color.clear) { Size = Vector2.one * 10 });
                }
                else
                {
                    var hue = Mathf.InverseLerp(0, text.Length, i);
                    var col = Color.HSVToRGB(hue, 1, 1);
                    var label = new SLabel(c.ToString()) { Foreground = col, With = { new SRainbowModifier() } };
                    container.Children.Add(label);
                }
            }
            container.AutoLayout = (SGroup g) => new Action<int, SElement>(g.AutoLayoutHorizontal);
            ETGModConsole.Instance.GUI[0].Children.Add(container);
        }
    }
}


