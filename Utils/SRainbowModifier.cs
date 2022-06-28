using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

namespace SGUI
{
    public class SRainbowModifier : SModifier
    {
        float startTime;
        float baseHue;

        public override void Init()
        {
            startTime = Time.realtimeSinceStartup;
            var col = Elem.Foreground;
            Color.RGBToHSV(col, out baseHue, out _, out _);
        }

        public override void Update()
        {
            var time = Time.realtimeSinceStartup - startTime;
            var hue = Mathf.Abs(time * -0.5f + baseHue) % 1f;

            var freq = 8;
            var amp = 2;
            var speed = 3;
            var y = Mathf.Sin(time * speed + baseHue * freq) * amp;

            Elem.Foreground = Color.HSVToRGB(hue, 1, 1);
            Elem.Position = Elem.Position.WithY(y);
        }
    }
}
