using Gtk;
using OpenTK.Input;
using Ryujinx.Common;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Utilities;
using Ryujinx.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;

using Key = Ryujinx.Configuration.Hid.Key;

namespace Ryujinx.Ui
{
    public class ControllerWindow : Window
    {
        private readonly GtkUserInterface _gtkUserInterface;
        private readonly PlayerIndex      _playerIndex;
        private readonly InputConfig      _inputConfig;

        private bool _isWaitingForInput;

#pragma warning disable CS0649, IDE0044
        [Builder.Object] Adjustment   _controllerDeadzoneLeft;
        [Builder.Object] Adjustment   _controllerDeadzoneRight;
        [Builder.Object] Adjustment   _controllerTriggerThreshold;
        [Builder.Object] ComboBoxText _inputDevice;
        [Builder.Object] ComboBoxText _profile;
        [Builder.Object] Button       _loadProfile;
        [Builder.Object] Button       _addProfile;
        [Builder.Object] Button       _removeProfile;
        [Builder.Object] Button       _refreshInputDevicesButton;
        [Builder.Object] Box          _settingsBox;
        [Builder.Object] Grid         _leftStickKeyboard;
        [Builder.Object] Grid         _leftStickController;
        [Builder.Object] Box          _deadZoneLeftBox;
        [Builder.Object] Grid         _rightStickKeyboard;
        [Builder.Object] Grid         _rightStickController;
        [Builder.Object] Box          _deadZoneRightBox;
        [Builder.Object] Grid         _leftSideTriggerBox;
        [Builder.Object] Grid         _rightSideTriggerBox;
        [Builder.Object] Box          _triggerThresholdBox;
        [Builder.Object] ComboBoxText _controllerType;
        [Builder.Object] ToggleButton _lStickX;
        [Builder.Object] CheckButton  _invertLStickX;
        [Builder.Object] ToggleButton _lStickY;
        [Builder.Object] CheckButton  _invertLStickY;
        [Builder.Object] ToggleButton _lStickUp;
        [Builder.Object] ToggleButton _lStickDown;
        [Builder.Object] ToggleButton _lStickLeft;
        [Builder.Object] ToggleButton _lStickRight;
        [Builder.Object] ToggleButton _lStickButton;
        [Builder.Object] ToggleButton _dpadUp;
        [Builder.Object] ToggleButton _dpadDown;
        [Builder.Object] ToggleButton _dpadLeft;
        [Builder.Object] ToggleButton _dpadRight;
        [Builder.Object] ToggleButton _minus;
        [Builder.Object] ToggleButton _l;
        [Builder.Object] ToggleButton _zL;
        [Builder.Object] ToggleButton _rStickX;
        [Builder.Object] CheckButton  _invertRStickX;
        [Builder.Object] ToggleButton _rStickY;
        [Builder.Object] CheckButton  _invertRStickY;
        [Builder.Object] ToggleButton _rStickUp;
        [Builder.Object] ToggleButton _rStickDown;
        [Builder.Object] ToggleButton _rStickLeft;
        [Builder.Object] ToggleButton _rStickRight;
        [Builder.Object] ToggleButton _rStickButton;
        [Builder.Object] ToggleButton _a;
        [Builder.Object] ToggleButton _b;
        [Builder.Object] ToggleButton _x;
        [Builder.Object] ToggleButton _y;
        [Builder.Object] ToggleButton _plus;
        [Builder.Object] ToggleButton _r;
        [Builder.Object] ToggleButton _zR;
        [Builder.Object] ToggleButton _lSl;
        [Builder.Object] ToggleButton _lSr;
        [Builder.Object] ToggleButton _rSl;
        [Builder.Object] ToggleButton _rSr;
        [Builder.Object] Image        _controllerImage;
        [Builder.Object] Button       _saveButton;
        [Builder.Object] Button       _closeButton;
#pragma warning restore CS0649, IDE0044

        public ControllerWindow(GtkUserInterface gtkUserInterface, PlayerIndex controllerId) 
            : this(new Builder("Ryujinx.Ui.ControllerWindow.glade"), gtkUserInterface, controllerId) { }

        private ControllerWindow(Builder builder, GtkUserInterface gtkUserInterface, PlayerIndex controllerId) 
            : base(builder.GetObject("ControllerWindow").Handle)
        {
            builder.Autoconnect(this);

            _gtkUserInterface  = gtkUserInterface;
            _playerIndex       = controllerId;
            _inputConfig       = ConfigurationState.Instance.Hid.InputConfig.Value.Find(inputConfig => inputConfig.PlayerIndex == _playerIndex);

            this.Icon = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.Icon.png");

            _inputDevice.Changed               += InputDevice_Changed;
            _refreshInputDevicesButton.Clicked += RefreshInputDevicesButton_Clicked;
            _controllerType.Changed            += Controller_Changed;
            _loadProfile.Clicked               += ProfileLoad_Clicked;
            _addProfile.Clicked                += ProfileAdd_Clicked;
            _removeProfile.Clicked             += ProfileRemove_Clicked;
            _lStickX.Clicked                   += Button_Clicked;
            _lStickY.Clicked                   += Button_Clicked;
            _lStickUp.Clicked                  += Button_Clicked;
            _lStickDown.Clicked                += Button_Clicked;
            _lStickLeft.Clicked                += Button_Clicked;
            _lStickRight.Clicked               += Button_Clicked;
            _lStickButton.Clicked              += Button_Clicked;
            _dpadUp.Clicked                    += Button_Clicked;
            _dpadDown.Clicked                  += Button_Clicked;
            _dpadLeft.Clicked                  += Button_Clicked;
            _dpadRight.Clicked                 += Button_Clicked;
            _minus.Clicked                     += Button_Clicked;
            _l.Clicked                         += Button_Clicked;
            _zL.Clicked                        += Button_Clicked;
            _lSl.Clicked                       += Button_Clicked;
            _lSr.Clicked                       += Button_Clicked;
            _rStickX.Clicked                   += Button_Clicked;
            _rStickY.Clicked                   += Button_Clicked;
            _rStickUp.Clicked                  += Button_Clicked;
            _rStickDown.Clicked                += Button_Clicked;
            _rStickLeft.Clicked                += Button_Clicked;
            _rStickRight.Clicked               += Button_Clicked;
            _rStickButton.Clicked              += Button_Clicked;
            _a.Clicked                         += Button_Clicked;
            _b.Clicked                         += Button_Clicked;
            _x.Clicked                         += Button_Clicked;
            _y.Clicked                         += Button_Clicked;
            _plus.Clicked                      += Button_Clicked;
            _r.Clicked                         += Button_Clicked;
            _zR.Clicked                        += Button_Clicked;
            _rSl.Clicked                       += Button_Clicked;
            _rSr.Clicked                       += Button_Clicked;
            _saveButton.Clicked                += SaveButton_Clicked;
            _closeButton.Clicked               += (sender, args) => this.Dispose();

            UpdateInputDeviceList();
            SetAvailableOptions();

            ClearValues();
            if (_inputDevice.ActiveId != null) SetCurrentValues();
        }

        private void UpdateInputDeviceList()
        {
            _inputDevice.RemoveAll();
            _inputDevice.Append("disabled", "Disabled");
            _inputDevice.SetActiveId("disabled");

            _inputDevice.Append($"keyboard/{KeyboardConfig.AllKeyboardsIndex}", "All keyboards");

            for (int i = 0; i < 20; i++)
            {
                if (KeyboardController.GetKeyboardState(i + 1).IsConnected)
                    _inputDevice.Append($"keyboard/{i + 1}", $"Keyboard/{i + 1}");

                if (GamePad.GetState(i).IsConnected)
                    _inputDevice.Append($"controller/{i}", $"Controller/{i} ({GamePad.GetName(i)})");
            }

            switch (_inputConfig)
            {
                case KeyboardConfig keyboard:
                    _inputDevice.SetActiveId($"keyboard/{keyboard.Index}");
                    break;
                case ControllerConfig controller:
                    _inputDevice.SetActiveId($"controller/{controller.Index}");
                    break;
            }
        }

        private void SetAvailableOptions()
        {
            if (_inputDevice.ActiveId != null && _inputDevice.ActiveId.StartsWith("keyboard"))
            {
                this.ShowAll();
                _leftStickController.Hide();
                _rightStickController.Hide();
                _deadZoneLeftBox.Hide();
                _deadZoneRightBox.Hide();
                _triggerThresholdBox.Hide();
            }
            else if (_inputDevice.ActiveId != null && _inputDevice.ActiveId.StartsWith("controller"))
            {
                this.ShowAll();
                _leftStickKeyboard.Hide();
                _rightStickKeyboard.Hide();
            }
            else
            {
                _settingsBox.Hide();
            }

            ClearValues();
        }

        private void SetCurrentValues()
        {
            SetControllerSpecificFields();

            SetProfiles();

            if (_inputDevice.ActiveId.StartsWith("keyboard") && _inputConfig is KeyboardConfig)
            {
                SetValues(_inputConfig);
            }
            else if (_inputDevice.ActiveId.StartsWith("controller") && _inputConfig is ControllerConfig)
            {
                SetValues(_inputConfig);
            }
        }

        private void SetControllerSpecificFields()
        {
            _leftSideTriggerBox.Hide();
            _rightSideTriggerBox.Hide();

            switch (_controllerType.ActiveId)
            {
                case "JoyconLeft":
                    _leftSideTriggerBox.Show();
                    break;
                case "JoyconRight":
                    _rightSideTriggerBox.Show();
                    break;
            }

            _controllerImage.Pixbuf = _controllerType.ActiveId switch
            {
                "ProController" => new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.ProCon.svg",      400, 400),
                "JoyconLeft"    => new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.JoyConLeft.svg",  400, 400),
                "JoyconRight"   => new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.JoyConRight.svg", 400, 400),
                _               => new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.Ui.assets.JoyConPair.svg",  400, 400),
            };
        }

        private void ClearValues()
        {
            _lStickX.Label = _lStickY.Label = _lStickUp.Label = _lStickDown.Label = _lStickLeft.Label = _lStickRight.Label = _lStickButton.Label =
            _dpadUp.Label = _dpadDown.Label = _dpadLeft.Label = _dpadRight.Label = _minus.Label = _l.Label = _zL.Label = _lSl.Label = _lSr.Label =
            _rStickX.Label = _rStickY.Label = _rStickUp.Label = _rStickDown.Label = _rStickLeft.Label = _rStickRight.Label = _rStickButton.Label =
            _a.Label = _b.Label = _x.Label = _y.Label = _plus.Label = _r.Label = _zR.Label = _rSl.Label = _rSr.Label                             = "Unbound";

            _controllerDeadzoneLeft.Value = _controllerDeadzoneRight.Value = _controllerTriggerThreshold.Value = 0;
        }

        private void SetValues(InputConfig config)
        {
            switch (config)
            {
                case KeyboardConfig keyboardConfig:
                    _controllerType.SetActiveId(keyboardConfig.ControllerType.ToString());

                    _lStickUp.Label     = keyboardConfig.LeftJoycon.StickUp.ToString();
                    _lStickDown.Label   = keyboardConfig.LeftJoycon.StickDown.ToString();
                    _lStickLeft.Label   = keyboardConfig.LeftJoycon.StickLeft.ToString();
                    _lStickRight.Label  = keyboardConfig.LeftJoycon.StickRight.ToString();
                    _lStickButton.Label = keyboardConfig.LeftJoycon.StickButton.ToString();
                    _dpadUp.Label       = keyboardConfig.LeftJoycon.DPadUp.ToString();
                    _dpadDown.Label     = keyboardConfig.LeftJoycon.DPadDown.ToString();
                    _dpadLeft.Label     = keyboardConfig.LeftJoycon.DPadLeft.ToString();
                    _dpadRight.Label    = keyboardConfig.LeftJoycon.DPadRight.ToString();
                    _minus.Label        = keyboardConfig.LeftJoycon.ButtonMinus.ToString();
                    _l.Label            = keyboardConfig.LeftJoycon.ButtonL.ToString();
                    _zL.Label           = keyboardConfig.LeftJoycon.ButtonZl.ToString();
                    _lSl.Label          = keyboardConfig.LeftJoycon.ButtonSl.ToString();
                    _lSr.Label          = keyboardConfig.LeftJoycon.ButtonSr.ToString();
                    _rStickUp.Label     = keyboardConfig.RightJoycon.StickUp.ToString();
                    _rStickDown.Label   = keyboardConfig.RightJoycon.StickDown.ToString();
                    _rStickLeft.Label   = keyboardConfig.RightJoycon.StickLeft.ToString();
                    _rStickRight.Label  = keyboardConfig.RightJoycon.StickRight.ToString();
                    _rStickButton.Label = keyboardConfig.RightJoycon.StickButton.ToString();
                    _a.Label            = keyboardConfig.RightJoycon.ButtonA.ToString();
                    _b.Label            = keyboardConfig.RightJoycon.ButtonB.ToString();
                    _x.Label            = keyboardConfig.RightJoycon.ButtonX.ToString();
                    _y.Label            = keyboardConfig.RightJoycon.ButtonY.ToString();
                    _plus.Label         = keyboardConfig.RightJoycon.ButtonPlus.ToString();
                    _r.Label            = keyboardConfig.RightJoycon.ButtonR.ToString();
                    _zR.Label           = keyboardConfig.RightJoycon.ButtonZr.ToString();
                    _rSl.Label          = keyboardConfig.RightJoycon.ButtonSl.ToString();
                    _rSr.Label          = keyboardConfig.RightJoycon.ButtonSr.ToString();
                    break;
                case ControllerConfig controllerConfig:
                    _controllerType.SetActiveId(controllerConfig.ControllerType.ToString());

                    _lStickX.Label                    = controllerConfig.LeftJoycon.StickX.ToString();
                    _invertLStickX.Active             = controllerConfig.LeftJoycon.InvertStickX;
                    _lStickY.Label                    = controllerConfig.LeftJoycon.StickY.ToString();
                    _invertLStickY.Active             = controllerConfig.LeftJoycon.InvertStickY;
                    _lStickButton.Label               = controllerConfig.LeftJoycon.StickButton.ToString();
                    _dpadUp.Label                     = controllerConfig.LeftJoycon.DPadUp.ToString();
                    _dpadDown.Label                   = controllerConfig.LeftJoycon.DPadDown.ToString();
                    _dpadLeft.Label                   = controllerConfig.LeftJoycon.DPadLeft.ToString();
                    _dpadRight.Label                  = controllerConfig.LeftJoycon.DPadRight.ToString();
                    _minus.Label                      = controllerConfig.LeftJoycon.ButtonMinus.ToString();
                    _l.Label                          = controllerConfig.LeftJoycon.ButtonL.ToString();
                    _zL.Label                         = controllerConfig.LeftJoycon.ButtonZl.ToString();
                    _lSl.Label                        = controllerConfig.LeftJoycon.ButtonSl.ToString();
                    _lSr.Label                        = controllerConfig.LeftJoycon.ButtonSr.ToString();
                    _rStickX.Label                    = controllerConfig.RightJoycon.StickX.ToString();
                    _invertRStickX.Active             = controllerConfig.RightJoycon.InvertStickX;
                    _rStickY.Label                    = controllerConfig.RightJoycon.StickY.ToString();
                    _invertRStickY.Active             = controllerConfig.RightJoycon.InvertStickY;
                    _rStickButton.Label               = controllerConfig.RightJoycon.StickButton.ToString();
                    _a.Label                          = controllerConfig.RightJoycon.ButtonA.ToString();
                    _b.Label                          = controllerConfig.RightJoycon.ButtonB.ToString();
                    _x.Label                          = controllerConfig.RightJoycon.ButtonX.ToString();
                    _y.Label                          = controllerConfig.RightJoycon.ButtonY.ToString();
                    _plus.Label                       = controllerConfig.RightJoycon.ButtonPlus.ToString();
                    _r.Label                          = controllerConfig.RightJoycon.ButtonR.ToString();
                    _zR.Label                         = controllerConfig.RightJoycon.ButtonZr.ToString();
                    _rSl.Label                        = controllerConfig.RightJoycon.ButtonSl.ToString();
                    _rSr.Label                        = controllerConfig.RightJoycon.ButtonSr.ToString();
                    _controllerDeadzoneLeft.Value     = controllerConfig.DeadzoneLeft;
                    _controllerDeadzoneRight.Value    = controllerConfig.DeadzoneRight;
                    _controllerTriggerThreshold.Value = controllerConfig.TriggerThreshold;
                    break;
            }
        }

        private InputConfig GetValues()
        {
            if (_inputDevice.ActiveId.StartsWith("keyboard"))
            {
                Enum.TryParse(_lStickUp.Label,     out Key lStickUp);
                Enum.TryParse(_lStickDown.Label,   out Key lStickDown);
                Enum.TryParse(_lStickLeft.Label,   out Key lStickLeft);
                Enum.TryParse(_lStickRight.Label,  out Key lStickRight);
                Enum.TryParse(_lStickButton.Label, out Key lStickButton);
                Enum.TryParse(_dpadUp.Label,       out Key lDPadUp);
                Enum.TryParse(_dpadDown.Label,     out Key lDPadDown);
                Enum.TryParse(_dpadLeft.Label,     out Key lDPadLeft);
                Enum.TryParse(_dpadRight.Label,    out Key lDPadRight);
                Enum.TryParse(_minus.Label,        out Key lButtonMinus);
                Enum.TryParse(_l.Label,            out Key lButtonL);
                Enum.TryParse(_zL.Label,           out Key lButtonZl);
                Enum.TryParse(_lSl.Label,          out Key lButtonSl);
                Enum.TryParse(_lSr.Label,          out Key lButtonSr);

                Enum.TryParse(_rStickUp.Label,     out Key rStickUp);
                Enum.TryParse(_rStickDown.Label,   out Key rStickDown);
                Enum.TryParse(_rStickLeft.Label,   out Key rStickLeft);
                Enum.TryParse(_rStickRight.Label,  out Key rStickRight);
                Enum.TryParse(_rStickButton.Label, out Key rStickButton);
                Enum.TryParse(_a.Label,            out Key rButtonA);
                Enum.TryParse(_b.Label,            out Key rButtonB);
                Enum.TryParse(_x.Label,            out Key rButtonX);
                Enum.TryParse(_y.Label,            out Key rButtonY);
                Enum.TryParse(_plus.Label,         out Key rButtonPlus);
                Enum.TryParse(_r.Label,            out Key rButtonR);
                Enum.TryParse(_zR.Label,           out Key rButtonZr);
                Enum.TryParse(_rSl.Label,          out Key rButtonSl);
                Enum.TryParse(_rSr.Label,          out Key rButtonSr);

                return new KeyboardConfig
                {
                    Index          = int.Parse(_inputDevice.ActiveId.Split("/")[1]),
                    ControllerType = Enum.Parse<ControllerType>(_controllerType.ActiveId),
                    PlayerIndex    = _playerIndex,
                    LeftJoycon     = new NpadKeyboardLeft
                    {
                        StickUp     = lStickUp,
                        StickDown   = lStickDown,
                        StickLeft   = lStickLeft,
                        StickRight  = lStickRight,
                        StickButton = lStickButton,
                        DPadUp      = lDPadUp,
                        DPadDown    = lDPadDown,
                        DPadLeft    = lDPadLeft,
                        DPadRight   = lDPadRight,
                        ButtonMinus = lButtonMinus,
                        ButtonL     = lButtonL,
                        ButtonZl    = lButtonZl,
                        ButtonSl    = lButtonSl,
                        ButtonSr    = lButtonSr
                    },
                    RightJoycon    = new NpadKeyboardRight
                    {
                        StickUp     = rStickUp,
                        StickDown   = rStickDown,
                        StickLeft   = rStickLeft,
                        StickRight  = rStickRight,
                        StickButton = rStickButton,
                        ButtonA     = rButtonA,
                        ButtonB     = rButtonB,
                        ButtonX     = rButtonX,
                        ButtonY     = rButtonY,
                        ButtonPlus  = rButtonPlus,
                        ButtonR     = rButtonR,
                        ButtonZr    = rButtonZr,
                        ButtonSl    = rButtonSl,
                        ButtonSr    = rButtonSr
                    }
                };
            }
            
            if (_inputDevice.ActiveId.StartsWith("controller"))
            {
                Enum.TryParse(_lStickX.Label,      out ControllerInputId lStickX);
                Enum.TryParse(_lStickY.Label,      out ControllerInputId lStickY);
                Enum.TryParse(_lStickButton.Label, out ControllerInputId lStickButton);
                Enum.TryParse(_minus.Label,        out ControllerInputId lButtonMinus);
                Enum.TryParse(_l.Label,            out ControllerInputId lButtonL);
                Enum.TryParse(_zL.Label,           out ControllerInputId lButtonZl);
                Enum.TryParse(_lSl.Label,          out ControllerInputId lButtonSl);
                Enum.TryParse(_lSr.Label,          out ControllerInputId lButtonSr);
                Enum.TryParse(_dpadUp.Label,       out ControllerInputId lDPadUp);
                Enum.TryParse(_dpadDown.Label,     out ControllerInputId lDPadDown);
                Enum.TryParse(_dpadLeft.Label,     out ControllerInputId lDPadLeft);
                Enum.TryParse(_dpadRight.Label,    out ControllerInputId lDPadRight);

                Enum.TryParse(_rStickX.Label,      out ControllerInputId rStickX);
                Enum.TryParse(_rStickY.Label,      out ControllerInputId rStickY);
                Enum.TryParse(_rStickButton.Label, out ControllerInputId rStickButton);
                Enum.TryParse(_a.Label,            out ControllerInputId rButtonA);
                Enum.TryParse(_b.Label,            out ControllerInputId rButtonB);
                Enum.TryParse(_x.Label,            out ControllerInputId rButtonX);
                Enum.TryParse(_y.Label,            out ControllerInputId rButtonY);
                Enum.TryParse(_plus.Label,         out ControllerInputId rButtonPlus);
                Enum.TryParse(_r.Label,            out ControllerInputId rButtonR);
                Enum.TryParse(_zR.Label,           out ControllerInputId rButtonZr);
                Enum.TryParse(_rSl.Label,          out ControllerInputId rButtonSl);
                Enum.TryParse(_rSr.Label,          out ControllerInputId rButtonSr);

                return new ControllerConfig
                {
                    Index            = int.Parse(_inputDevice.ActiveId.Split("/")[1]),
                    ControllerType   = Enum.Parse<ControllerType>(_controllerType.ActiveId),
                    PlayerIndex      = _playerIndex,
                    DeadzoneLeft     = (float)_controllerDeadzoneLeft.Value,
                    DeadzoneRight    = (float)_controllerDeadzoneRight.Value,
                    TriggerThreshold = (float)_controllerTriggerThreshold.Value,
                    LeftJoycon       = new NpadControllerLeft
                    {
                        InvertStickX = _invertLStickX.Active,
                        StickX       = lStickX,
                        InvertStickY = _invertLStickY.Active,
                        StickY       = lStickY,
                        StickButton  = lStickButton,
                        ButtonMinus  = lButtonMinus,
                        ButtonL      = lButtonL,
                        ButtonZl     = lButtonZl,
                        ButtonSl     = lButtonSl,
                        ButtonSr     = lButtonSr,
                        DPadUp       = lDPadUp,
                        DPadDown     = lDPadDown,
                        DPadLeft     = lDPadLeft,
                        DPadRight    = lDPadRight
                    },
                    RightJoycon      = new NpadControllerRight
                    {
                        InvertStickX = _invertRStickX.Active,
                        StickX       = rStickX,
                        InvertStickY = _invertRStickY.Active,
                        StickY       = rStickY,
                        StickButton  = rStickButton,
                        ButtonA      = rButtonA,
                        ButtonB      = rButtonB,
                        ButtonX      = rButtonX,
                        ButtonY      = rButtonY,
                        ButtonPlus   = rButtonPlus,
                        ButtonR      = rButtonR,
                        ButtonZr     = rButtonZr,
                        ButtonSl     = rButtonSl,
                        ButtonSr     = rButtonSr
                    }
                };
            }

            if (!_inputDevice.ActiveId.StartsWith("disabled"))
            {
                _gtkUserInterface.ShowErrorDialog("Some fields entered where invalid and therefore your config was not saved.");
            }

            return null;
        }

        private static bool IsAnyKeyPressed(out Key pressedKey, int index)
        {
            KeyboardState keyboardState = KeyboardController.GetKeyboardState(index);

            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (keyboardState.IsKeyDown((OpenTK.Input.Key)key))
                {
                    pressedKey = key;

                    return true;
                }
            }

            pressedKey = Key.Unbound;

            return false;
        }

        private static bool IsAnyButtonPressed(out ControllerInputId pressedButton, int index, double triggerThreshold)
        {
            JoystickState        joystickState        = Joystick.GetState(index);
            JoystickCapabilities joystickCapabilities = Joystick.GetCapabilities(index);

            //Buttons
            for (int i = 0; i != joystickCapabilities.ButtonCount; i++)
            {
                if (joystickState.IsButtonDown(i))
                {
                    Enum.TryParse($"Button{i}", out pressedButton);

                    return true;
                }
            }

            //Axis
            for (int i = 0; i != joystickCapabilities.AxisCount; i++)
            {
                if (joystickState.GetAxis(i) > 0.5f && joystickState.GetAxis(i) > triggerThreshold)
                {
                    Enum.TryParse($"Axis{i}", out pressedButton);

                    return true;
                }
            }

            //Hats
            for (int i = 0; i != joystickCapabilities.HatCount; i++)
            {
                JoystickHatState hatState = joystickState.GetHat((JoystickHat)i);
                string           pos      = null;

                if (hatState.IsUp)    pos = "Up";
                if (hatState.IsDown)  pos = "Down";
                if (hatState.IsLeft)  pos = "Left";
                if (hatState.IsRight) pos = "Right";
                if (pos == null)      continue;

                Enum.TryParse($"Hat{i}{pos}", out pressedButton);

                return true;
            }

            pressedButton = ControllerInputId.Unbound;

            return false;
        }

        private string GetProfileBasePath()
        {
            string path = System.IO.Path.Combine(Constants.BasePath, "profiles");

            if (_inputDevice.ActiveId.StartsWith("keyboard"))
            {
                path = System.IO.Path.Combine(path, "keyboard");
            }
            else if (_inputDevice.ActiveId.StartsWith("controller"))
            {
                path = System.IO.Path.Combine(path, "controller");
            }

            return path;
        }

        private void InputDevice_Changed(object sender, EventArgs args)
        {
            SetAvailableOptions();
            SetControllerSpecificFields();

            if (_inputDevice.ActiveId != null) SetProfiles();
        }

        private void Controller_Changed(object sender, EventArgs args)
        {
            SetControllerSpecificFields();
        }

        private void RefreshInputDevicesButton_Clicked(object sender, EventArgs args)
        {
            UpdateInputDeviceList();
        }

        private void Button_Clicked(object sender, EventArgs args)
        {
            if (_isWaitingForInput)
            {
                return;
            }

            _isWaitingForInput = true;

            Thread inputThread = new Thread(() =>
            {
                Button button = (ToggleButton)sender;

                if (_inputDevice.ActiveId.StartsWith("keyboard"))
                {
                    Key pressedKey;

                    int index = int.Parse(_inputDevice.ActiveId.Split("/")[1]);
                    while (!IsAnyKeyPressed(out pressedKey, index))
                    {
                        if (Mouse.GetState().IsAnyButtonDown || Keyboard.GetState().IsKeyDown(OpenTK.Input.Key.Escape))
                        {
                            Application.Invoke(delegate
                            {
                                button.SetStateFlags(0, true);
                            });

                            _isWaitingForInput = false;

                            return;
                        }
                    }

                    Application.Invoke(delegate
                    {
                        button.Label = pressedKey.ToString();
                        button.SetStateFlags(0, true);
                    });
                }
                else if (_inputDevice.ActiveId.StartsWith("controller"))
                {
                    ControllerInputId pressedButton;

                    int index = int.Parse(_inputDevice.ActiveId.Split("/")[1]);
                    while (!IsAnyButtonPressed(out pressedButton, index, _controllerTriggerThreshold.Value))
                    {
                        if (Mouse.GetState().IsAnyButtonDown || Keyboard.GetState().IsAnyKeyDown)
                        {
                            Application.Invoke(delegate
                            {
                                button.SetStateFlags(0, true);
                            });

                            _isWaitingForInput = false;

                            return;
                        }
                    }

                    Application.Invoke(delegate
                    {
                        button.Label = pressedButton.ToString();
                        button.SetStateFlags(0, true);
                    });
                }

                _isWaitingForInput = false;
            });
            inputThread.Name = "GUI.InputThread";
            inputThread.IsBackground = true;
            inputThread.Start();
        }

        private void SetProfiles()
        {
            string basePath = GetProfileBasePath();
            
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            _profile.RemoveAll();
            _profile.Append("default", "Default");

            foreach (string profile in Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories))
            {
                _profile.Append(System.IO.Path.GetFileName(profile), System.IO.Path.GetFileNameWithoutExtension(profile));
            }
        }

        private void ProfileLoad_Clicked(object sender, EventArgs args)
        {
            if (_inputDevice.ActiveId == "disabled" || _profile.ActiveId == null) return;

            InputConfig config = null;
            int         pos    = _profile.Active;

            if (_profile.ActiveId == "default")
            {
                if (_inputDevice.ActiveId.StartsWith("keyboard"))
                {
                    config = new KeyboardConfig
                    {
                        Index          = 0,
                        ControllerType = ControllerType.JoyconPair,
                        LeftJoycon     = new NpadKeyboardLeft
                        {
                            StickUp     = Key.W,
                            StickDown   = Key.S,
                            StickLeft   = Key.A,
                            StickRight  = Key.D,
                            StickButton = Key.F,
                            DPadUp      = Key.Up,
                            DPadDown    = Key.Down,
                            DPadLeft    = Key.Left,
                            DPadRight   = Key.Right,
                            ButtonMinus = Key.Minus,
                            ButtonL     = Key.E,
                            ButtonZl    = Key.Q,
                            ButtonSl    = Key.Unbound,
                            ButtonSr    = Key.Unbound
                        },
                        RightJoycon    = new NpadKeyboardRight
                        {
                            StickUp     = Key.I,
                            StickDown   = Key.K,
                            StickLeft   = Key.J,
                            StickRight  = Key.L,
                            StickButton = Key.H,
                            ButtonA     = Key.Z,
                            ButtonB     = Key.X,
                            ButtonX     = Key.C,
                            ButtonY     = Key.V,
                            ButtonPlus  = Key.Plus,
                            ButtonR     = Key.U,
                            ButtonZr    = Key.O,
                            ButtonSl    = Key.Unbound,
                            ButtonSr    = Key.Unbound
                        }
                    };
                }
                else if (_inputDevice.ActiveId.StartsWith("controller"))
                {
                    config = new ControllerConfig
                    {
                        Index            = 0,
                        ControllerType   = ControllerType.ProController,
                        DeadzoneLeft     = 0.1f,
                        DeadzoneRight    = 0.1f,
                        TriggerThreshold = 0.5f,
                        LeftJoycon       = new NpadControllerLeft
                        {
                            StickX       = ControllerInputId.Axis0,
                            StickY       = ControllerInputId.Axis1,
                            StickButton  = ControllerInputId.Button8,
                            DPadUp       = ControllerInputId.Hat0Up,
                            DPadDown     = ControllerInputId.Hat0Down,
                            DPadLeft     = ControllerInputId.Hat0Left,
                            DPadRight    = ControllerInputId.Hat0Right,
                            ButtonMinus  = ControllerInputId.Button6,
                            ButtonL      = ControllerInputId.Button4,
                            ButtonZl     = ControllerInputId.Axis2,
                            ButtonSl     = ControllerInputId.Unbound,
                            ButtonSr     = ControllerInputId.Unbound,
                            InvertStickX = false,
                            InvertStickY = false
                        },
                        RightJoycon      = new NpadControllerRight
                        {
                            StickX       = ControllerInputId.Axis3,
                            StickY       = ControllerInputId.Axis4,
                            StickButton  = ControllerInputId.Button9,
                            ButtonA      = ControllerInputId.Button1,
                            ButtonB      = ControllerInputId.Button0,
                            ButtonX      = ControllerInputId.Button3,
                            ButtonY      = ControllerInputId.Button2,
                            ButtonPlus   = ControllerInputId.Button7,
                            ButtonR      = ControllerInputId.Button5,
                            ButtonZr     = ControllerInputId.Axis5,
                            ButtonSl     = ControllerInputId.Unbound,
                            ButtonSr     = ControllerInputId.Unbound,
                            InvertStickX = false,
                            InvertStickY = false
                        }
                    };
                }
            }
            else
            {
                string path = System.IO.Path.Combine(GetProfileBasePath(), _profile.ActiveId);

                if (!File.Exists(path))
                {
                    if (pos >= 0)
                    {
                        _profile.Remove(pos);
                    }

                    return;
                }

                try
                {
                    using (Stream stream = File.OpenRead(path))
                    {
                        config = JsonHelper.Deserialize<ControllerConfig>(stream);
                    }
                }
                catch (JsonException)
                {
                    try
                    {
                        using (Stream stream = File.OpenRead(path))
                        {
                            config = JsonHelper.Deserialize<KeyboardConfig>(stream);
                        }
                    }
                    catch { }
                }
            }

            SetValues(config);
        }

        private void ProfileAdd_Clicked(object sender, EventArgs args)
        {
            if (_inputDevice.ActiveId == "disabled") return;

            InputConfig   inputConfig   = GetValues();
            ProfileDialog profileDialog = new ProfileDialog();

            if (inputConfig == null) return;

            if (profileDialog.Run() == (int)ResponseType.Ok)
            {
                string path = System.IO.Path.Combine(GetProfileBasePath(), profileDialog.FileName);
                string jsonString;

                if (inputConfig is KeyboardConfig keyboardConfig)
                {
                    jsonString = JsonHelper.Serialize(keyboardConfig, true);
                }
                else
                {
                    jsonString = JsonHelper.Serialize(inputConfig as ControllerConfig, true);
                }

                File.WriteAllText(path, jsonString);
            }

            profileDialog.Dispose();

            SetProfiles();
        }

        private void ProfileRemove_Clicked(object sender, EventArgs args)
        {
            if (_inputDevice.ActiveId == "disabled" || _profile.ActiveId == "default" || _profile.ActiveId == null) return;

            if (_gtkUserInterface.ShowConfirmationDialog("Deleting Profile", "This action is irreversible, are your sure you want to continue?"))
            {
                string path = System.IO.Path.Combine(GetProfileBasePath(), _profile.ActiveId);

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                SetProfiles();
            }
        }

        private void SaveButton_Clicked(object sender, EventArgs args)
        {
            InputConfig inputConfig = GetValues();

            if (_inputConfig == null && inputConfig != null)
            {
                ConfigurationState.Instance.Hid.InputConfig.Value.Add(inputConfig);
            }
            else
            {
                if (_inputDevice.ActiveId == "disabled")
                {
                    ConfigurationState.Instance.Hid.InputConfig.Value.Remove(_inputConfig);
                }
                else if (inputConfig != null)
                {
                    int index = ConfigurationState.Instance.Hid.InputConfig.Value.IndexOf(_inputConfig);        

                    ConfigurationState.Instance.Hid.InputConfig.Value[index] = inputConfig;
                }
            }

            MainWindow.SaveConfig();

            Dispose();
        }
    }
}