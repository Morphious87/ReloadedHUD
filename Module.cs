using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using SGUI;
using BepInEx;

namespace ReloadedHUD
{
    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Module : BaseUnityPlugin
    {
        public const string GUID = "morphious86.etg.reloadedhud";
        public const string NAME = "Reloaded HUD";
        public const string VERSION = "1.1.1";

        public void Start()
        {
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
        }

        public void GMStart(GameManager g)
        {
            try
            {
                HooksManager.Init();
                HooksManager.PostInit();

                ConsoleCommandsManager.Init();
                SettingsManager.LoadSettings();
                ETGModMainBehaviour.Instance.gameObject.AddComponent<RHController>();

                MorphUtils.LogRainbow($"{NAME} v{VERSION} started successfully.");
            }
            catch (Exception e)
            {
                MorphUtils.LogError($"{NAME} v{VERSION} failed to initialize!", e);
            }
        }
    }
}
