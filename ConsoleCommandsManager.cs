using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReloadedHUD
{
    public static class ConsoleCommandsManager
    {
        static RHController hud => RHController.Instance;
        const string prefix = "reloadedhud";

        public static void Init()
        {
            ETGModConsole.Commands.AddGroup(prefix, (args) =>
            {
                MorphUtils.LogRainbow($"{Module.NAME} is being made by Morphious86#6617 on Discord!");
            });

            AddCommand("combat_opacity", (args) => TryParseFloat(args[0], (opacity) =>
            {
                ETGModConsole.Log($"Setting HUD opacity while in combat to {opacity}");
                hud.alphaDuringCombat = opacity;
                hud.SaveSettings();
            }));

            AddCommand("safe_opacity", (args) => TryParseFloat(args[0], (opacity) =>
            {
                ETGModConsole.Log($"Setting HUD opacity while safe to {opacity}");
                hud.alphaWhenSafe = opacity;
                hud.SaveSettings();
            }));
        }

        static ConsoleCommandGroup AddCommand(string name, Action<string[]> action)
        {
            return ETGModConsole.Commands.GetGroup(prefix).AddUnit(name, action);
        }

        static void TryParseFloat(string arg, Action<float> action)
        {
            if (float.TryParse(arg, out var f))
                action(f);
            else
                MorphUtils.LogError($"\"{arg}\" is not a proper value!");
        }
    }
}
