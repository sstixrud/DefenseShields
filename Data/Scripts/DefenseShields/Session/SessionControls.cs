namespace DefenseShields
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Control;
    using Support;
    using Sandbox.Game.Localization;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces.Terminal;
    using VRage.ModAPI;
    using VRage.Utils;

    public partial class Session
    {
        #region UI Config
        public static void AppendConditionToAction<T>(Func<IMyTerminalAction, bool> actionFindCondition, Func<IMyTerminalAction, IMyTerminalBlock, bool> actionEnabledAppend)
        {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<T>(out actions);

            foreach (var a in actions)
            {
                if (actionFindCondition(a))
                {
                    var existingAction = a.Enabled;

                    a.Enabled = (b) => (existingAction?.Invoke(b) ?? true) && actionEnabledAppend(a, b);
                }
            }
        }

        public void CreateControllerElements(IMyTerminalBlock block)
        {
            try
            {
                if (DsControl) return;
                var comp = block?.GameLogic?.GetAs<DefenseShields>();
                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep0");
                ToggleShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_ToggleShield", "Shield Status", "Raise or Lower Shields", "Up", "Down", DsUi.GetRaiseShield, DsUi.SetRaiseShield);
                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep1");
                //ChargeSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_ChargeRate", "Shield Charge Rate", "Percentage Of Power The Shield May Consume", DsUi.GetRate, DsUi.SetRate);
                //ChargeSlider.SetLimits(20, 95);
                PowerScaleSelect = TerminalHelpers.AddCombobox(comp?.Shield, "DS-C_PowerScale", "Select Power Scale", "Select the power scale to use", DsUi.GetPowerScale, DsUi.SetPowerScale, DsUi.ListPowerScale);

                PowerWatts = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_PowerWatts", "Power To Use", "Select the maximum scaled power the shield can use", DsUi.GetPowerWatts, DsUi.SetPowerWatts,  DsUi.EnablePowerWatts);
                PowerWatts.SetLimits(1, 999);
                if (comp != null && comp.GridIsMobile)
                {
                    TerminalHelpers.Separator(comp.Shield, "DS-C_sep2");
                }

                Fit = TerminalHelpers.AddSlider(comp?.Shield, "DS-CFit", "Shield Fit", "Shield Fit", DsUi.GetFit, DsUi.SetFit);
                Fit.SetLimits(0, 29);

                SphereFit = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_SphereFit", "Sphere Shield", "Sphere Shield", DsUi.GetSphereFit, DsUi.SetSphereFit);
                FortifyShield = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_ShieldFortify", "Fortify Shield ", "Fortify Shield ", DsUi.GetFortify, DsUi.SetFortify);

                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep3");
                
                WidthSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_WidthSlider", "Shield Size Width", "Shield Size Width", DsUi.GetWidth, DsUi.SetWidth);
                WidthSlider.SetLimits(30, 1000);

                HeightSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_HeightSlider", "Shield Size Height", "Shield Size Height", DsUi.GetHeight, DsUi.SetHeight);
                HeightSlider.SetLimits(30, 1000);

                DepthSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_DepthSlider", "Shield Size Depth", "Shield Size Depth", DsUi.GetDepth, DsUi.SetDepth);
                DepthSlider.SetLimits(30, 1000);

                OffsetWidthSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_OffsetWidthSlider", "Width Offset", "Width Offset", DsUi.GetOffsetWidth, DsUi.SetOffsetWidth);
                OffsetWidthSlider.SetLimits(-69, 69);

                OffsetHeightSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_OffsetHeightSlider", "Height Offset", "Height Offset", DsUi.GetOffsetHeight, DsUi.SetOffsetHeight);
                OffsetHeightSlider.SetLimits(-69, 69);

                OffsetDepthSlider = TerminalHelpers.AddSlider(comp?.Shield, "DS-C_OffsetDepthSlider", "Depth Offset", "Depth Offset", DsUi.GetOffsetDepth, DsUi.SetOffsetDepth);
                OffsetDepthSlider.SetLimits(-69, 69);

                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep4");

                BatteryBoostCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_UseBatteries", "Ignore battery input power ", "Allow shields to fight with batteries for power", DsUi.GetBatteries, DsUi.SetBatteries);
                SendToHudCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_HideIcon", "Broadcast Shield Status To Hud", "Broadcast Shield Status To Nearby Friendly Huds", DsUi.GetSendToHud, DsUi.SetSendToHud);
                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep5");
                ShellSelect = TerminalHelpers.AddCombobox(comp?.Shield, "DS-C_ShellSelect", "Select Shield Look", "Select shield's shell texture", DsUi.GetShell, DsUi.SetShell, DsUi.ListShell);

                ShellVisibility = TerminalHelpers.AddCombobox(comp?.Shield, "DS-C_ShellSelect", "Select Shield Visibility", "Determines when the shield is visible", DsUi.GetVisible, DsUi.SetVisible, DsUi.ListVisible);

                HideActiveCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_HideActive", "Hide Shield Health On Hit  ", "Hide Shield Health Grid On Hit", DsUi.GetHideActive, DsUi.SetHideActive);

                RefreshAnimationCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_RefreshAnimation", "Show Refresh Animation  ", "Show Random Refresh Animation", DsUi.GetRefreshAnimation, DsUi.SetRefreshAnimation);
                HitWaveAnimationCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_HitWaveAnimation", "Show Hit Wave Animation", "Show Wave Effect On Shield Damage", DsUi.GetHitWaveAnimation, DsUi.SetHitWaveAnimation);
                NoWarningSoundsCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_NoWarningSounds", "Disable audio warnings    ", "Supress shield audio warnings", DsUi.GetNoWarningSounds, DsUi.SetNoWarningSounds);
                DimShieldHitsCheckBox = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_DimShieldHits", "Dim Incoming Hit Effects ", "Supress brightness of incoming hit effects", DsUi.GetDimShieldHits, DsUi.SetDimShieldHits);

                TerminalHelpers.Separator(comp?.Shield, "DS-C_sep6");
                SideRedirect = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_SideRedirect", "Shunt Shields", "Enable Shield Shunting", DsUi.GetSideRedirect, DsUi.SetSideRedirect);
                ShowRedirect = TerminalHelpers.AddCheckbox(comp?.Shield, "DS-C_ShowRedirect", "Show Shunted Shields", "Enable/Disable showing side shield states", DsUi.GetShowRedirect, DsUi.SetShowRedirect);

                TopShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_TopShield", "Shunt Top Shield", "Redirect Top shield power to others", "On", "Off", DsUi.GeTopShield, DsUi.SetTopShield, DsUi.RedirectEnabled);
                BottomShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_BottomShield", "Shunt Bottom Shield", "Redirect bottom shield power to others", "On", "Off", DsUi.GetBottomShield, DsUi.SetBottomShield, DsUi.RedirectEnabled);
                LeftShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_LeftShield", "Shunt Left Shield", "Redirect Left shield power to others", "On", "Off", DsUi.GetLeftShield, DsUi.SetLeftShield, DsUi.RedirectEnabled);
                RightShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_RightShield", "Shunt Right Shield", "Redirect Right shield power to others", "On", "Off", DsUi.GetRightShield, DsUi.SetRightShield, DsUi.RedirectEnabled);
                FrontShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_FrontShield", "Shunt Front Shield", "Redirect Front shield power to others", "On", "Off", DsUi.GetFrontShield, DsUi.SetFrontShield, DsUi.RedirectEnabled);
                BackShield = TerminalHelpers.AddOnOff(comp?.Shield, "DS-C_BackShield", "Shunt Back Shield", "Redirect Back shield power to others", "On", "Off", DsUi.GetBackShield, DsUi.SetBackShield, DsUi.RedirectEnabled);


                CreateAction<IMyUpgradeModule>(ToggleShield);

                CreateAction<IMyUpgradeModule>(SphereFit);
                CreateFitAction<IMyUpgradeModule>(Fit);
                CreateAction<IMyUpgradeModule>(FortifyShield);


                CreateAction<IMyUpgradeModule>(HideActiveCheckBox);
                CreateAction<IMyUpgradeModule>(RefreshAnimationCheckBox);
                CreateAction<IMyUpgradeModule>(HitWaveAnimationCheckBox);
                CreateAction<IMyUpgradeModule>(SendToHudCheckBox);
                CreateAction<IMyUpgradeModule>(BatteryBoostCheckBox);
                CreateDepthAction<IMyUpgradeModule>(DepthSlider);
                CreateWidthAction<IMyUpgradeModule>(WidthSlider);
                CreateHeightAction<IMyUpgradeModule>(HeightSlider);

                CreateAction<IMyUpgradeModule>(TopShield);
                CreateAction<IMyUpgradeModule>(BottomShield);
                CreateAction<IMyUpgradeModule>(LeftShield);
                CreateAction<IMyUpgradeModule>(RightShield);
                CreateAction<IMyUpgradeModule>(FrontShield);
                CreateAction<IMyUpgradeModule>(BackShield);

                CreateAction<IMyUpgradeModule>(SideRedirect);

                DsControl = true;
            }
            catch (Exception ex) { Log.Line($"Exception in CreateControlerUi: {ex}"); }
        }

        internal static void TerminalDepthIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetDepth(blk);
            var nextValue = currentValue + 1 < max ? currentValue + 1 : currentValue; 
            DsUi.SetOffsetDepth(blk, nextValue);
        }

        internal static void TerminalActionDepthDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetDepth(blk);
            var nextValue = currentValue - 1 > -max ? currentValue - 1 : currentValue;
            DsUi.SetOffsetDepth(blk, nextValue);
        }

        internal static void TerminalActioWidthIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;
            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetWidth(blk);
            var nextValue = currentValue + 1 < max ? currentValue + 1 : currentValue;
            DsUi.SetOffsetWidth(blk, nextValue);
        }

        internal static void TerminalActionWidthDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetWidth(blk);
            var nextValue = currentValue - 1 > -max ? currentValue - 1 : currentValue;
            DsUi.SetOffsetWidth(blk, nextValue);
        }

        internal static void TerminalActioHeightIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetHeight(blk);
            var nextValue = currentValue + 1 < max ? currentValue + 1 : currentValue;
            DsUi.SetOffsetHeight(blk, nextValue);
        }

        internal static void TerminalActionHeightDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var max = comp.ShieldMode == DefenseShields.ShieldType.Station ? 70 : 39;
            var currentValue = DsUi.GetOffsetHeight(blk);
            var nextValue = currentValue - 1 > -max ? currentValue - 1 : currentValue;
            DsUi.SetOffsetHeight(blk, nextValue);
        }

        internal static void TerminalActioFitSizeIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var currentValue = DsUi.GetFit(blk);
            var nextValue = currentValue + 1 < Instance.Fits.Length ? currentValue + 1 : currentValue;
            DsUi.SetFit(blk, nextValue);
        }

        internal static void TerminalActionFitSizeDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null)
                return;

            var currentValue = DsUi.GetFit(blk);
            var nextValue = currentValue - 1 >= 0 ? currentValue - 1 : currentValue;
            DsUi.SetFit(blk, nextValue);
        }

        internal static bool HasShield(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<DefenseShields>();
            return comp != null;
        }

        internal static void FitSizeWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            sb.Append(comp.DsSet.Settings.Fit);
        }

        internal static void DepthWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            sb.Append(DsUi.GetOffsetDepth(blk));
        }

        internal static void WidthWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            sb.Append(DsUi.GetOffsetWidth(blk));
        }

        internal static void HeightWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk?.GameLogic?.GetAs<DefenseShields>();
            if (comp == null) return;
            sb.Append(DsUi.GetOffsetHeight(blk));
        }

        public void CreateDepthAction<T>(IMyTerminalControlSlider c)
        {
            var id = ((IMyTerminalControl)c).Id;

            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_DepthIncrease");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder($"Depth Increase");
            action0.Action = TerminalDepthIncrease;
            action0.Writer = DepthWriter;
            action0.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_DepthDecrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder($"Depth Decrease");
            action1.Action = TerminalActionDepthDecrease;
            action1.Writer = DepthWriter;
            action1.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
        }

        public void CreateWidthAction<T>(IMyTerminalControlSlider c)
        {
            var id = ((IMyTerminalControl)c).Id;

            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_WidthIncrease");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder($"Width Increase");
            action0.Action = TerminalActioWidthIncrease;
            action0.Writer = WidthWriter;
            action0.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_WidthDecrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder($"Width Decrease");
            action1.Action = TerminalActionWidthDecrease;
            action1.Writer = WidthWriter;
            action1.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
        }

        public void CreateHeightAction<T>(IMyTerminalControlSlider c)
        {
            var id = ((IMyTerminalControl)c).Id;

            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_HeightIncrease");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder($"Height Increase");
            action0.Action = TerminalActioHeightIncrease;
            action0.Writer = HeightWriter;
            action0.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_HeightDecrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder($"Height Decrease");
            action1.Action = TerminalActionHeightDecrease;
            action1.Writer = HeightWriter;
            action1.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
        }

        public void CreateFitAction<T>(IMyTerminalControlSlider c)
        {
            var id = ((IMyTerminalControl)c).Id;

            var action0 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_FitIncrease");
            action0.Icon = @"Textures\GUI\Icons\Actions\Increase.dds";
            action0.Name = new StringBuilder($"Fit Increase");
            action0.Action = TerminalActioFitSizeIncrease;
            action0.Writer = FitSizeWriter;
            action0.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action0);

            var action1 = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_FitDecrease");
            action1.Icon = @"Textures\GUI\Icons\Actions\Decrease.dds";
            action1.Name = new StringBuilder($"Fit Decrease");
            action1.Action = TerminalActionFitSizeDecrease;
            action1.Writer = FitSizeWriter;
            action1.Enabled = HasShield;

            MyAPIGateway.TerminalControls.AddAction<T>(action1);
        }


        public void CreatePlanetShieldElements(IMyTerminalBlock block)
        {
            try
            {
            }
            catch (Exception ex) { Log.Line($"Exception in CreateControlerUi: {ex}"); }
        }

        public void CreateModulatorUi(IMyTerminalBlock block)
        {
            try
            {
                if (ModControl) return;
                var comp = block?.GameLogic?.GetAs<Modulators>();
                ModSep1 = TerminalHelpers.Separator(comp?.Modulator, "DS-M_sep1");
                ModDamage = TerminalHelpers.AddSlider(comp?.Modulator, "DS-M_DamageModulation", "Balance Shield Protection", "Balance Shield Protection", ModUi.GetDamage, ModUi.SetDamage);
                ModDamage.SetLimits(20, 180);
                ModSep2 = TerminalHelpers.Separator(comp?.Modulator, "DS-M_sep2");
                ModReInforce = TerminalHelpers.AddCheckbox(comp?.Modulator, "DS-M_ModulateReInforceProt", "Enhance structural integrity", "Enhance structural integrity, prevents damage from collisions", ModUi.GetReInforceProt, ModUi.SetReInforceProt);
                ModVoxels = TerminalHelpers.AddCheckbox(comp?.Modulator, " DS-M_ModulateVoxels", "Terrain is ignored by shield", "Let voxels bypass shield", ModUi.GetVoxels, ModUi.SetVoxels);
                ModGrids = TerminalHelpers.AddCheckbox(comp?.Modulator, "DS-M_ModulateGrids", "Entities may pass the shield", "Let grid bypass shield", ModUi.GetGrids, ModUi.SetGrids);
                ModAllies = TerminalHelpers.AddCheckbox(comp?.Modulator, "DS-M_ModulateAllies", "Allied players can bypass", "Let ally players bypass shield", ModUi.GetAllies, ModUi.SetAllies);
                ModEmp = TerminalHelpers.AddCheckbox(comp?.Modulator, "DS-M_ModulateEmpProt", "Protects against EMP damage", "But generates heat 10x faster", ModUi.GetEmpProt, ModUi.SetEmpProt);

                CreateActionDamageModRate<IMyUpgradeModule>(ModDamage);

                CreateAction<IMyUpgradeModule>(ModVoxels);
                CreateAction<IMyUpgradeModule>(ModGrids);
                CreateAction<IMyUpgradeModule>(ModEmp);
                CreateAction<IMyUpgradeModule>(ModReInforce);
                ModControl = true;
            }
            catch (Exception ex) { Log.Line($"Exception in CreateModulatorUi: {ex}"); }
        }

        public void CreateO2GeneratorUi(IMyTerminalBlock block)
        {
            try
            {
                if (O2Control) return;
                var comp = block?.GameLogic?.GetAs<O2Generators>();
                O2DoorFix = TerminalHelpers.AddCheckbox(comp?.O2Generator, "DS-FixRoomPressure", "Keen-Bug, Fix Room Pressure", "Keen-Bug, Fix Room Pressure", O2Ui.FixStatus, O2Ui.FixRooms);
                O2Control = true;
            }
            catch (Exception ex) { Log.Line($"Exception in CreateO2GeneratorUi: {ex}"); }
        }

        public void CreateAction<T>(IMyTerminalControlOnOffSwitch c)
        {
            try
            {
                var id = ((IMyTerminalControl)c).Id;
                var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                Action<IMyTerminalBlock, StringBuilder> writer = (b, s) => s.Append(c.Getter(b) ? c.OnText : c.OffText);
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Toggle");
                    a.Name = new StringBuilder(c.Title.String).Append(" - ").Append(c.OnText.String).Append("/").Append(c.OffText.String);

                    a.Icon = gamePath + @"\Textures\GUI\Icons\Actions\SmallShipToggle.dds";

                    a.ValidForGroups = true;
                    a.Action = (b) => c.Setter(b, !c.Getter(b));
                    a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_On");
                    a.Name = new StringBuilder(c.Title.String).Append(" - ").Append(c.OnText.String);
                    a.Icon = gamePath + @"\Textures\GUI\Icons\Actions\SmallShipSwitchOn.dds";
                    a.ValidForGroups = true;
                    a.Action = (b) => c.Setter(b, true);
                    a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Off");
                    a.Name = new StringBuilder(c.Title.String).Append(" - ").Append(c.OffText.String);
                    a.Icon = gamePath + @"\Textures\GUI\Icons\Actions\LargeShipSwitchOn.dds";
                    a.ValidForGroups = true;
                    a.Action = (b) => c.Setter(b, false);
                    a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateAction: {ex}"); }
        }

        private void CustomControls(IMyTerminalBlock tBlock, List<IMyTerminalControl> myTerminalControls)
        {
            try
            {
                LastTerminalId = tBlock.EntityId;
                switch (tBlock.BlockDefinition.SubtypeId)
                {
                    case "LargeShieldModulator":
                    case "SmallShieldModulator":
                        SetCustomDataToPassword(myTerminalControls);
                        break;
                    case "DSControlLarge":
                    case "DSControlSmall":
                    case "DSControlTable":
                        SetCustomDataToShieldFreq(myTerminalControls);
                        break;
                    default:
                        if (!CustomDataReset) ResetCustomData(myTerminalControls);
                        break;
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CustomControls: {ex}"); }
        }


        public void BlockTagActive(IMyTerminalBlock tBlock)
        {
            var customName = tBlock.CustomName;
            if (customName.StartsWith("[A] ")) return;
            if (customName.StartsWith("[B] "))
            {
                customName = customName.Remove(0, 4);
                customName = "[A] " + customName;
            }
            else
            {
                customName = "[A] " + customName;
            }
            tBlock.CustomName = customName;
        }

        public void BlockTagBackup(IMyTerminalBlock tBlock)
        {
            var customName = tBlock.CustomName;
            if (customName.StartsWith("[B] ")) return;
            if (customName.StartsWith("[A] "))
            {
                customName = customName.Remove(0, 4);
                customName = "[B] " + customName;
            }
            else
            {
                customName = "[B] " + customName;
            }
            tBlock.CustomName = customName;
        }

        private void SetCustomDataToPassword(IEnumerable<IMyTerminalControl> controls)
        {
            var customData = controls.First((x) => x.Id.ToString() == "CustomData");
            ((IMyTerminalControlTitleTooltip)customData).Title = Password;
            ((IMyTerminalControlTitleTooltip)customData).Tooltip = PasswordTooltip;
            customData.RedrawControl();
            CustomDataReset = false;
        }

        private void SetCustomDataToShieldFreq(IEnumerable<IMyTerminalControl> controls)
        {
            var customData = controls.First((x) => x.Id.ToString() == "CustomData");
            ((IMyTerminalControlTitleTooltip)customData).Title = ShieldFreq;
            ((IMyTerminalControlTitleTooltip)customData).Tooltip = ShieldFreqTooltip;
            customData.RedrawControl();
            CustomDataReset = false;
        }

        private void ResetCustomData(IEnumerable<IMyTerminalControl> controls)
        {
            var customData = controls.First((x) => x.Id.ToString() == "CustomData");
            ((IMyTerminalControlTitleTooltip)customData).Title = MySpaceTexts.Terminal_CustomData;
            ((IMyTerminalControlTitleTooltip)customData).Tooltip = MySpaceTexts.Terminal_CustomDataTooltip;
            customData.RedrawControl();
            CustomDataReset = true;
        }

        private void CreateAction<T>(IMyTerminalControlCheckbox c,
            bool addToggle = true,
            bool addOnOff = false,
            string iconPack = null,
            string iconToggle = null,
            string iconOn = null,
            string iconOff = null)
        {
            try
            {

                var id = ((IMyTerminalControl)c).Id;
                var name = c.Title.String;
                Action<IMyTerminalBlock, StringBuilder> writer = (b, s) => s.Append(c.Getter(b) ? c.OnText : c.OffText);

                if (iconToggle == null && iconOn == null && iconOff == null)
                {
                    var pack = iconPack ?? string.Empty;
                    var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                    iconToggle = gamePath + @"\Textures\GUI\Icons\Actions\" + pack + "Toggle.dds";
                    iconOn = gamePath + @"\Textures\GUI\Icons\Actions\" + pack + "SwitchOn.dds";
                    iconOff = gamePath + @"\Textures\GUI\Icons\Actions\" + pack + "SwitchOff.dds";
                }

                if (addToggle)
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Toggle");
                    a.Name = new StringBuilder(name).Append(" On/Off");
                    a.Icon = iconToggle;
                    a.ValidForGroups = true;
                    a.Action = (b) => c.Setter(b, !c.Getter(b));
                    if (writer != null)
                        a.Writer = writer;

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }

                if (addOnOff)
                {
                    {
                        var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_On");
                        a.Name = new StringBuilder(name).Append(" On");
                        a.Icon = iconOn;
                        a.ValidForGroups = true;
                        a.Action = (b) => c.Setter(b, true);
                        if (writer != null)
                            a.Writer = writer;

                        MyAPIGateway.TerminalControls.AddAction<T>(a);
                    }
                    {
                        var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Off");
                        a.Name = new StringBuilder(name).Append(" Off");
                        a.Icon = iconOff;
                        a.ValidForGroups = true;
                        a.Action = (b) => c.Setter(b, false);
                        if (writer != null)
                            a.Writer = writer;

                        MyAPIGateway.TerminalControls.AddAction<T>(a);
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateAction<T>(IMyTerminalControlCheckbox: {ex}"); }
        }

        private void CreateActionChargeRate<T>(IMyTerminalControlSlider c,
            float defaultValue = 50f, // HACK terminal controls don't have a default value built in...
            float modifier = 1f,
            string iconReset = null,
            string iconIncrease = null,
            string iconDecrease = null,
            bool gridSizeDefaultValue = false) // hacky quick way to get a dynamic default value depending on grid size)
        {
            try
            {
                var id = ((IMyTerminalControl)c).Id;
                var name = c.Title.String;

                if (iconReset == null && iconIncrease == null && iconDecrease == null)
                {
                    var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                    iconReset = gamePath + @"\Textures\GUI\Icons\Actions\Reset.dds";
                    iconIncrease = gamePath + @"\Textures\GUI\Icons\Actions\Increase.dds";
                    iconDecrease = gamePath + @"\Textures\GUI\Icons\Actions\Decrease.dds";
                }

                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Reset");
                    a.Name = new StringBuilder("Default ").Append(name);
                    if (!gridSizeDefaultValue)
                        a.Name.Append(" (").Append(defaultValue.ToString("0.###")).Append(")");
                    a.Icon = iconReset;
                    a.ValidForGroups = true;
                    a.Action = (b) => c.Setter(b, (gridSizeDefaultValue ? b.CubeGrid.GridSize : defaultValue));
                    a.Writer = (b, s) => s.Append(c.Getter(b));

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Increase");
                    a.Name = new StringBuilder("Increase ").Append(name).Append(" (+").Append(modifier.ToString("0.###")).Append(")");
                    a.Icon = iconIncrease;
                    a.ValidForGroups = true;
                    a.Action = ActionAddChargeRate;
                    a.Writer = (b, s) => s.Append(c.Getter(b));

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Decrease");
                    a.Name = new StringBuilder("Decrease ").Append(name).Append(" (-").Append(modifier.ToString("0.###")).Append(")");
                    a.Icon = iconDecrease;
                    a.ValidForGroups = true;
                    a.Action = ActionSubtractChargeRate;
                    a.Writer = (b, s) => s.Append(c.Getter(b).ToString("0.###"));

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateActionChargeRate: {ex}"); }
        }

        private void ActionAddChargeRate(IMyTerminalBlock b)
        {
            try
            {
                List<IMyTerminalControl> controls;
                MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
                var chargeRate = controls.First((x) => x.Id.ToString() == "DS-C_ChargeRate");
                var c = (IMyTerminalControlSlider)chargeRate;
                if (c.Getter(b) > 94)
                {
                    c.Setter(b, 95f);
                    return;
                }
                c.Setter(b, c.Getter(b) + 5f);
            }
            catch (Exception ex) { Log.Line($"Exception in ActionSubtractChargeRate: {ex}"); }
        }

        private void ActionSubtractChargeRate(IMyTerminalBlock b)
        {
            try
            {
                var controls = new List<IMyTerminalControl>();
                MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
                var chargeRate = controls.First((x) => x.Id.ToString() == "DS-C_ChargeRate");
                var c = (IMyTerminalControlSlider)chargeRate;
                if (c.Getter(b) < 21)
                {
                    c.Setter(b, 20f);
                    return;
                }
                c.Setter(b, c.Getter(b) - 5f);
            }
            catch (Exception ex) { Log.Line($"Exception in ActionSubtractChargeRate: {ex}"); }
        }

        private void CreateActionDamageModRate<T>(IMyTerminalControlSlider c,
        float defaultValue = 100f, // HACK terminal controls don't have a default value built in...
        float modifier = 1f,
        string iconReset = null,
        string iconIncrease = null,
        string iconDecrease = null,
        bool gridSizeDefaultValue = false) // hacky quick way to get a dynamic default value depending on grid size)
        {
            try
            {
                var id = ((IMyTerminalControl)c).Id;
                var name = c.Title.String;

                if (iconReset == null && iconIncrease == null && iconDecrease == null)
                {
                    var gamePath = MyAPIGateway.Utilities.GamePaths.ContentPath;
                    iconReset = gamePath + @"\Textures\GUI\Icons\Actions\Reset.dds";
                    iconIncrease = gamePath + @"\Textures\GUI\Icons\Actions\Increase.dds";
                    iconDecrease = gamePath + @"\Textures\GUI\Icons\Actions\Decrease.dds";
                }

                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Reset");
                    a.Name = new StringBuilder("Default ").Append(name);
                    if (!gridSizeDefaultValue)
                        a.Name.Append(" (").Append(defaultValue.ToString("0.###")).Append(")");
                    a.Icon = iconReset;
                    a.ValidForGroups = true;
                    a.Action = (b) => c.Setter(b, gridSizeDefaultValue ? b.CubeGrid.GridSize : defaultValue);
                    a.Writer = (b, s) => s.Append(c.Getter(b));

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Increase");
                    a.Name = new StringBuilder("Increase ").Append(name).Append(" (+").Append(modifier.ToString("0.###")).Append(")");
                    a.Icon = iconIncrease;
                    a.ValidForGroups = true;
                    a.Action = ActionAddDamageMod;
                    a.Writer = (b, s) => s.Append(c.Getter(b));

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
                {
                    var a = MyAPIGateway.TerminalControls.CreateAction<T>(id + "_Decrease");
                    a.Name = new StringBuilder("Decrease ").Append(name).Append(" (-").Append(modifier.ToString("0.###")).Append(")");
                    a.Icon = iconDecrease;
                    a.ValidForGroups = true;
                    a.Action = ActionSubtractDamageMod;
                    a.Writer = (b, s) => s.Append(c.Getter(b).ToString("0.###"));

                    MyAPIGateway.TerminalControls.AddAction<T>(a);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in CreateActionDamageModRate: {ex}"); }
        }

        private void ActionAddDamageMod(IMyTerminalBlock b)
        {
            try
            {
                List<IMyTerminalControl> controls;
                MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
                var damageMod = controls.First((x) => x.Id.ToString() == "DS-M_DamageModulation");
                var c = (IMyTerminalControlSlider)damageMod;
                if (c.Getter(b) > 179)
                {
                    c.Setter(b, 180f);
                    return;
                }
                c.Setter(b, c.Getter(b) + 1f);
            }
            catch (Exception ex) { Log.Line($"Exception in ActionAddDamageMod: {ex}"); }
        }

        private void ActionSubtractDamageMod(IMyTerminalBlock b)
        {
            try
            {
                List<IMyTerminalControl> controls;
                MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
                var chargeRate = controls.First((x) => x.Id.ToString() == "DS-M_DamageModulation");
                var c = (IMyTerminalControlSlider)chargeRate;
                if (c.Getter(b) < 21)
                {
                    c.Setter(b, 20f);
                    return;
                }
                c.Setter(b, c.Getter(b) - 1f);
            }
            catch (Exception ex) { Log.Line($"Exception in ActionSubtractDamageMod: {ex}"); }
        }

        private void CreateActionCombobox<T>(IMyTerminalControlCombobox c,
            string[] itemIds = null,
            string[] itemNames = null,
            string icon = null)
        {
            var items = new List<MyTerminalControlComboBoxItem>();
            c.ComboBoxContent.Invoke(items);

            foreach (var item in items)
            {
                var id = itemIds == null ? item.Value.String : itemIds[item.Key];

                if (id == null)
                    continue; // item id is null intentionally in the array, this means "don't add action".

                var a = MyAPIGateway.TerminalControls.CreateAction<T>(id);
                a.Name = new StringBuilder(itemNames == null ? item.Value.String : itemNames[item.Key]);
                if (icon != null)
                    a.Icon = icon;
                a.ValidForGroups = true;
                a.Action = (b) => c.Setter(b, item.Key);
                //if(writer != null)
                //    a.Writer = writer;

                MyAPIGateway.TerminalControls.AddAction<T>(a);
            }
        }
        #endregion
    }
}
