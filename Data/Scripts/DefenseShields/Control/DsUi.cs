using System;
using VRage.Game.ModAPI;
using VRageMath;

namespace DefenseShields
{
    using System.Collections.Generic;
    using Support;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using VRage.ModAPI;
    using VRage.Utils;

    internal static class DsUi
    {
        #region Create UI
        private static readonly List<MyTerminalControlComboBoxItem> ShellList = new List<MyTerminalControlComboBoxItem>()
        {
            new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Medium Reflective") },
            new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("High Reflective") },
            new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Low Reflective") },
            new MyTerminalControlComboBoxItem() { Key = 3, Value = MyStringId.GetOrCompute("Medium Reflective Red Tint") },
            new MyTerminalControlComboBoxItem() { Key = 4, Value = MyStringId.GetOrCompute("Medium Reflective Blue Tint") },
            new MyTerminalControlComboBoxItem() { Key = 5, Value = MyStringId.GetOrCompute("Medium Reflective Green Tint") },
            new MyTerminalControlComboBoxItem() { Key = 6, Value = MyStringId.GetOrCompute("Medium Reflective Purple Tint") },
            new MyTerminalControlComboBoxItem() { Key = 7, Value = MyStringId.GetOrCompute("Medium Reflective Gold Tint") },
            new MyTerminalControlComboBoxItem() { Key = 8, Value = MyStringId.GetOrCompute("Medium Reflective Orange Tint") },
            new MyTerminalControlComboBoxItem() { Key = 9, Value = MyStringId.GetOrCompute("Medium Reflective Cyan Tint") },
        };

        private static readonly List<MyTerminalControlComboBoxItem> VisibleList = new List<MyTerminalControlComboBoxItem>()
        {
            new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Always Visible") },
            new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Never Visible") },
            new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Visible On Hit") }
        };

        private static readonly List<MyTerminalControlComboBoxItem> ReserveList = new List<MyTerminalControlComboBoxItem>()
        {
            new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Disabled") },
            new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("KiloWatt") },
            new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("MegaWatt") },
            new MyTerminalControlComboBoxItem() { Key = 3, Value = MyStringId.GetOrCompute("GigaWatt") },
            new MyTerminalControlComboBoxItem() { Key = 4, Value = MyStringId.GetOrCompute("TeraWatt") },
        };
            

        internal static void CreateUi(IMyTerminalBlock shield)
        {
            Session.Instance.WidthSlider.Visible = ShowSizeSlider;
            Session.Instance.HeightSlider.Visible = ShowSizeSlider;
            Session.Instance.DepthSlider.Visible = ShowSizeSlider;

            Session.Instance.OffsetWidthSlider.Visible = ShowOffSetSlider;
            Session.Instance.OffsetHeightSlider.Visible = ShowOffSetSlider; 
            Session.Instance.OffsetDepthSlider.Visible = ShowOffSetSlider;

            Session.Instance.Fit.Visible = ShowReSizeCheckBoxs;
            Session.Instance.SphereFit.Visible = ShowReSizeCheckBoxs;
            Session.Instance.FortifyShield.Visible = ShowReSizeCheckBoxs;
        }

        internal static bool ShowOffSetSlider(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp != null;
        }

        internal static bool ShowSizeSlider(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            var station = comp != null && comp.IsStatic && comp.ShieldMode == DefenseShields.ShieldType.Station;
            return station;
        }

        internal static float GetRate(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.Rate ?? 0f;
        }

        internal static void SetRate(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.Rate = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static float GetFit(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.Fit ?? 0f;
        }

        internal static void SetFit(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            var checkValue = (int)Math.Round(newValue);
            if (checkValue != comp.DsSet.Settings.Fit)
            {
                comp.DsSet.Settings.Fit = (int)Math.Round(newValue);
                comp.FitChanged = true;
                comp.SettingsUpdated = true;
                comp.ClientUiUpdate = true;
            }
        }

        internal static bool GetSphereFit(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.SphereFit ?? false;
        }

        internal static void SetSphereFit(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.SphereFit = newValue;
            comp.FitChanged = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static long GetPowerScale(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.PowerScale ?? 0;
        }

        internal static void SetPowerScale(IMyTerminalBlock block, long newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.PowerScale = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetFortify(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.FortifyShield ?? false;
        }

        internal static void SetFortify(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.FortifyShield = newValue;
            comp.FitChanged = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static float GetWidth(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.Width ?? 0f;
        }

        internal static void SetWidth(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.Width = newValue;
            comp.UpdateDimensions = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.LosCheckTick = Session.Instance.Tick + 1800;
        }

        internal static float GetHeight(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.Height ?? 0f;
        }

        internal static void SetHeight(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.Height = newValue;
            comp.UpdateDimensions = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.LosCheckTick = Session.Instance.Tick + 1800;
        }

        internal static float GetDepth(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.Depth ?? 0f;
        }

        internal static void SetDepth(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.Depth = newValue;
            comp.UpdateDimensions = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.LosCheckTick = Session.Instance.Tick + 1800;
        }

        internal static float GetOffsetWidth(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.ShieldOffset.X ?? 0;
        }

        internal static void SetOffsetWidth(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;

            comp.DsSet.Settings.ShieldOffset.X = (int)newValue;
            comp.UpdateDimensions = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.LosCheckTick = Session.Instance.Tick + 1800;
            ((MyCubeBlock)block).UpdateTerminal();
        }

        internal static float GetOffsetHeight(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.ShieldOffset.Y ?? 0;
        }

        internal static void SetOffsetHeight(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;

            comp.DsSet.Settings.ShieldOffset.Y = (int)newValue;
            comp.UpdateDimensions = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.LosCheckTick = Session.Instance.Tick + 1800;
            ((MyCubeBlock)block).UpdateTerminal();
        }

        internal static float GetOffsetDepth(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.ShieldOffset.Z ?? 0;
        }

        internal static void SetOffsetDepth(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;

            comp.DsSet.Settings.ShieldOffset.Z = (int)newValue;
            comp.UpdateDimensions = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.LosCheckTick = Session.Instance.Tick + 1800;
            ((MyCubeBlock)block).UpdateTerminal();
        }

        internal static bool GetBatteries(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.UseBatteries ?? false;
        }

        internal static void SetBatteries(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.UseBatteries = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetHideActive(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.ActiveInvisible ?? false;
        }

        internal static void SetHideActive(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.ActiveInvisible = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetRefreshAnimation(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.RefreshAnimation ?? false;
        }

        internal static void SetRefreshAnimation(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.RefreshAnimation = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetHitWaveAnimation(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.HitWaveAnimation ?? false;
        }

        internal static void SetHitWaveAnimation(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.HitWaveAnimation = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetNoWarningSounds(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.NoWarningSounds ?? false;
        }

        internal static void SetDimShieldHits(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.DimShieldHits = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetDimShieldHits(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.DimShieldHits ?? false;
        }

        internal static void SetNoWarningSounds(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.NoWarningSounds = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetSendToHud(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.SendToHud ?? false;
        }

        internal static void SetSendToHud(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.SendToHud = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool GetRaiseShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.RaiseShield ?? false;
        }

        internal static void SetRaiseShield(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.RaiseShield = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static long GetShell(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.ShieldShell ?? 0;
        }

        internal static void SetShell(IMyTerminalBlock block, long newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.ShieldShell = newValue;
            comp.SelectPassiveShell();
            comp.UpdatePassiveModel();
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static long GetVisible(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.Visible ?? 0;
        }

        internal static void SetVisible(IMyTerminalBlock block, long newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.Visible = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static void ListShell(List<MyTerminalControlComboBoxItem> shellList)
        {
            foreach (var shell in ShellList) shellList.Add(shell);
        }

        internal static void ListVisible(List<MyTerminalControlComboBoxItem> visibleList)
        {
            foreach (var visible in VisibleList) visibleList.Add(visible);
        }

        private static bool ShowReSizeCheckBoxs(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            var notStation = comp != null && !comp.Shield.CubeGrid.IsStatic;
            return notStation;
        }

        internal static void ListPowerScale(List<MyTerminalControlComboBoxItem> reserveList)
        {
            foreach (var shell in ReserveList) reserveList.Add(shell);
        }


        internal static float GetPowerWatts(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.PowerWatts ?? 0;
        }

        internal static void SetPowerWatts(IMyTerminalBlock block, float newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            comp.DsSet.Settings.PowerWatts = (int)newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static bool EnablePowerWatts(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return false;
            return comp.DsSet.Settings.PowerScale != 0;
        }


        internal static bool GeTopShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp != null && SideEnabled(Session.ShieldSides.Up, comp.DsSet.Settings.ShieldRedirects.Y);
        }

        internal static void SetTopShield(IMyTerminalBlock block, bool redirect)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            if (Math.Abs(comp.DsSet.Settings.ShieldRedirects.X) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Y) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Z) >= 5 && redirect)
                return;

            if (!NewSideState(comp, Session.ShieldSides.Up, redirect, comp.DsSet.Settings.ShieldRedirects.Y))
                return;

            comp.FitChanged = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.StartRedirectTimer();
        }

        internal static bool GetBottomShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp != null && SideEnabled(Session.ShieldSides.Down, comp.DsSet.Settings.ShieldRedirects.Y);

        }

        internal static void SetBottomShield(IMyTerminalBlock block, bool redirect)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            if (Math.Abs(comp.DsSet.Settings.ShieldRedirects.X) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Y) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Z) >= 5 && redirect)
                return;

            if (!NewSideState(comp, Session.ShieldSides.Down, redirect, comp.DsSet.Settings.ShieldRedirects.Y))
                return;

            comp.FitChanged = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.StartRedirectTimer();
        }


        internal static bool GetRightShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp != null && SideEnabled(Session.ShieldSides.Right, comp.DsSet.Settings.ShieldRedirects.X);
        }

        internal static void SetRightShield(IMyTerminalBlock block, bool redirect)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            if (Math.Abs(comp.DsSet.Settings.ShieldRedirects.X) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Y) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Z) >= 5 && redirect)
                return;

            if (!NewSideState(comp, Session.ShieldSides.Right, redirect, comp.DsSet.Settings.ShieldRedirects.X))
                return;

            comp.FitChanged = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.StartRedirectTimer();
        }

        internal static bool GetLeftShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp != null && SideEnabled(Session.ShieldSides.Left, comp.DsSet.Settings.ShieldRedirects.X);
        }

        internal static void SetLeftShield(IMyTerminalBlock block, bool redirect)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            if (Math.Abs(comp.DsSet.Settings.ShieldRedirects.X) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Y) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Z) >= 5 && redirect)
                return;

            if (!NewSideState(comp, Session.ShieldSides.Left, redirect, comp.DsSet.Settings.ShieldRedirects.X))
                return;

            comp.FitChanged = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.StartRedirectTimer();
        }


        internal static bool GetFrontShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp != null && SideEnabled(Session.ShieldSides.Forward, comp.DsSet.Settings.ShieldRedirects.Z);
        }

        internal static void SetFrontShield(IMyTerminalBlock block, bool redirect)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            if (Math.Abs(comp.DsSet.Settings.ShieldRedirects.X) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Y) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Z) >= 5 && redirect)
                return;

            if (!NewSideState(comp, Session.ShieldSides.Forward, redirect, comp.DsSet.Settings.ShieldRedirects.Z))
                return;

            comp.FitChanged = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.StartRedirectTimer();
        }

        internal static bool GetBackShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp != null && SideEnabled(Session.ShieldSides.Backward, comp.DsSet.Settings.ShieldRedirects.Z);
        }

        internal static void SetBackShield(IMyTerminalBlock block, bool redirect)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            if (Math.Abs(comp.DsSet.Settings.ShieldRedirects.X) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Y) + Math.Abs(comp.DsSet.Settings.ShieldRedirects.Z) >= 5 && redirect)
                return;

            if (!NewSideState(comp, Session.ShieldSides.Backward, redirect, comp.DsSet.Settings.ShieldRedirects.Z))
                return;

            comp.FitChanged = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
            comp.StartRedirectTimer();
        }


        public static bool NewSideState(DefenseShields ds, Session.ShieldSides side, bool redirect, int currentState)
        {
            int newState;
            var enableValue = Session.Instance.SideControlMap[side];
            var oppositeValue = enableValue * -1;

            if (redirect)
                newState = currentState == oppositeValue ? 2 : enableValue;
            else
                newState = currentState == 2 ? oppositeValue : 0;

            SetRedirect(ds, side, newState);

            return newState != currentState;
        }

        public static bool SideEnabled(Session.ShieldSides side, int state)
        {
            return state == 2 || Session.Instance.SideControlMap[side] == state;
        }

        public static void SetRedirect(DefenseShields ds, Session.ShieldSides side, int newValue)
        {
            if (!Session.Instance.DedicatedServer && Session.Instance.Settings.ClientConfig.Notices) {

                var pendingChanges = ds.DsSet.Settings.ShieldRedirects != ds.ShieldRedirectState;
                var enableText = SideEnabled(side, newValue) ? "Shunting" : "Normalizing";
                var text = $"[{enableText} {side}] Shields in 2 seconds -- delaying previous changes: [{pendingChanges}]";
                Session.Instance.SendNotice(text);
            }

            switch (side)
            {
                case Session.ShieldSides.Left:
                case Session.ShieldSides.Right:
                    ds.DsSet.Settings.ShieldRedirects.X = newValue;
                    break;
                case Session.ShieldSides.Up:
                case Session.ShieldSides.Down:
                    ds.DsSet.Settings.ShieldRedirects.Y = newValue;
                    break;
                case Session.ShieldSides.Forward:
                case Session.ShieldSides.Backward:
                    ds.DsSet.Settings.ShieldRedirects.Z = newValue;
                    break;
            }
        }

        internal static bool RedirectEnabled(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return false;

            return comp.DsSet.Settings.SideShunting;
        }


        internal static bool GetSideShunting(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.SideShunting ?? false;
        }

        internal static void SetSideShunting(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            if (comp.DsSet.Settings.SideShunting != newValue)
            {
                comp.StartRedirectTimer();
                comp.DsSet.Settings.SideShunting = newValue;
                comp.FitChanged = true;
                comp.SettingsUpdated = true;
                comp.ClientUiUpdate = true;
            }
        }

        internal static bool GetShowShunting(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp?.DsSet.Settings.ShowRedirect ?? false;
        }

        internal static void SetShowShunting(IMyTerminalBlock block, bool newValue)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;

            comp.DsSet.Settings.ShowRedirect = newValue;
            comp.FitChanged = true;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }
        #endregion
    }
}
