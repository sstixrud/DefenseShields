using System;
using System.Collections.Generic;
using System.IO;
using DefenseShields.Support;
using ProtoBuf;
using Sandbox.Game;
using VRage.Input;
using Sandbox.ModAPI;
using VRageMath;

namespace DefenseShields
{
    public class ShieldSettings
    {
        internal readonly VersionControl VersionControl;
        internal ClientSettings ClientConfig;
        internal Session Session;
        internal bool ClientWaiting;
        internal ShieldSettings(Session session)
        {
            Session = session;
            VersionControl = new VersionControl(this);
            VersionControl.InitSettings();
            if (!Session.IsServer)
                ClientWaiting = true;
        }

        [ProtoContract]
        public class ClientSettings
        {
            [ProtoMember(1)] public int Version = -1;
            [ProtoMember(2)] public string ActionKey = MyKeys.NumPad0.ToString();
            [ProtoMember(3)] public string NoShunting = MyKeys.NumPad5.ToString();
            [ProtoMember(4)] public string Left = MyKeys.NumPad4.ToString();
            [ProtoMember(5)] public string Right = MyKeys.NumPad6.ToString();
            [ProtoMember(6)] public string Up = MyKeys.NumPad9.ToString();
            [ProtoMember(7)] public string Down = MyKeys.NumPad3.ToString();
            [ProtoMember(8)] public string Front = MyKeys.NumPad8.ToString();
            [ProtoMember(9)] public string Back = MyKeys.NumPad2.ToString();
            [ProtoMember(10)] public Vector2D ShieldIconPos = new Vector2D(-0.46, -0.85);
            [ProtoMember(11)] public float HudScale = 0.82f;
            [ProtoMember(12)] public string Kinetic = MyKeys.NumPad7.ToString();
            [ProtoMember(13)] public string Energy = MyKeys.NumPad1.ToString();
            [ProtoMember(14)] public bool Notices = true;
            [ProtoMember(15)] public bool DisableKeys = true;
            [ProtoMember(16)] public int MaxHitRings = 7;
            [ProtoMember(17)] public bool ShowHitRings = true;

            internal void UpdateKey(MyKeys key, string value, UiInput uiInput)
            {
                var keyString = key.ToString();
                switch (value)
                {
                    case "kinetic":
                        Energy = keyString;
                        uiInput.Kinetic = key;
                        break;
                    case "energy":
                        Energy = keyString;
                        uiInput.Energy = key;
                        break;
                    case "action":
                        ActionKey = keyString;
                        uiInput.ActionKey = key;
                        break;
                    case "noshunt":
                        NoShunting = keyString;
                        uiInput.Shunting = key;
                        break;
                    case "left":
                        Left = keyString;
                        uiInput.Left = key;
                        break;
                    case "right":
                        Right = keyString;
                        uiInput.Right = key;
                        break;
                    case "front":
                        Front = keyString;
                        uiInput.Front = key;
                        break;
                    case "back":
                        Back = keyString;
                        uiInput.Back = key;
                        break;
                    case "up":
                        Up = keyString;
                        uiInput.Up = key;
                        break;
                    case "down":
                        Down = keyString;
                        uiInput.Down = key;
                        break;
                }
            }
        }
    }

    internal class VersionControl
    {
        public ShieldSettings Core;
        public bool VersionChange;
        public VersionControl(ShieldSettings core)
        {
            Core = core;
        }

        public void InitSettings()
        {
            if (MyAPIGateway.Utilities.FileExistsInGlobalStorage(Session.ClientCfgName))
            {

                var writer = MyAPIGateway.Utilities.ReadFileInGlobalStorage(Session.ClientCfgName);
                var xmlData = MyAPIGateway.Utilities.SerializeFromXML<ShieldSettings.ClientSettings>(writer.ReadToEnd());
                writer.Dispose();

                if (xmlData?.Version == Session.ClientCfgVersion)
                {
                    Core.ClientConfig = xmlData;
                    InitKeys(xmlData);
                }
                else
                    WriteNewClientCfg(xmlData);
            }
            else WriteNewClientCfg();

            if (VersionChange)
            {
                Core.Session.PlayerMessage = "You may access DefenseShield client settings with the /ds chat command";
            }
        }

        private void WriteNewClientCfg(ShieldSettings.ClientSettings oldSettings = null)
        {
            VersionChange = true;
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage(Session.ClientCfgName);
            Core.ClientConfig = new ShieldSettings.ClientSettings { Version = Session.ClientCfgVersion };
            
            RetainSettings(Core.ClientConfig, oldSettings);
            InitKeys(Core.ClientConfig);

            var writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(Session.ClientCfgName);
            var data = MyAPIGateway.Utilities.SerializeToXML(Core.ClientConfig);

            Write(writer, data);
        }

        internal void UpdateClientCfgFile()
        {
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage(Session.ClientCfgName);
            var writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(Session.ClientCfgName);
            var data = MyAPIGateway.Utilities.SerializeToXML(Core.ClientConfig);
            Write(writer, data);
        }

        private static void Write(TextWriter writer, string data)
        {
            writer.Write(data);
            writer.Flush();
            writer.Dispose();
        }

        private static void RetainSettings(ShieldSettings.ClientSettings newSettings, ShieldSettings.ClientSettings oldSettings)
        {
            if (oldSettings == null || newSettings == null)
                return;

            newSettings.Left = oldSettings.Left;
            newSettings.Right = oldSettings.Right;
            newSettings.Up = oldSettings.Up;
            newSettings.Down = oldSettings.Down;
            newSettings.Front = oldSettings.Front;
            newSettings.Back = oldSettings.Back;

            newSettings.NoShunting = oldSettings.NoShunting;
            newSettings.ActionKey = oldSettings.ActionKey;

            newSettings.Notices = oldSettings.Notices;

            newSettings.DisableKeys = oldSettings.DisableKeys;

            newSettings.Energy = oldSettings.Energy;
            newSettings.Kinetic = oldSettings.Kinetic;

            newSettings.HudScale = oldSettings.HudScale;
            newSettings.ShieldIconPos = oldSettings.ShieldIconPos;

            newSettings.MaxHitRings = oldSettings.MaxHitRings;
            newSettings.ShowHitRings = oldSettings.ShowHitRings;

            if (oldSettings.Version <= 11)
            {
                newSettings.HudScale = 0.82f;
                newSettings.ShieldIconPos = new Vector2D(-0.46, -0.85);
            }
        }

        private void InitKeys(ShieldSettings.ClientSettings data)
        {
            Core.Session.UiInput.ActionKey = Core.Session.KeyMap[data.ActionKey];
            Core.Session.UiInput.Shunting = Core.Session.KeyMap[data.NoShunting];

            Core.Session.UiInput.Left = Core.Session.KeyMap[data.Left];
            Core.Session.UiInput.Right = Core.Session.KeyMap[data.Right];
            Core.Session.UiInput.Front = Core.Session.KeyMap[data.Front];
            Core.Session.UiInput.Back = Core.Session.KeyMap[data.Back];
            Core.Session.UiInput.Up = Core.Session.KeyMap[data.Up];
            Core.Session.UiInput.Down = Core.Session.KeyMap[data.Down];

            Core.Session.UiInput.Kinetic = Core.Session.KeyMap[data.Kinetic];
            Core.Session.UiInput.Energy = Core.Session.KeyMap[data.Energy];
        }
    }

    internal class UiInput
    {
        internal int PreviousWheel;
        internal int CurrentWheel;
        internal int ShiftTime;
        internal int ShuntKeyTime;
        internal bool MouseButtonPressed;
        internal bool InputChanged;
        internal bool MouseButtonLeftWasPressed;
        internal bool MouseButtonMiddleWasPressed;
        internal bool MouseButtonRightWasPressed;
        internal bool MouseButtonLeftIsPressed;
        internal bool MouseButtonMiddleIsPressed;
        internal bool MouseButtonRightIsPressed;
        internal bool WasInMenu;
        internal bool WheelForward;
        internal bool WheelBackward;
        internal bool ShiftReleased;
        internal bool ShiftPressed;
        internal bool LongShift;
        internal bool LongShuntKey;
        internal bool AltPressed;
        internal bool ActionKeyPressed;
        internal bool ActionKeyReleased;
        internal bool CtrlPressed;
        internal bool AnyKeyPressed;
        internal bool KeyPrevPressed;
        internal bool UiKeyPressed;
        internal bool UiKeyWasPressed;
        internal bool ShuntKeyPressed;
        internal bool ShuntKeyWasPressed;
        internal bool PlayerCamera;
        internal bool FirstPersonView;
        internal bool Debug = true;
        internal bool BlackListActive1;
        internal bool BlackListActive2;
        internal bool NumPadNumKeyPressed;
        private readonly Session _session;
        internal MyKeys ActionKey;
        internal MyKeys Shunting;
        internal MyKeys Left;
        internal MyKeys Right;
        internal MyKeys Front;
        internal MyKeys Back;
        internal MyKeys Up;
        internal MyKeys Down;
        internal MyKeys Kinetic;
        internal MyKeys Energy;
        internal bool ShuntReleased;
        internal bool LeftReleased;
        internal bool RightReleased;
        internal bool FrontReleased;
        internal bool BackReleased;
        internal bool UpReleased;
        internal bool DownReleased;
        internal bool KineticReleased;
        internal bool EnergyReleased;


        internal readonly List<MyKeys> NumPadNumbers = new List<MyKeys>()
        {
            MyKeys.NumPad0,
            MyKeys.NumPad1,
            MyKeys.NumPad2,
            MyKeys.NumPad3,
            MyKeys.NumPad4,
            MyKeys.NumPad5,
            MyKeys.NumPad6,
            MyKeys.NumPad7,
            MyKeys.NumPad8,
            MyKeys.NumPad9,
        };

        internal UiInput(Session session)
        {
            _session = session;
        }

        internal void UpdateInputState()
        {
            WheelForward = false;
            WheelBackward = false;

            if (!_session.InMenu)
            {
                MouseButtonPressed = MyAPIGateway.Input.IsAnyMousePressed();

                MouseButtonLeftWasPressed = MouseButtonLeftIsPressed;
                MouseButtonMiddleWasPressed = MouseButtonMiddleIsPressed;
                MouseButtonRightWasPressed = MouseButtonRightIsPressed;

                WasInMenu = _session.InMenu;

                if (MouseButtonPressed)
                {
                    MouseButtonLeftIsPressed = MyAPIGateway.Input.IsMousePressed(MyMouseButtonsEnum.Left);
                    MouseButtonMiddleIsPressed = MyAPIGateway.Input.IsMousePressed(MyMouseButtonsEnum.Middle);
                    MouseButtonRightIsPressed = MyAPIGateway.Input.IsMousePressed(MyMouseButtonsEnum.Right);
                }
                else
                {
                    MouseButtonLeftIsPressed = false;
                    MouseButtonMiddleIsPressed = false;
                    MouseButtonRightIsPressed = false;
                }

                if (_session.MpActive)
                {
                }

                ShiftReleased = MyAPIGateway.Input.IsNewKeyReleased(MyKeys.LeftShift);
                ShiftPressed = MyAPIGateway.Input.IsKeyPress(MyKeys.LeftShift);
                ActionKeyReleased = MyAPIGateway.Input.IsNewKeyReleased(ActionKey);

                
                if (ShiftPressed)
                {
                    ShiftTime++;
                    LongShift = ShiftTime > 59;
                }
                else
                {
                    if (LongShift) ShiftReleased = false;
                    ShiftTime = 0;
                    LongShift = false;
                }

                AltPressed = MyAPIGateway.Input.IsAnyAltKeyPressed();
                CtrlPressed = MyAPIGateway.Input.IsKeyPress(MyKeys.Control);
                KeyPrevPressed = AnyKeyPressed;
                AnyKeyPressed = MyAPIGateway.Input.IsAnyKeyPress();
                UiKeyWasPressed = UiKeyPressed;
                UiKeyPressed = CtrlPressed || AltPressed || ShiftPressed;
                PlayerCamera = MyAPIGateway.Session.IsCameraControlledObject;
                FirstPersonView = PlayerCamera && MyAPIGateway.Session.CameraController.IsInFirstPersonView;

                ShuntKeyWasPressed = ShuntKeyPressed;

                if (AnyKeyPressed) {

                    ShuntKeyPressed = MyAPIGateway.Input.IsKeyPress(Left) || MyAPIGateway.Input.IsKeyPress(Right) || MyAPIGateway.Input.IsKeyPress(Front) || MyAPIGateway.Input.IsKeyPress(Back) || MyAPIGateway.Input.IsKeyPress(Up) || MyAPIGateway.Input.IsKeyPress(Down);
                    
                    if (ShuntKeyPressed) {
                        ShuntKeyTime++;
                        LongShuntKey = ShuntKeyTime > 39;
                    }
                    else 
                        ShuntKeyTime = 0;
                }
                else {
                    ShuntKeyPressed = false;
                }

                if (!ShuntKeyPressed && !ShuntKeyWasPressed) { 
                    ShuntKeyTime = 0;
                    LongShuntKey = false;
                }
                
                if (KeyPrevPressed) {

                    LeftReleased = MyAPIGateway.Input.IsNewKeyReleased(Left);
                    RightReleased = MyAPIGateway.Input.IsNewKeyReleased(Right);
                    FrontReleased = MyAPIGateway.Input.IsNewKeyReleased(Front);
                    BackReleased = MyAPIGateway.Input.IsNewKeyReleased(Back);
                    UpReleased = MyAPIGateway.Input.IsNewKeyReleased(Up);
                    DownReleased = MyAPIGateway.Input.IsNewKeyReleased(Down);

                    ShuntReleased = MyAPIGateway.Input.IsNewKeyReleased(Shunting);

                    KineticReleased = MyAPIGateway.Input.IsNewKeyReleased(Kinetic);
                    EnergyReleased = MyAPIGateway.Input.IsNewKeyReleased(Energy);

                }
                else {

                    LeftReleased = false;
                    RightReleased = false;
                    FrontReleased = false;
                    BackReleased = false;
                    UpReleased = false;
                    DownReleased = false;
                    ShuntReleased = false;
                    KineticReleased = false;
                    EnergyReleased = false;
                }

                if ((!UiKeyPressed && !UiKeyWasPressed) || !AltPressed && CtrlPressed && !FirstPersonView)
                {
                    PreviousWheel = MyAPIGateway.Input.PreviousMouseScrollWheelValue();
                    CurrentWheel = MyAPIGateway.Input.MouseScrollWheelValue();
                }

                ActionKeyPressed = MyAPIGateway.Input.IsKeyPress(ActionKey);
                NumPadNumKeyPressed = DirectionKeyPressed();
                if (NumPadNumKeyPressed && !BlackListActive2 && !_session.Settings.ClientConfig.DisableKeys)
                    BlackList2(true);

                if (ActionKeyPressed && _session.CanChangeHud) {

                    if (!BlackListActive1)
                        BlackList1(true);

                    var evenTicks = _session.Tick % 2 == 0;
                    if (evenTicks) {

                        if (MyAPIGateway.Input.IsKeyPress(MyKeys.Up)) {
                            _session.Settings.ClientConfig.ShieldIconPos.Y += 0.01;
                            _session.Settings.VersionControl.UpdateClientCfgFile();
                        }
                        else if (MyAPIGateway.Input.IsKeyPress(MyKeys.Down)) {
                            _session.Settings.ClientConfig.ShieldIconPos.Y -= 0.01;
                            _session.Settings.VersionControl.UpdateClientCfgFile();
                        }
                        else if (MyAPIGateway.Input.IsKeyPress(MyKeys.Left)) {
                            _session.Settings.ClientConfig.ShieldIconPos.X -= 0.01;
                            _session.Settings.VersionControl.UpdateClientCfgFile();
                        }
                        else if (MyAPIGateway.Input.IsKeyPress(MyKeys.Right)) {
                            _session.Settings.ClientConfig.ShieldIconPos.X += 0.01;
                            _session.Settings.VersionControl.UpdateClientCfgFile();
                        }
                    }

                    if (_session.Tick10) {
                        if (MyAPIGateway.Input.IsKeyPress(MyKeys.Add)) {
                            _session.Settings.ClientConfig.HudScale = MathHelper.Clamp(_session.Settings.ClientConfig.HudScale + 0.01f, 0.1f, 10f);
                            _session.Settings.VersionControl.UpdateClientCfgFile();
                        }
                        else if (MyAPIGateway.Input.IsKeyPress(MyKeys.Subtract)) {
                            _session.Settings.ClientConfig.HudScale = MathHelper.Clamp(_session.Settings.ClientConfig.HudScale - 0.01f, 0.1f, 10f);
                            _session.Settings.VersionControl.UpdateClientCfgFile();
                        }
                    }
                }
            }
            else
            {
                KeyPrevPressed = AnyKeyPressed;
                AnyKeyPressed = false;
                LeftReleased = false;
                RightReleased = false;
                FrontReleased = false;
                BackReleased = false;
                UpReleased = false;
                DownReleased = false;
                ShuntReleased = false;
                KineticReleased = false;
                EnergyReleased = false;
                ShuntKeyTime = 0;
                LongShuntKey = false;
            }

            if (!ActionKeyPressed && BlackListActive1)
                BlackList1(false);

            if (!NumPadNumKeyPressed && BlackListActive2)
                BlackList2(false);

            if (_session.MpActive)
            {
                InputChanged = true;
            }

            if (CurrentWheel != PreviousWheel && CurrentWheel > PreviousWheel)
                WheelForward = true;
            else if (CurrentWheel != PreviousWheel)
                WheelBackward = true;
        }

        private void BlackList1(bool activate)
        {
            try
            {
                var upKey = MyAPIGateway.Input.GetControl(MyKeys.Up);
                var downKey = MyAPIGateway.Input.GetControl(MyKeys.Down);
                var leftKey = MyAPIGateway.Input.GetControl(MyKeys.Left);
                var rightkey = MyAPIGateway.Input.GetControl(MyKeys.Right);
                var addKey = MyAPIGateway.Input.GetControl(MyKeys.Add);
                var subKey = MyAPIGateway.Input.GetControl(MyKeys.Subtract);

                if (upKey != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(upKey.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (downKey != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(downKey.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (leftKey != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(leftKey.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (rightkey != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(rightkey.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (addKey != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(addKey.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (subKey != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(subKey.GetGameControlEnum().String, _session.PlayerId, !activate);
                }

                BlackListActive1 = activate;
            }
            catch (Exception ex) { Log.Line($"Exception in BlackList1: {ex}"); }
        }

        private void BlackList2(bool activate)
        {
            try
            {
                var numPad0 = MyAPIGateway.Input.GetControl(MyKeys.NumPad0);
                var numPad1 = MyAPIGateway.Input.GetControl(MyKeys.NumPad1);
                var numPad2 = MyAPIGateway.Input.GetControl(MyKeys.NumPad2);
                var numPad3 = MyAPIGateway.Input.GetControl(MyKeys.NumPad3);
                var numPad4 = MyAPIGateway.Input.GetControl(MyKeys.NumPad4);
                var numPad5 = MyAPIGateway.Input.GetControl(MyKeys.NumPad5);
                var numPad6 = MyAPIGateway.Input.GetControl(MyKeys.NumPad6);
                var numPad7 = MyAPIGateway.Input.GetControl(MyKeys.NumPad7);
                var numPad8 = MyAPIGateway.Input.GetControl(MyKeys.NumPad8);
                var numPad9 = MyAPIGateway.Input.GetControl(MyKeys.NumPad9);

                if (numPad0 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad0.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (numPad1 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad1.GetGameControlEnum().String, _session.PlayerId, !activate);

                }
                if (numPad2 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad2.GetGameControlEnum().String, _session.PlayerId, !activate);

                }
                if (numPad3 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad3.GetGameControlEnum().String, _session.PlayerId, !activate);

                }
                if (numPad4 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad4.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (numPad5 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad5.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (numPad6 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad6.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (numPad7 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad7.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (numPad8 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad8.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                if (numPad9 != null)
                {
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(numPad9.GetGameControlEnum().String, _session.PlayerId, !activate);
                }
                BlackListActive2 = activate;
            }
            catch (Exception ex) { Log.Line($"Exception in BlackList2: {ex}"); }
        }

        private bool DirectionKeyPressed()
        {
            if (AnyKeyPressed)
            {
                foreach (var num in NumPadNumbers) {
                    if (MyAPIGateway.Input.IsKeyPress(num))
                        return true;
                }
            }

            return false;
        }
    }
}
