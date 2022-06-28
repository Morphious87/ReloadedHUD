using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace ReloadedHUD
{
    public static class SettingsManager
    {
        static string ResourcesDirectory = Path.Combine(ETGMod.ResourcesDirectory, "reloadedhud");
        static string ConfigPath = Path.Combine(ResourcesDirectory, "config.json");

        public static Settings settings;
        public class Settings
        {
            public bool enabled = false;
            public float opacityDuringCombat = 0.5f;
            public float opacityWhenSafe = 1f;
            public List<string> shownStats = new List<string>()
            {
                "Speed",
                "Curse",
                "Coolness",
                "Damage",
                "FireDelay",
                "ReloadTime",
                "ChargeTime",
                "Spread",
                "Range",
                "DPS"
            };
    }

        public static void SaveSettings()
        {
            if (!File.Exists(ConfigPath))
                Directory.CreateDirectory(ResourcesDirectory);

            try
            {
                File.WriteAllText(ConfigPath, JsonUtility.ToJson(settings, true));
            }
            catch (Exception e)
            {
                MorphUtils.LogError("Failed to save ReloadedHUD config!", e);
            }
        }

        public static void LoadSettings()
        {
            bool success = false;

            if (File.Exists(ConfigPath))
            {
                string text = File.ReadAllText(ConfigPath);
                if (text != null)
                {
                    try
                    {
                        settings = JsonUtility.FromJson<Settings>(text);
                    }
                    catch (Exception e)
                    {
                        MorphUtils.LogError("Failed to parse ReloadedHUD config!", e);
                    }
                    finally
                    {
                        success = true;
                    }
                }
                else
                {
                    MorphUtils.LogError("ReloadedHUD config is empty!");
                }
            }

            if (!success)
                settings = new Settings();
        }
    }
}
