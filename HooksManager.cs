using System;
using System.Reflection;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Linq;
using System.Collections.Generic;

namespace ReloadedHUD
{
    public static class HooksManager
    {
        static RHController hud => RHController.Instance;
        
        public static void Init()
        {
            AddHook(typeof(GameManager), "Pause");
            AddHook(typeof(GameManager), "Unpause");
            AddHook(typeof(PauseMenuController), "ToggleVisibility", "TogglePauseMenuVisibility");
            AddHook(typeof(GameUIRoot), "HideCoreUI");
            AddHook(typeof(GameUIRoot), "ShowCoreUI");
            AddHook(typeof(PlayerController), "Die");
        }

        public static void PostInit()
        {

        }

        static void Die(Action<PlayerController, Vector2> orig, PlayerController self, Vector2 finalDamageDirection)
        {
            hud.DoFade(0);
            orig(self, finalDamageDirection);
        }

        static void Unpause(Action<GameManager> orig, GameManager self)
        {
            orig(self);
            hud.SwitchEditMode(false);
        }

        static void Pause(Action<GameManager> orig, GameManager self)
        {
            hud.SwitchEditMode(true);
            orig(self);
        }

        static void TogglePauseMenuVisibility(Action<PauseMenuController, bool> orig, PauseMenuController self, bool visible)
        {
            hud.DoPausedFade(visible ? 1 : 0);
            orig(self, visible);
        }

        static void HideCoreUI(Action<GameUIRoot, string> orig, GameUIRoot self, string reason)
        {
            if (!hud.editing)
                hud.DoFade(0);

            orig(self, reason);
        }

        static void ShowCoreUI(Action<GameUIRoot, string> orig, GameUIRoot self, string reason)
        {
            if (!hud.editing)
                hud.DoFade(1);

            orig(self, reason);
        }

        public static Hook AddHook(Type type, string sourceMethodName, string hookMethodName = null)
        {
            if (hookMethodName == null) hookMethodName = sourceMethodName;
            return new Hook(
                type.GetMethod(sourceMethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                typeof(HooksManager).GetMethod(hookMethodName, BindingFlags.NonPublic | BindingFlags.Static)
            );
        }

        public static T GetTypedValue<T>(this FieldInfo This, object instance) { return (T)This.GetValue(instance); }
        public static T ReflectGetField<T>(Type classType, string fieldName, object o = null)
        {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            return (T)field.GetValue(o);
        }
    }
}
