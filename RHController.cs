using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ETGGUI;
using SGUI;
using ItemAPI;
using System.Collections;

namespace ReloadedHUD
{
    public enum StatType { Speed, Curse, Coolness, Damage, FireDelay, BulletSpeed, ReloadTime, Projectiles, ChargeTime, Range, DPS, ClipSize, Spread, AmmoCapacity, BulletSize, Knockback };

    public class RHController : MonoBehaviour
    {
        SGroup statsContainer;

        List<StatType> weaponStats = new List<StatType>() { StatType.Damage, StatType.FireDelay, StatType.ChargeTime, StatType.ReloadTime, StatType.AmmoCapacity, StatType.ClipSize, StatType.Projectiles, StatType.Spread, StatType.Range, StatType.BulletSize, StatType.BulletSpeed, StatType.Knockback, StatType.DPS };
        List<StatType> playerStats = new List<StatType>() { StatType.Speed, StatType.Curse, StatType.Coolness };

        public enum StatUnit { None, Whole, Multiplier, Seconds, Degrees }

        public bool editing;
        float alphaMultiplier = 1f;
        float pausedAlphaMultiplier = 1f;
        bool init;

        // in-game fade animation
        float fadeStartTime;
        float fadeTo;
        float fadeFrom;
        float fadeDur;

        // pause menu fade animation
        float pausedFadeStartTime;
        float pausedFadeTo;
        float pausedFadeFrom;
        float pausedFadeDur;

        // config
        public float alphaDuringCombat;
        public float alphaWhenSafe;

        public Texture2D tex_Visibility_On = ResourceExtractor.GetTextureFromResource("ReloadedHUD/Resources/Visibility_On.png");
        public Texture2D tex_Visibility_Off = ResourceExtractor.GetTextureFromResource("ReloadedHUD/Resources/Visibility_Off.png");
        public Texture2D tex_Infinity = ResourceExtractor.GetTextureFromResource("ReloadedHUD/Resources/Infinity.png");

        Dictionary<StatType, Stat> stats = new Dictionary<StatType, Stat>();
        class Stat
        {
            public StatType type;
            public float value;
            public float multiplier;
            public float previousValue;
            public float previousMultiplier;
            public bool visible;
            public StatUnit unit;

            public float baseAlpha; // currently always 1

            public bool lessIsBetter;
            public bool announceBaseChange; // either stat has no multiplier, or is already a multiplier

            public SGroup container; // houses layout group & button
            public SGroup layout; // icon + value + multiplier + difference
            public SButton button; // for toggling visibility
            public SImage icon;
            public SImage infinity;
            public SImage visibilityIcon;
            public SLabel label; // base stat
            public SLabel label2; // stat's multiplier
            public SLabel labelChange; // difference

            float multiplierBeforeChanges;
            float valueBeforeChanges;
            bool doingChange;

            float changeAppearTime;

            public Stat(StatType type)
            {
                this.type = type;
                var iconTexture = ResourceExtractor.GetTextureFromResource("ReloadedHUD/Resources/StatIcons/" + type.ToString() + ".png");

                icon = new SImage(iconTexture);
                icon.UpdateStyle();

                visibilityIcon = new SImage(RHController.Instance.tex_Visibility_On) { Position = new Vector2(12, -8) };
                icon.UpdateStyle();
                icon.Children.Add(visibilityIcon);

                label = new SLabel(type.ToString());
                infinity = new SImage(RHController.Instance.tex_Infinity);
                label2 = new SLabel() { Foreground = Color.gray };
                labelChange = new SLabel();

                container = new SGroup() { Background = Color.clear, Size = new Vector2(300, icon.Size.y + 4) };

                layout = new SGroup() { Background = Color.clear, Size = container.Size, AutoLayoutVerticalStretch = false };
                layout.Children.Add(icon);
                layout.Children.Add(new SRect(Color.clear) { Size = Vector2.zero.WithX(8) });
                layout.Children.Add(infinity);
                layout.Children.Add(label);
                layout.Children.Add(label2);
                layout.Children.Add(labelChange);
                layout.AutoLayout = (SGroup g) => new Action<int, SElement>(g.AutoLayoutHorizontal);
                container.Children.Add(layout);

                button = new SButtonRect() { Background = Color.clear, Size = container.Size, OnClick = (s) => OnPressed()};
                container.Children.Add(button);
                
                multiplier = 1f;
                baseAlpha = 1f;
                visible = true;
            }

            string GetSuffix()
            {
                if (unit == StatUnit.Seconds)
                    return "s";
                else if (unit == StatUnit.Multiplier)
                    return "x";
                else if (unit == StatUnit.Degrees)
                    return "°";
                else
                    return "";
            }

            void OnPressed()
            {
                var hud = RHController.Instance;
                if (!hud.editing || hud.alphaMultiplier <= 0)
                    return;

                AkSoundEngine.PostEvent("Play_UI_menu_back_01", hud.gameObject);
                ToggleVisibility();
            }

            public void ToggleVisibility()
            {
                visible = !visible;
            }

            public void Update(bool dontAnnounceChange = false)
            {
                var hud = RHController.Instance;
                container.Visible = visible || hud.editing;
                visibilityIcon.Visible = hud.editing;

                if (dontAnnounceChange)
                {
                    previousMultiplier = multiplier;
                    previousValue = value;
                }

                if (!visible && !hud.editing)
                    return;

                string text;
                float changeAlpha;
                if (hud.editing)
                {
                    changeAlpha = 0f;
                    text = type.ToString().FormatWithSpaces();
                    visibilityIcon.Texture = visible ? hud.tex_Visibility_On : hud.tex_Visibility_Off;
                    label.Visible = true;
                    label2.Visible = false;
                    infinity.Visible = false;
                }
                else
                {
                    // show base value
                    bool isInfinite = value == float.MaxValue;
                    label.Visible = !isInfinite;
                    infinity.Visible = isInfinite;
                    if (!announceBaseChange && value == 0f)
                    {
                        text = "-";
                    }
                    else
                    {
                        if (unit == StatUnit.Whole)
                            text = value.ToString("0");
                        else
                            text = value.ToString("0.0") + GetSuffix();
                    }

                    // show multiplier
                    if (!Mathf.Approximately(multiplier, 1f) && !isInfinite)
                    {
                        label2.Visible = true;
                        if (unit == StatUnit.Whole)
                            label2.Text = $" ({(value * multiplier).ToString("0")})";
                        else
                            label2.Text = $" ({multiplier.ToString("0.00")}x)";
                    }
                    else
                        label2.Visible = false;

                    // check for change
                    if (!announceBaseChange && multiplier != previousMultiplier)
                    {
                        ShowDifference(multiplier, multiplierBeforeChanges, false);
                        previousMultiplier = multiplier;
                    }
                    else if (announceBaseChange && value != previousValue)
                    {
                        ShowDifference(value, valueBeforeChanges, true);
                        previousValue = value;
                    }

                    // fade animation for ticker
                    float timePassedSinceChange = Time.time - changeAppearTime;
                    float changeAppearDur = 0.25f;
                    float changeAppearEnd = 2f;
                    if (timePassedSinceChange <= changeAppearDur && !doingChange)
                        changeAlpha = Mathf.InverseLerp(0f, changeAppearDur, timePassedSinceChange);
                    else if (timePassedSinceChange <= changeAppearEnd + changeAppearDur)
                        changeAlpha = 1 - Mathf.InverseLerp(changeAppearEnd, changeAppearEnd + changeAppearDur, timePassedSinceChange);
                    else
                    {
                        valueBeforeChanges = value;
                        multiplierBeforeChanges = multiplier;
                        changeAlpha = 0;
                        doingChange = false;
                    }
                }

                // apply everything
                var alpha = hud.editing ? hud.pausedAlphaMultiplier : hud.alphaMultiplier;
                labelChange.Foreground = labelChange.Foreground.WithAlpha(baseAlpha * alpha * changeAlpha);
                label.Foreground = (visible ? Color.white : Color.gray).WithAlpha(baseAlpha * alpha);
                label2.Foreground = label2.Foreground.WithAlpha(baseAlpha * alpha);
                icon.Foreground = icon.Foreground.WithAlpha(baseAlpha * alpha);
                infinity.Foreground = icon.Foreground.WithAlpha(baseAlpha * alpha);
                visibilityIcon.Foreground = icon.Foreground.WithAlpha(baseAlpha * alpha);

                label.Text = text;
                label.Update();
                label2.Update();
                labelChange.Update();
            }

            void ShowDifference(float cur, float prev, bool whole = false)
            {
                MorphUtils.LogError($"cur: {cur}, prev: {prev}, whole: {whole}, frame: " + Time.frameCount);
                float diff;
                bool positive = cur - prev > 0;
                bool good = positive ^ lessIsBetter;
                bool noChange = false;
                if (whole)
                {
                    diff = cur - prev;
                    noChange = diff == 0;
                    labelChange.Text = " " + (positive ? "+" : "-") + Mathf.Abs(diff).ToString("0.0").TrimEnd('0').TrimEnd('.') + GetSuffix();
                }
                else
                {
                    diff = 1 + (cur - prev) / prev;
                    noChange = diff == 1;
                    labelChange.Text = " x" + Mathf.Abs(diff).ToString("0.00");
                }

                changeAppearTime = noChange ? 0f : Time.time;
                labelChange.Foreground = good ? Color.green : Color.red;
                doingChange = true;
            }
        }

        void Awake()
        {
            // singleton
            Instance = this;

            SpawnLabels();
            StartCoroutine(AddPlayerEventsCo());
            DoFade();
        }
        
        IEnumerator AddPlayerEventsCo()
        {
            PlayerController prevPlayer = null;
            while (true)
            {
                var player = GameManager.Instance.PrimaryPlayer;
                if (player != prevPlayer)
                {
                    if (player != null)
                    {
                        GameManager.Instance.PrimaryPlayer.OnEnteredCombat += () => DoFade(alphaDuringCombat);
                        GameManager.Instance.PrimaryPlayer.OnRoomClearEvent += (p) => DoFade(alphaWhenSafe);
                    }
                    prevPlayer = player;
                }
                yield return null;
            }
        }

        public void DoFade(float alpha = 1, float dur = 0.25f)
        {
            fadeTo = alpha;
            fadeFrom = alphaMultiplier;
            fadeDur = dur;
            fadeStartTime = Time.realtimeSinceStartup;
        }

        public void DoPausedFade(float alpha = 1, float dur = 0.25f)
        {
            pausedFadeTo = alpha;
            pausedFadeFrom = pausedAlphaMultiplier;
            pausedFadeDur = dur;
            pausedFadeStartTime = Time.realtimeSinceStartup;        
        }

        void SpawnLabels()
        {
            statsContainer = new SGroup() { Background = Color.clear, Size = new Vector2(400, 2), AutoGrowDirection = SGroup.EDirection.Vertical };

            void GenerateStatLabel(StatType statType)
            {
                var stat = new Stat(statType);
                statsContainer.Children.Add(stat.container);
                stats[statType] = stat;
            }

            statsContainer.Children.Add(new SRect(Color.clear) { Size = Vector2.zero }); // add empty element to the beginning due to a bug in SGUI's code TT
            weaponStats.ForEach((s) => GenerateStatLabel(s));
            statsContainer.Children.Add(new SRect(Color.clear) { Size = Vector2.zero.WithY(32) });
            playerStats.ForEach((s) => GenerateStatLabel(s));

            statsContainer.AutoLayout = (SGroup g) => new Action<int, SElement>(g.AutoLayoutVertical);
            SGUIRoot.Main.Children.Add(statsContainer);

            LoadSettings();
        }

        void LateUpdate()
        {
            // do fade tween(s)
            var curFadeTime = Time.realtimeSinceStartup - fadeStartTime;
            alphaMultiplier = Mathf.Lerp(fadeFrom, fadeTo, curFadeTime / fadeDur);
            var curPausedFadeTime = Time.realtimeSinceStartup - pausedFadeStartTime;
            pausedAlphaMultiplier = Mathf.Lerp(pausedFadeFrom, pausedFadeTo, curPausedFadeTime / pausedFadeDur);

            // update stats
            UpdateStats(); // TO DO: only call this on gun switch & stat recalculation

            // update hud
            statsContainer.ContentSize = statsContainer.Size.WithY(0);
            statsContainer.UpdateStyle();
            statsContainer.Position.y = statsContainer.Root.Size.y / 2 - statsContainer.Size.y / 2 + 5;
        }

        public void SwitchEditMode(bool edit)
        {
            editing = edit;

            if (edit)
            {
                DoPausedFade(1f, 0);
            }
            else
            {
                SaveSettings();
                DoPausedFade(0f, 0);
            }
        }

        public void SaveSettings()
        {
            var s = SettingsManager.settings;

            var statsList = new List<string>();
            foreach (var stat in stats.Values)
                if (stat.visible) statsList.Add(stat.type.ToString());
            statsList.Sort();

            s.shownStats = statsList;
            s.opacityWhenSafe = alphaWhenSafe;
            s.opacityDuringCombat = alphaDuringCombat;
            SettingsManager.SaveSettings();
        }

        public void LoadSettings()
        {
            var s = SettingsManager.settings;
            foreach (var stat in stats.Values)
                stat.visible = s.shownStats.Contains(stat.type.ToString());

            alphaDuringCombat = s.opacityDuringCombat;
            alphaWhenSafe = s.opacityWhenSafe;
        }

        void UpdateStats(bool init = false)
        {
            //statsContainer.Visible = !GameManager.Instance.IsPaused;

            PlayerController player = GameManager.Instance.PrimaryPlayer;
            Gun gun = player?.CurrentGun;
            if (gun == null)
            {
                statsContainer.Visible = false;
                return;
            }
            statsContainer.Visible = true;

            // get gun info
            ProjectileVolleyData volley = gun.Volley;
            ProjectileModule projectileModule = gun.DefaultModule;
            Projectile projectile = projectileModule.GetCurrentProjectile();
            ProjectileData projectileData = projectile.baseData;
            ExplosiveModifier explosiveModifier = projectile.GetComponent<ExplosiveModifier>();

            // get player stats
            PlayerStats pStats = player.stats;
            int projectilesMulti = (int)Mathf.Pow(3, player.passiveItems.Where(item => item.PickupObjectId == 241).Count()); // scattershot is the only item that changes number of projectiles
            float damageMulti = pStats.GetStatValue(PlayerStats.StatType.Damage) * projectilesMulti;
            float reloadTimeMulti = pStats.GetStatValue(PlayerStats.StatType.ReloadSpeed);
            float firerateMulti = 1f / pStats.GetStatValue(PlayerStats.StatType.RateOfFire);
            float projectileSpeedMulti = pStats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
            float chargeMulti = pStats.GetStatValue(PlayerStats.StatType.ChargeAmountMultiplier);
            float rangeMulti = pStats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
            float clipMulti = pStats.GetStatValue(PlayerStats.StatType.AdditionalClipCapacityMultiplier);
            float spreadMulti = pStats.GetStatValue(PlayerStats.StatType.Accuracy);
            float ammoMulti = pStats.GetStatValue(PlayerStats.StatType.AmmoCapacityMultiplier);
            float bulletSizeMulti = pStats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
            float knockbackMulti = pStats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);

            float speed = pStats.GetStatModifier(PlayerStats.StatType.MovementSpeed);
            float curse = pStats.GetStatValue(PlayerStats.StatType.Curse);
            float coolness = pStats.GetStatValue(PlayerStats.StatType.Coolness);

            // get weapon stats
            int projectiles = volley.ModulesAreTiers ? 1 : volley.projectiles.Count();
            projectiles /= projectilesMulti;
            float impactDamage = projectileData.damage;
            float explosionDamage = explosiveModifier != null ? explosiveModifier.explosionData.damage : 0f;
            float singleDamage = impactDamage + explosionDamage;
            float damage = singleDamage * projectiles;
            float firerate = gun.GetPrimaryCooldown();
            float projectileSpeed = projectileData.speed;
            float range = projectileData.range;
            float reloadTime = gun.reloadTime;
            float charge = projectileModule.LongestChargeTime;
            float clip = gun.ClipCapacity / clipMulti;
            float spread = projectileModule.angleVariance * 2f;
            float ammo = gun.InfiniteAmmo ? float.MaxValue : gun.GetBaseMaxAmmo();
            float bulletSize = 1f; // this is always 1, will calculate via sprite size in the future
            float knockback = projectileData.force;

            // calculate dps
            float clipTime = (clip * clipMulti - 1) * firerate * firerateMulti + clip * clipMulti * charge + reloadTime * reloadTimeMulti;
            float clipDamage = clip * clipMulti * damage * damageMulti;
            float dps = clipDamage / clipTime;

            foreach (var stat in stats.Values)
            {
                switch (stat.type)
                {
                    case StatType.Speed:
                        stat.value = speed;
                        stat.unit = StatUnit.Multiplier;
                        stat.announceBaseChange = true;
                        break;
                    case StatType.Curse:
                        stat.value = curse;
                        stat.lessIsBetter = true;
                        stat.announceBaseChange = true;
                        break;
                    case StatType.Coolness: 
                        stat.value = coolness;
                        stat.announceBaseChange = true;
                        break;
                    case StatType.Damage:
                        stat.value = damage;
                        stat.multiplier = damageMulti;
                        break;
                    case StatType.FireDelay: 
                        stat.value = firerate;
                        stat.multiplier = firerateMulti;
                        stat.lessIsBetter = true;
                        stat.unit = StatUnit.Seconds;
                        break;
                    case StatType.BulletSpeed:
                        stat.value = projectileSpeed;
                        stat.multiplier = projectileSpeedMulti;
                        break;
                    case StatType.Range:
                        stat.value = range;
                        stat.multiplier = rangeMulti;
                        break;
                    case StatType.ReloadTime:
                        stat.value = reloadTime;
                        stat.multiplier = reloadTimeMulti;
                        stat.lessIsBetter = true;
                        stat.unit = StatUnit.Seconds;
                        break;
                    case StatType.Projectiles:
                        stat.value = projectiles;
                        stat.multiplier = projectilesMulti;
                        stat.unit = StatUnit.Whole;
                        break;
                    case StatType.ChargeTime:
                        stat.value = charge;
                        stat.multiplier = chargeMulti;
                        stat.lessIsBetter = true;
                        stat.unit = StatUnit.Seconds;
                        break;
                    case StatType.DPS:
                        stat.value = dps;
                        break;
                    case StatType.AmmoCapacity:
                        stat.value = ammo;
                        stat.multiplier = ammoMulti;
                        stat.unit = StatUnit.Whole;
                        break;
                    case StatType.ClipSize:
                        stat.value = clip;
                        stat.multiplier = clipMulti;
                        stat.unit = StatUnit.Whole;
                        break;
                    case StatType.Spread:
                        stat.value = spread;
                        stat.multiplier = spreadMulti;
                        stat.lessIsBetter = true;
                        stat.unit = StatUnit.Degrees;
                        break;
                    case StatType.BulletSize:
                        stat.value = bulletSize;
                        stat.multiplier = bulletSizeMulti;
                        break;
                    case StatType.Knockback:
                        stat.value = knockback;
                        stat.multiplier = knockbackMulti;
                        break;
                }

                stat.Update(!this.init);
            }
            this.init = true;
        }

        public static RHController Instance;
    }
}
