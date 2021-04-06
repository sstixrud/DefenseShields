using System;
using System.IO;
using System.Runtime.InteropServices;
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
            [ProtoMember(10)] public Vector2D ShieldIconPos = new Vector2D(-0.92, -0.80);
            [ProtoMember(11)] public float HudScale = 1f;

            internal void UpdateKey(MyKeys key, string value, UiInput uiInput)
            {
                var keyString = key.ToString();
                switch (value)
                {
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
                    Core.Session.UiInput.ActionKey = Core.Session.KeyMap[xmlData.ActionKey];

                    Core.Session.UiInput.Shunting = Core.Session.KeyMap[xmlData.NoShunting];

                    Core.Session.UiInput.Left = Core.Session.KeyMap[xmlData.Left];
                    Core.Session.UiInput.Right = Core.Session.KeyMap[xmlData.Right];
                    Core.Session.UiInput.Front = Core.Session.KeyMap[xmlData.Front];
                    Core.Session.UiInput.Back = Core.Session.KeyMap[xmlData.Back];
                    Core.Session.UiInput.Up = Core.Session.KeyMap[xmlData.Up];
                    Core.Session.UiInput.Down = Core.Session.KeyMap[xmlData.Down];
                }
                else
                    WriteNewClientCfg();
            }
            else WriteNewClientCfg();

            if (VersionChange)
            {
                Core.Session.PlayerMessage = "You may access DefenseShield client settings with the /ds chat command";
            }
        }

        private void WriteNewClientCfg()
        {
            VersionChange = true;
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage(Session.ClientCfgName);
            Core.ClientConfig = new ShieldSettings.ClientSettings { Version = Session.ClientCfgVersion };
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
    }

    internal class UiInput
    {
        internal int PreviousWheel;
        internal int CurrentWheel;
        internal int ShiftTime;
        internal int KeyTime;
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
        internal bool LongKey;
        internal bool AltPressed;
        internal bool ActionKeyPressed;
        internal bool ActionKeyReleased;
        internal bool CtrlPressed;
        internal bool AnyKeyPressed;
        internal bool KeyPrevPressed;
        internal bool UiKeyPressed;
        internal bool UiKeyWasPressed;
        internal bool PlayerCamera;
        internal bool FirstPersonView;
        internal bool Debug = true;
        internal bool BlackListActive;
        private readonly Session _session;
        internal MyKeys ActionKey;
        internal MyKeys Shunting;
        internal MyKeys Left;
        internal MyKeys Right;
        internal MyKeys Front;
        internal MyKeys Back;
        internal MyKeys Up;
        internal MyKeys Down;

        internal bool ShuntReleased;
        internal bool LeftReleased;
        internal bool RightReleased;
        internal bool FrontReleased;
        internal bool BackReleased;
        internal bool UpReleased;
        internal bool DownReleased;

        internal UiInput(Session session)
        {
            _session = session;
        }

        internal void UpdateInputState()
        {
            var s = _session;
            WheelForward = false;
            WheelBackward = false;

            if (!s.InMenu)
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

                if (AnyKeyPressed) {
                    KeyTime++;
                    LongKey = KeyTime > 39;
                }

                if (KeyPrevPressed) {

                    LeftReleased = MyAPIGateway.Input.IsNewKeyReleased(Left);
                    RightReleased = MyAPIGateway.Input.IsNewKeyReleased(Right);
                    FrontReleased = MyAPIGateway.Input.IsNewKeyReleased(Front);
                    BackReleased = MyAPIGateway.Input.IsNewKeyReleased(Back);
                    UpReleased = MyAPIGateway.Input.IsNewKeyReleased(Up);
                    DownReleased = MyAPIGateway.Input.IsNewKeyReleased(Down);
                    ShuntReleased = MyAPIGateway.Input.IsNewKeyReleased(Shunting);
                }
                else {

                    LeftReleased = false;
                    RightReleased = false;
                    FrontReleased = false;
                    BackReleased = false;
                    UpReleased = false;
                    DownReleased = false;
                    ShuntReleased = false;
                    KeyTime = 0;
                    LongKey = false;
                }

                if ((!UiKeyPressed && !UiKeyWasPressed) || !AltPressed && CtrlPressed && !FirstPersonView)
                {
                    PreviousWheel = MyAPIGateway.Input.PreviousMouseScrollWheelValue();
                    CurrentWheel = MyAPIGateway.Input.MouseScrollWheelValue();
                }

                ActionKeyPressed = MyAPIGateway.Input.IsKeyPress(ActionKey);

                if (ActionKeyPressed) {

                    if (!BlackListActive)
                        BlackList(true);

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
                KeyTime = 0;
                LongKey = false;
            }

            if (!ActionKeyPressed && BlackListActive)
                BlackList(false);

            if (_session.MpActive)
            {
                InputChanged = true;
            }

            if (CurrentWheel != PreviousWheel && CurrentWheel > PreviousWheel)
                WheelForward = true;
            else if (CurrentWheel != PreviousWheel)
                WheelBackward = true;
        }

        private void BlackList(bool activate)
        {
            MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(MyAPIGateway.Input.GetControl(MyKeys.Up).GetGameControlEnum().String, MyAPIGateway.Session.Player.IdentityId, !activate);
            MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(MyAPIGateway.Input.GetControl(MyKeys.Down).GetGameControlEnum().String, MyAPIGateway.Session.Player.IdentityId, !activate);
            MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(MyAPIGateway.Input.GetControl(MyKeys.Left).GetGameControlEnum().String, MyAPIGateway.Session.Player.IdentityId, !activate);
            MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(MyAPIGateway.Input.GetControl(MyKeys.Right).GetGameControlEnum().String, MyAPIGateway.Session.Player.IdentityId, !activate);
            MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(MyAPIGateway.Input.GetControl(MyKeys.Add).GetGameControlEnum().String, MyAPIGateway.Session.Player.IdentityId, !activate);
            MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(MyAPIGateway.Input.GetControl(MyKeys.Subtract).GetGameControlEnum().String, MyAPIGateway.Session.Player.IdentityId, !activate);
            BlackListActive = activate;
        }
    }
}
