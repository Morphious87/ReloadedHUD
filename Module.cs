using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using SGUI;

namespace ReloadedHUD
{
    public class Module : ETGModule
    {
        public static readonly string MOD_NAME = "Reloaded HUD";
        public static readonly string VERSION = "1.0.1";

        public override void Init()
        {
            
        }

        public override void Start()
        {
            try
            {
                HooksManager.Init();

                ConsoleCommandsManager.Init();
                SettingsManager.LoadSettings();
                ETGModMainBehaviour.Instance.gameObject.AddComponent<RHController>();
            }
            catch (Exception e)
            {
                MorphUtils.LogError("Reloaded HUD failed to initialize!", e);
            }
            finally
            {
                MorphUtils.LogRainbow($"{MOD_NAME} v{VERSION} started successfully.");
            }
            HooksManager.PostInit();
        }

        public override void Exit()
        {

        }
    }
}
