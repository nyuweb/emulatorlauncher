﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Drawing;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.EmulationStation;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.Common.Joysticks;

namespace EmulatorLauncher
{
    class DolphinControllers
    {
        /// <summary>
        /// Cf. https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/InputCommon/ControllerInterface/SDL/SDL.cpp#L191
        /// </summary>
        private static void UpdateSdlControllersWithHints()
        {
            var hints = new List<string>();
            hints.Add("SDL_HINT_JOYSTICK_HIDAPI_PS4_RUMBLE = 1");

            SdlGameController.ReloadWithHints(string.Join(",", hints));
            Program.Controllers.ForEach(c => c.ResetSdlController());
        }

        public static bool WriteControllersConfig(string path, string system, string rom, bool triforce)
        {
            if (Program.SystemConfig.isOptSet("disableautocontrollers") && Program.SystemConfig["disableautocontrollers"] == "1")
                return false;

            UpdateSdlControllersWithHints();

            if (system == "wii")
            {
                if (Program.SystemConfig.isOptSet("emulatedwiimotes") && Program.SystemConfig.getOptBoolean("emulatedwiimotes"))
                {
                    generateControllerConfig_emulatedwiimotes(path, rom);
                    removeControllerConfig_gamecube(path, "Dolphin.ini"); // because pads will already be used as emulated wiimotes
                    return true;
                }
                else
                    generateControllerConfig_realwiimotes(path, "WiimoteNew.ini", "Wiimote");

                generateControllerConfig_gamecube(path, rom, gamecubeMapping);
            }
            // Special mapping for triforce games to remove Z button from R1 (as this is used to access service menu and will be mapped to R3+L3)
            else if (triforce)
                generateControllerConfig_gamecube(path, rom, triforceMapping, triforce);
            else
                generateControllerConfig_gamecube(path, rom, gamecubeMapping);
            return true;            
        }

        static InputKeyMapping gamecubeMapping = new InputKeyMapping()
        { 
            { InputKey.l3,              "Main Stick/Modifier"},
            { InputKey.r3,              "C-Stick/Modifier"},
            { InputKey.l2,              "Triggers/L-Analog" },    
            { InputKey.r2,              "Triggers/R-Analog"},
            { InputKey.y,               "Buttons/Y" },  
            { InputKey.b,               "Buttons/B" },
            { InputKey.x,               "Buttons/X" },  
            { InputKey.a,               "Buttons/A" },
            { InputKey.start,           "Buttons/Start" },
            { InputKey.pagedown,        "Buttons/Z" },  
            { InputKey.l2,              "Triggers/L" }, 
            { InputKey.r2,              "Triggers/R" },
            { InputKey.up,              "D-Pad/Up" }, 
            { InputKey.down,            "D-Pad/Down" }, 
            { InputKey.left,            "D-Pad/Left" }, 
            { InputKey.right,           "D-Pad/Right" },
            { InputKey.joystick1up,     "Main Stick/Up" }, 
            { InputKey.joystick1left,   "Main Stick/Left" },
            { InputKey.joystick2up,     "C-Stick/Up" },    
            { InputKey.joystick2left,   "C-Stick/Left"},
            { InputKey.hotkey,          "Buttons/Hotkey" },
        };

        static InputKeyMapping triforceMapping = new InputKeyMapping()
        {
            { InputKey.l2,              "Triggers/L-Analog" },
            { InputKey.r2,              "Triggers/R-Analog"},
            { InputKey.y,               "Buttons/Y" },
            { InputKey.b,               "Buttons/B" },
            { InputKey.select,          "Buttons/X" },
            { InputKey.a,               "Buttons/A" },
            { InputKey.start,           "Buttons/Start" },
            { InputKey.l2,              "Triggers/L" },
            { InputKey.r2,              "Triggers/R" },
            { InputKey.up,              "D-Pad/Up" },
            { InputKey.down,            "D-Pad/Down" },
            { InputKey.left,            "D-Pad/Left" },
            { InputKey.right,           "D-Pad/Right" },
            { InputKey.joystick1up,     "Main Stick/Up" },
            { InputKey.joystick1left,   "Main Stick/Left" },
            { InputKey.joystick2up,     "C-Stick/Up" },
            { InputKey.joystick2left,   "C-Stick/Left"},
            { InputKey.hotkey,          "Buttons/Hotkey" },
        };

        static InputKeyMapping vs4mapping = new InputKeyMapping()
        {
            { InputKey.joystick1left,  "Main Stick/Down" },
            { InputKey.joystick1up,    "Main Stick/Left" },
        };

        static InputKeyMapping gamecubeWiiMapping = new InputKeyMapping()
        { 
            { InputKey.l3,              "Main Stick/Modifier"},
            { InputKey.r3,              "C-Stick/Modifier"},
            { InputKey.l2,              "Triggers/L-Analog" },    
            { InputKey.r2,              "Triggers/R-Analog"},
            { InputKey.y,               "Buttons/Y" },  
            { InputKey.b,               "Buttons/B" },
            { InputKey.x,               "Buttons/X" },  
            { InputKey.a,               "Buttons/A" },
            { InputKey.select,          "Buttons/Z" },  
            { InputKey.start,           "Buttons/Start" },
            { InputKey.pageup,          "Triggers/L" }, 
            { InputKey.pagedown,        "Triggers/R" },
            { InputKey.up,              "D-Pad/Up" }, 
            { InputKey.down,            "D-Pad/Down" }, 
            { InputKey.left,            "D-Pad/Left" }, 
            { InputKey.right,           "D-Pad/Right" },
            { InputKey.joystick1up,     "Main Stick/Up" }, 
            { InputKey.joystick1left,   "Main Stick/Left" },
            { InputKey.joystick2up,     "C-Stick/Up" },    
            { InputKey.joystick2left,   "C-Stick/Left"}          
        };

        static InputKeyMapping reversedButtons = new InputKeyMapping()
        {
            { InputKey.b,               "Buttons/A" },
            { InputKey.a,               "Buttons/B" },
            { InputKey.x,               "Buttons/Y" },
            { InputKey.y,               "Buttons/X" }
        };

        static InputKeyMapping reversedButtonsAB = new InputKeyMapping()
        {
            { InputKey.b,               "Buttons/A" },
            { InputKey.a,               "Buttons/B" }
        };

        static InputKeyMapping reversedButtonsRotate = new InputKeyMapping()
        {
            { InputKey.b,               "Buttons/A" },
            { InputKey.y,               "Buttons/B" },
            { InputKey.x,               "Buttons/Y" },
            { InputKey.a,               "Buttons/X" }
        };

        static Dictionary<string, string> gamecubeReverseAxes = new Dictionary<string,string>()
        {
            { "Main Stick/Up",   "Main Stick/Down" },
            { "Main Stick/Left", "Main Stick/Right" },
            { "C-Stick/Up",      "C-Stick/Down" },
            { "C-Stick/Left",    "C-Stick/Right" }
        };

        static Dictionary<string, string> vs4ReverseAxes = new Dictionary<string, string>()
        {
            { "Main Stick/Down",   "Main Stick/Up" },
            { "Main Stick/Left",   "Main Stick/Right" },
            { "C-Stick/Up",        "C-Stick/Down" },
            { "C-Stick/Left",      "C-Stick/Right" }
        };

        // if joystick1up is missing on the pad, use up instead
        static Dictionary<string, string> gamecubeReplacements = new Dictionary<string, string>()
        {
            { "joystick1up", "up" },
            { "joystick1left", "left" },
            { "joystick1down", "down" },
            { "joystick1right", "right" }
        };

        static InputKeyMapping _wiiMapping = new InputKeyMapping 
        {
            { InputKey.x,               "Buttons/2" },  
            { InputKey.b,               "Buttons/A" },
            { InputKey.y,               "Buttons/1" },  
            { InputKey.a,               "Buttons/B" },
            { InputKey.pageup,          "Buttons/-" },  
            { InputKey.pagedown,        "Buttons/+" },
            { InputKey.select,          "Buttons/Home" },
            { InputKey.up,              "D-Pad/Up" }, 
            { InputKey.down,            "D-Pad/Down" }, 
            { InputKey.left,            "D-Pad/Left" }, 
            { InputKey.right,           "D-Pad/Right" },
            { InputKey.joystick1up,     "IR/Up" }, 
            { InputKey.joystick1left,   "IR/Left" },
            { InputKey.joystick2up,     "Tilt/Forward" }, 
            { InputKey.joystick2left,   "Tilt/Left" },
            { InputKey.l3,              "IR/Relative Input Hold" },
            { InputKey.r3,              "Tilt/Modifier" }
        };

        static Dictionary<string, string> wiiReverseAxes = new Dictionary<string,string>()
        {
            { "IR/Up",      "IR/Down"},
            { "IR/Left",    "IR/Right"},
            { "Swing/Up",   "Swing/Down"},
            { "Swing/Left", "Swing/Right"},
            { "Tilt/Left",  "Tilt/Right"},
            { "Tilt/Forward", "Tilt/Backward"},
            { "Nunchuk/Stick/Up" ,  "Nunchuk/Stick/Down"},
            { "Nunchuk/Stick/Left", "Nunchuk/Stick/Right"},
            { "Classic/Right Stick/Up" , "Classic/Right Stick/Down"},
            { "Classic/Right Stick/Left" , "Classic/Right Stick/Right"},
            { "Classic/Left Stick/Up" , "Classic/Left Stick/Down"},
            { "Classic/Left Stick/Left" , "Classic/Left Stick/Right" }
        };

        private static void generateControllerConfig_emulatedwiimotes(string path, string rom)
        {
            var extraOptions = new Dictionary<string, string>();
            extraOptions["Source"] = "1";

            var wiiMapping = new InputKeyMapping(_wiiMapping);

            if (Program.SystemConfig["controller_mode"] != "cc" && Program.SystemConfig["controller_mode"] != "ccp")
            {
                if (Program.SystemConfig["controller_mode"] == "side")
                {
                    extraOptions["Options/Sideways Wiimote"] = "1";
                    wiiMapping[InputKey.x] = "Buttons/A";
                    wiiMapping[InputKey.y] = "Buttons/1";
                    wiiMapping[InputKey.b] = "Buttons/2";
                    wiiMapping[InputKey.a] = "Buttons/B";
                    wiiMapping[InputKey.l2] = "Shake/X";
                    wiiMapping[InputKey.l2] = "Shake/Y";
                    wiiMapping[InputKey.l2] = "Shake/Z";
                    wiiMapping[InputKey.select] = "Buttons/-";
                    wiiMapping[InputKey.start] = "Buttons/+";
                    wiiMapping[InputKey.pageup] = "Tilt/Left";
                    wiiMapping[InputKey.pagedown] = "Tilt/Right";
                }

                // i: infrared, s: swing, t: tilt, n: nunchuk
                // 12 possible combinations : is si / it ti / in ni / st ts / sn ns / tn nt

                // i
                if (Program.SystemConfig["controller_mode"] == "is" || Program.SystemConfig["controller_mode"] == "it" || Program.SystemConfig["controller_mode"] == "in")
                {
                    wiiMapping[InputKey.joystick1up] = "IR/Up";
                    wiiMapping[InputKey.joystick1left] = "IR/Left";
                    wiiMapping[InputKey.l3] = "IR/Relative Input Hold";
                }

                if (Program.SystemConfig["controller_mode"] == "si" || Program.SystemConfig["controller_mode"] == "ti" || Program.SystemConfig["controller_mode"] == "ni")
                {
                    wiiMapping[InputKey.joystick2up] = "IR/Up";
                    wiiMapping[InputKey.joystick2left] = "IR/Left";
                    wiiMapping[InputKey.r3] = "IR/Relative Input Hold";
                }

                // s
                if (Program.SystemConfig["controller_mode"] == "si" || Program.SystemConfig["controller_mode"] == "st" || Program.SystemConfig["controller_mode"] == "sn")
                {
                    wiiMapping[InputKey.joystick1up]   = "Swing/Up";
                    wiiMapping[InputKey.joystick1left] = "Swing/Left";
                }

                if (Program.SystemConfig["controller_mode"] == "is" || Program.SystemConfig["controller_mode"] == "ts" || Program.SystemConfig["controller_mode"] == "ns")
                {
                    wiiMapping[InputKey.joystick2up]   = "Swing/Up";
                    wiiMapping[InputKey.joystick2left] = "Swing/Left";
                }

                // t
                if (Program.SystemConfig["controller_mode"] == "ti" || Program.SystemConfig["controller_mode"] == "ts" || Program.SystemConfig["controller_mode"] == "tn")
                {
                    wiiMapping[InputKey.joystick1up] = "Tilt/Forward";
                    wiiMapping[InputKey.joystick1left] = "Tilt/Left";
                    wiiMapping[InputKey.l3] = "Tilt/Modifier";
                }

                if (Program.SystemConfig["controller_mode"] == "it" || Program.SystemConfig["controller_mode"] == "st" || Program.SystemConfig["controller_mode"] == "nt")
                {
                    wiiMapping[InputKey.joystick2up] = "Tilt/Forward";
                    wiiMapping[InputKey.joystick2left] = "Tilt/Left";
                    wiiMapping[InputKey.r3] = "Tilt/Modifier";
                }

                // n
                if (Program.SystemConfig["controller_mode"] == "ni" || Program.SystemConfig["controller_mode"] == "ns" || Program.SystemConfig["controller_mode"] == "nt")
                {
                    extraOptions["Extension"] = "Nunchuk";
                    wiiMapping[InputKey.l1] = "Nunchuk/Buttons/C";
                    wiiMapping[InputKey.r1] = "Nunchuk/Buttons/Z";
                    wiiMapping[InputKey.joystick1up] = "Nunchuk/Stick/Up";
                    wiiMapping[InputKey.joystick1left] = "Nunchuk/Stick/Left";
                    wiiMapping[InputKey.l3] = "Nunchuk/Stick/Modifier";
                    wiiMapping[InputKey.select] = "Buttons/-";
                    wiiMapping[InputKey.start] = "Buttons/+";
                    wiiMapping[InputKey.l2] = "Shake/X";
                    wiiMapping[InputKey.l2] = "Shake/Y";
                    wiiMapping[InputKey.l2] = "Shake/Z";
                }

                if (Program.SystemConfig["controller_mode"] == "in" || Program.SystemConfig["controller_mode"] == "sn" || Program.SystemConfig["controller_mode"] == "tn")
                {
                    extraOptions["Extension"] = "Nunchuk";
                    wiiMapping[InputKey.l1] = "Nunchuk/Buttons/C";
                    wiiMapping[InputKey.r1] = "Nunchuk/Buttons/Z";
                    wiiMapping[InputKey.joystick2up] = "Nunchuk/Stick/Up";
                    wiiMapping[InputKey.joystick2left] = "Nunchuk/Stick/Left";
                    wiiMapping[InputKey.r3] = "Nunchuk/Stick/Modifier";
                    wiiMapping[InputKey.select] = "Buttons/-";
                    wiiMapping[InputKey.start] = "Buttons/+";
                    wiiMapping[InputKey.l2] = "Shake/X";
                    wiiMapping[InputKey.l2] = "Shake/Y";
                    wiiMapping[InputKey.l2] = "Shake/Z";
                }
            }

            // cc : Classic Controller Settings
            else if (Program.SystemConfig["controller_mode"] == "cc" || Program.SystemConfig["controller_mode"] == "ccp")
            {
                bool revertall = Program.Features.IsSupported("gamepadbuttons") && Program.SystemConfig.isOptSet("gamepadbuttons") && Program.SystemConfig["gamepadbuttons"] == "reverse_all";
                bool revertAB = Program.Features.IsSupported("gamepadbuttons") && Program.SystemConfig.isOptSet("gamepadbuttons") && Program.SystemConfig["gamepadbuttons"] == "reverse_ab";

                bool pro = Program.SystemConfig["controller_mode"] == "ccp";

                extraOptions["Extension"] = "Classic";

                if (revertall)
                {
                    wiiMapping[InputKey.y] = "Classic/Buttons/X";
                    wiiMapping[InputKey.x] = "Classic/Buttons/Y";
                    wiiMapping[InputKey.a] = "Classic/Buttons/B";
                    wiiMapping[InputKey.b] = "Classic/Buttons/A";
                }
                else
                {
                    wiiMapping[InputKey.x] = "Classic/Buttons/X";
                    wiiMapping[InputKey.y] = "Classic/Buttons/Y";
                    wiiMapping[InputKey.b] = revertAB ? "Classic/Buttons/A" : "Classic/Buttons/B";
                    wiiMapping[InputKey.a] = revertAB ? "Classic/Buttons/B" : "Classic/Buttons/A";
                }
                wiiMapping[InputKey.select] = "Classic/Buttons/-";
                wiiMapping[InputKey.start] = "Classic/Buttons/+";

                if (!pro)
                {
                    wiiMapping[InputKey.pageup] = "Classic/Buttons/ZL";
                    wiiMapping[InputKey.pagedown] = "Classic/Buttons/ZR";
                    wiiMapping[InputKey.l2] = "Classic/Triggers/L";
                    wiiMapping[InputKey.r2] = "Classic/Triggers/R";
                }
                else
                {
                    wiiMapping[InputKey.pageup] = "Classic/Triggers/L";
                    wiiMapping[InputKey.pagedown] = "Classic/Triggers/R";
                    wiiMapping[InputKey.l2] = "Classic/Buttons/ZL";
                    wiiMapping[InputKey.r2] = "Classic/Buttons/ZR";
                }

                wiiMapping[InputKey.up] = "Classic/D-Pad/Up";
                wiiMapping[InputKey.down] = "Classic/D-Pad/Down";
                wiiMapping[InputKey.left] = "Classic/D-Pad/Left";
                wiiMapping[InputKey.right] = "Classic/D-Pad/Right";
                wiiMapping[InputKey.joystick1up] = "Classic/Left Stick/Up";
                wiiMapping[InputKey.joystick1left] = "Classic/Left Stick/Left";
                wiiMapping[InputKey.joystick2up] = "Classic/Right Stick/Up";
                wiiMapping[InputKey.joystick2left] = "Classic/Right Stick/Left";
                wiiMapping[InputKey.l3] = "Classic/Left Stick/Modifier";
                wiiMapping[InputKey.r3] = "Classic/Right Stick/Modifier";
            }

            generateControllerConfig_any(path, "WiimoteNew.ini", "Wiimote", wiiMapping, wiiReverseAxes, null, false, extraOptions);
        }

        private static void generateControllerConfig_gamecube(string path, string rom, InputKeyMapping anyMapping, bool triforce = false)
        {
            bool vs4axis = triforce && Program.SystemConfig.isOptSet("triforce_mapping") && Program.SystemConfig["triforce_mapping"] == "vs4";

            generateControllerConfig_any(path, "GCPadNew.ini", "GCPad", anyMapping, vs4axis ? vs4ReverseAxes : gamecubeReverseAxes, gamecubeReplacements, triforce);     
        }

        static Dictionary<XINPUTMAPPING, string> xInputMapping = new Dictionary<XINPUTMAPPING, string>()
        {
            { XINPUTMAPPING.X,                  "`Button Y`" },  
            { XINPUTMAPPING.B,                  "`Button A`" },
            { XINPUTMAPPING.Y,                  "`Button X`" },  
            { XINPUTMAPPING.A,                  "`Button B`" },
            { XINPUTMAPPING.BACK,               "Back" },  
            { XINPUTMAPPING.START,              "Start" },
            { XINPUTMAPPING.LEFTSHOULDER,       "`Shoulder L`" }, 
            { XINPUTMAPPING.RIGHTSHOULDER,      "`Shoulder R`" },
            { XINPUTMAPPING.DPAD_UP,            "`Pad N`" }, 
            { XINPUTMAPPING.DPAD_DOWN,          "`Pad S`" }, 
            { XINPUTMAPPING.DPAD_LEFT,          "`Pad W`" }, 
            { XINPUTMAPPING.DPAD_RIGHT,         "`Pad E`" },
            { XINPUTMAPPING.LEFTANALOG_UP,      "`Left Y+`" }, 
            { XINPUTMAPPING.LEFTANALOG_DOWN,    "`Left Y-`" },
            { XINPUTMAPPING.LEFTANALOG_LEFT,    "`Left X-`" },    
            { XINPUTMAPPING.LEFTANALOG_RIGHT,   "`Left X+`"},
            { XINPUTMAPPING.RIGHTANALOG_UP,     "`Right Y+`" }, 
            { XINPUTMAPPING.RIGHTANALOG_DOWN,   "`Right Y-`" },
            { XINPUTMAPPING.RIGHTANALOG_LEFT,   "`Right X-`" },    
            { XINPUTMAPPING.RIGHTANALOG_RIGHT,  "`Right X+`"},
            { XINPUTMAPPING.LEFTSTICK,          "`Thumb L`" },
            { XINPUTMAPPING.RIGHTSTICK,         "`Thumb R`" },
            { XINPUTMAPPING.LEFTTRIGGER,        "`Trigger L`" },
            { XINPUTMAPPING.RIGHTTRIGGER,       "`Trigger R`" }
        };

        private static void generateControllerConfig_realwiimotes(string path, string filename, string anyDefKey)
        {
            string iniFile = Path.Combine(path, "User", "Config", filename);

            using (IniFile ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                for (int i = 1; i < 5; i++)
                {
                    ini.ClearSection(anyDefKey + i.ToString());
                    ini.WriteValue(anyDefKey + i.ToString(), "Source", "2");
                }

                ini.Save();
            }
        }

        private static string GetSDLMappingName(Controller pad, InputKey key)
        {
            var input = pad.GetSdlMapping(key);
            if (input == null)
                return null;

            if (input.Type == "button")
            {
                if (input.Id == 0) // invert A&B
                    return "`Button 1`";
                
                if (input.Id == 1) // invert A&B
                    return "`Button 0`";
                
                return "`Button " + input.Id.ToString() + "`";
            }
            
            if (input.Type == "axis")
            {
                Func<Input, bool, string> axisValue = (inp, revertAxis) =>
                {
                    string axis = "`Axis ";

                    if (inp.Id == 0 || inp.Id == 1 || inp.Id == 2 || inp.Id == 3)
                        axis += inp.Id;

                    if ((!revertAxis && inp.Value > 0) || (revertAxis && inp.Value < 0))
                        axis += "+";
                    else
                        axis += "-";

                    if (inp.Id == 4 || inp.Id == 5)
                        axis = "`Full Axis " + inp.Id + "+";

                    return axis + "`";
                };

                return axisValue(input, false);
                /*
                string reverseAxis;
                if (anyReverseAxes.TryGetValue(value, out reverseAxis))
                    ini.WriteValue(gcpad, reverseAxis, axisValue(input, true));*/
            }

            return null;
        }

        private static void generateControllerConfig_any(string path, string filename, string anyDefKey, InputKeyMapping anyMapping, Dictionary<string, string> anyReverseAxes, Dictionary<string, string> anyReplacements, bool triforce = false, Dictionary<string, string> extraOptions = null)
        {
            //string path = Program.AppConfig.GetFullPath("dolphin");
            string iniFile = Path.Combine(path, "User", "Config", filename);

            bool forceSDL = false;
            if (Program.SystemConfig.isOptSet("input_forceSDL") && Program.SystemConfig.getOptBoolean("input_forceSDL"))
                forceSDL = true;

            int nsamepad = 0;
            bool gc = anyDefKey == "GCPad";

            Dictionary<string, int> double_pads = new Dictionary<string,int>();

            using (IniFile ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                foreach (var pad in Program.Controllers.OrderBy(i => i.PlayerIndex))
                {
                    string gcpad = anyDefKey + pad.PlayerIndex;
                    ini.ClearSection(gcpad);

                    if (pad.Config == null)
                        continue;

                    // SIDevice0 = 7 -> Keyb GCKeyNew.ini
                    // SIDevice1 = 6 -> controlleur standard GCPadNew.ini

                    string guid = pad.GetSdlGuid(SdlVersion.SDL2_0_X).ToLowerInvariant();
                    var prod = pad.ProductID;

                    string tech = "XInput";
                    string deviceName = "Gamepad";
                    int xIndex = 0;

                    if (pad.Config.Type == "keyboard")
                    {
                        tech = "DInput";
                        deviceName = "Keyboard Mouse";                        
                    } 
                    else if (!pad.IsXInputDevice || forceSDL)
                    {
                        var s = pad.SdlController;
                        if (s == null)
                            continue;
                        
                        tech = "SDL";

                        deviceName = pad.Name;

                        if (deviceName == "DualSense Wireless Controller")
                            deviceName = "PS5 Controller";
                    }
             
                    if (double_pads.ContainsKey(tech + "/" + deviceName))
                        nsamepad = double_pads[tech + "/" + deviceName];
                    else 
                        nsamepad = 0;

                    double_pads[tech + "/" + deviceName] = nsamepad + 1;

                    if (pad.IsXInputDevice)
                        xIndex = pad.XInput != null ? pad.XInput.DeviceIndex : pad.DeviceIndex; 

                    if (tech == "XInput")
                        ini.WriteValue(gcpad, "Device", tech + "/" + xIndex + "/" + deviceName);
                    else
                        ini.WriteValue(gcpad, "Device", tech + "/" + nsamepad.ToString() + "/" + deviceName);

                    if (extraOptions != null)
                       foreach(var xtra in extraOptions)
                           ini.WriteValue(gcpad, xtra.Key, xtra.Value);

                    bool revertButtons = Program.Features.IsSupported("gamepadbuttons") && Program.SystemConfig.isOptSet("gamepadbuttons") && Program.SystemConfig["gamepadbuttons"] == "reverse_all";
                    bool revertButtonsAB = Program.Features.IsSupported("gamepadbuttons") && Program.SystemConfig.isOptSet("gamepadbuttons") && Program.SystemConfig["gamepadbuttons"] == "reverse_ab";
                    bool revertRotate = Program.Features.IsSupported("gamepadbuttons") && Program.SystemConfig.isOptSet("gamepadbuttons") && Program.SystemConfig["gamepadbuttons"] == "reverse_rotate";
                    bool rumble = !Program.SystemConfig.getOptBoolean("input_rumble");

                    foreach (var x in anyMapping)
                    {
                        string value = x.Value;

                        if (revertButtons && reversedButtons.ContainsKey(x.Key))
                            value = reversedButtons[x.Key];

                        if (revertButtonsAB && reversedButtonsAB.ContainsKey(x.Key))
                            value = reversedButtonsAB[x.Key];

                        if (revertRotate && reversedButtonsRotate.ContainsKey(x.Key))
                            value = reversedButtonsRotate[x.Key];

                        if (triforce && Program.SystemConfig.isOptSet("triforce_mapping") && Program.SystemConfig["triforce_mapping"] == "vs4")
                        {
                            if (vs4mapping.ContainsKey(x.Key))
                                value = vs4mapping[x.Key];
                        }

                        if (pad.Config.Type == "keyboard")
                        {
                            if (x.Key == InputKey.a && (revertButtons || revertButtonsAB))
                                value = "Buttons/B";
                            else if (x.Key == InputKey.a)
                                value = "Buttons/A";
                            else if (x.Key == InputKey.b && (revertButtons || revertButtonsAB))
                                value = "Buttons/A";
                            else if (x.Key == InputKey.b)
                                value = "Buttons/B";
                            else if (x.Key == InputKey.x && revertButtons)
                                value = "Buttons/Y";
                            else if (x.Key == InputKey.x)
                                value = "Buttons/X";
                            else if (x.Key == InputKey.y && revertButtons)
                                value = "Buttons/X";
                            else if (x.Key == InputKey.y)
                                value = "Buttons/Y";
                            else if (x.Key == InputKey.up)
                                value = "Main Stick/Up";
                            else if (x.Key == InputKey.down)
                                value = "Main Stick/Down";
                            else if (x.Key == InputKey.left)
                                value = "Main Stick/Left";
                            else if (x.Key == InputKey.right)
                                value = "Main Stick/Right";

                            if (x.Key == InputKey.joystick1left || x.Key == InputKey.joystick1up)
                                continue;

                            var input = pad.Config[x.Key];
                            if (input == null)
                                continue;

                            var name = ToDolphinKey(input.Id);
                            ini.WriteValue(gcpad, value, name);
                        }
                        else if (tech == "XInput")
                        {
                            var mapping = pad.GetXInputMapping(x.Key);
                            if (mapping != XINPUTMAPPING.UNKNOWN && xInputMapping.ContainsKey(mapping))
                                ini.WriteValue(gcpad, value, xInputMapping[mapping]);

                            string reverseAxis;
                            if (anyReverseAxes.TryGetValue(value, out reverseAxis))
                            {
                                mapping = pad.GetXInputMapping(x.Key, true);
                                if (mapping != XINPUTMAPPING.UNKNOWN && xInputMapping.ContainsKey(mapping))
                                    ini.WriteValue(gcpad, reverseAxis, xInputMapping[mapping]);
                            }

                            // Z button is used to access test menu, do not map it with R1
                            if (triforce)
                                ini.WriteValue(gcpad, "Buttons/Z", "`Thumb L`&`Thumb R`");

                        }
                        else if (forceSDL)
                        {
                            var input = pad.Config[x.Key];

                            if (input == null)
                                continue;

                            if (input.Type == "button")
                            {
                                if (input.Id == 0) // invert A&B
                                    ini.WriteValue(gcpad, value, "`Button 1`");
                                else if (input.Id == 1) // invert A&B
                                    ini.WriteValue(gcpad, value, "`Button 0`");
                                else
                                    ini.WriteValue(gcpad, value, "`Button " + input.Id.ToString() + "`");
                            }

                            else if (input.Type == "axis")
                            {
                                Func<Input, bool, string> axisValue = (inp, revertAxis) =>
                                {
                                    string axis = "`Axis ";

                                    if (inp.Id == 0 || inp.Id == 1 || inp.Id == 2 || inp.Id == 3)
                                        axis += inp.Id;

                                    if ((!revertAxis && inp.Value > 0) || (revertAxis && inp.Value < 0))
                                        axis += "+";
                                    else
                                        axis += "-";

                                    if (inp.Id == 4 || inp.Id == 5)
                                        axis = "`Full Axis " + inp.Id + "+";

                                    return axis + "`";
                                };

                                ini.WriteValue(gcpad, value, axisValue(input, false));

                                string reverseAxis;
                                if (anyReverseAxes.TryGetValue(value, out reverseAxis))
                                    ini.WriteValue(gcpad, reverseAxis, axisValue(input, true));
                            }

                            else if (input.Type == "hat")
                            {
                                Int64 pid = input.Value;
                                switch (pid)
                                {
                                    case 1: 
                                        ini.WriteValue(gcpad, value, "`Hat " + input.Id.ToString() + " N`");
                                        break;
                                    case 2:
                                        ini.WriteValue(gcpad, value, "`Hat " + input.Id.ToString() + " E`");
                                        break;
                                    case 4:
                                        ini.WriteValue(gcpad, value, "`Hat " + input.Id.ToString() + " S`");
                                        break;
                                    case 8:
                                        ini.WriteValue(gcpad, value, "`Hat " + input.Id.ToString() + " W`");
                                        break;
                                }
                            }
                            // Z button is used to access test menu, do not map it with R1
                            if (triforce)
                                ini.WriteValue(gcpad, "Buttons/Z", "@(`Button 8`+`Button 9`)");
                        }
                        
                        else // SDL
                        {
                            var input = pad.GetSdlMapping(x.Key);

                            if (input == null)
                                continue;

                            if (input.Type == "button")
                            {
                                if (input.Id == 0) // invert A&B
                                    ini.WriteValue(gcpad, value, "`Button 1`");
                                else if (input.Id == 1) // invert A&B
                                    ini.WriteValue(gcpad, value, "`Button 0`");
                                else
                                    ini.WriteValue(gcpad, value, "`Button " + input.Id.ToString() + "`");
                            }
                            else if (input.Type == "axis")
                            {
                                Func<Input, bool, string> axisValue = (inp, revertAxis) =>
                                {                                     
                                    string axis = "`Axis ";

                                    if (inp.Id == 0 || inp.Id == 1 || inp.Id == 2 || inp.Id == 3)
                                        axis += inp.Id;

                                    if ((!revertAxis && inp.Value > 0) || (revertAxis && inp.Value < 0))                                            
                                        axis += "+";
                                    else 
                                        axis += "-";

                                    if (inp.Id == 4 || inp.Id == 5)
                                        axis = "`Full Axis " + inp.Id + "+";

                                    return axis+"`";
                                };

                                ini.WriteValue(gcpad, value, axisValue(input, false));

                                string reverseAxis;
                                if (anyReverseAxes.TryGetValue(value, out reverseAxis))
                                    ini.WriteValue(gcpad, reverseAxis, axisValue(input, true));
                            }

                            // Z button is used to access test menu, do not map it with R1
                            if (triforce)
                                ini.WriteValue(gcpad, "Buttons/Z", "@(`Button 7`+`Button 8`)");
                        }
                    }

                    if (gc)
                    {
                        ini.WriteValue(gcpad, "Main Stick/Modifier/Range", "50.0");
                        ini.WriteValue(gcpad, "C-Stick/Modifier/Range", "50.0");

                        // DEADZONE
                        if (Program.SystemConfig.isOptSet("dolphin_gcpad_deadzone") && !string.IsNullOrEmpty(Program.SystemConfig["dolphin_gcpad_deadzone"]))
                        {
                            ini.WriteValue(gcpad, "Main Stick/Dead Zone", Program.SystemConfig["dolphin_gcpad_deadzone"]);
                            ini.WriteValue(gcpad, "C-Stick/Dead Zone", Program.SystemConfig["dolphin_gcpad_deadzone"]);
                        }
                        else
                        {
                            ini.WriteValue(gcpad, "Main Stick/Dead Zone", "10.0");
                            ini.WriteValue(gcpad, "C-Stick/Dead Zone", "10.0");
                        }

                        // SENSITIVITY
                        if (Program.SystemConfig.isOptSet("dolphin_gcpad_sensitivity") && !string.IsNullOrEmpty(Program.SystemConfig["dolphin_gcpad_sensitivity"]))
                        {
                            ini.WriteValue(gcpad, "Main Stick/Up/Range", Program.SystemConfig["dolphin_gcpad_sensitivity"]);
                            ini.WriteValue(gcpad, "Main Stick/Down/Range", Program.SystemConfig["dolphin_gcpad_sensitivity"]);
                            ini.WriteValue(gcpad, "Main Stick/Left/Range", Program.SystemConfig["dolphin_gcpad_sensitivity"]);
                            ini.WriteValue(gcpad, "Main Stick/Right/Range", Program.SystemConfig["dolphin_gcpad_sensitivity"]);
                            ini.WriteValue(gcpad, "C-Stick/Up/Range", Program.SystemConfig["dolphin_gcpad_sensitivity"]);
                            ini.WriteValue(gcpad, "C-Stick/Down/Range", Program.SystemConfig["dolphin_gcpad_sensitivity"]);
                            ini.WriteValue(gcpad, "C-Stick/Left/Range", Program.SystemConfig["dolphin_gcpad_sensitivity"]);
                            ini.WriteValue(gcpad, "C-Stick/Right/Range", Program.SystemConfig["dolphin_gcpad_sensitivity"]);
                        }
                        else
                        {
                            ini.WriteValue(gcpad, "Main Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Main Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Main Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Main Stick/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "C-Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "C-Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "C-Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "C-Stick/Right/Range", "100.0");
                        }

                        if (tech == "XInput")
                        {
                            ini.WriteValue(gcpad, "Main Stick/Calibration", "100.00 101.96 108.24 109.27 115.00 109.59 106.10 101.96 100.00 101.96 105.22 107.49 117.34 112.43 108.24 101.96 100.00 101.96 108.24 116.11 116.57 116.72 108.24 101.96 100.00 101.96 108.24 109.75 115.91 109.18 107.47 101.96");
                            ini.WriteValue(gcpad, "C-Stick/Calibration", "100.00 101.96 108.24 112.26 122.26 118.12 108.24 101.96 100.00 101.96 108.24 114.92 117.37 115.98 108.24 101.96 100.00 101.96 105.40 112.07 114.52 113.89 104.20 99.64 99.97 101.73 106.63 108.27 103.63 104.40 107.15 101.96");
                        }

                        if (prod == USB_PRODUCT.NINTENDO_SWITCH_PRO)
                        {
                            ini.WriteValue(gcpad, "Main Stick/Calibration", "98.50 101.73 102.04 106.46 104.62 102.21 102.00 100.53 97.00 96.50 99.95 100.08 102.40 99.37 99.60 100.17 99.60 100.14 98.87 100.48 102.45 101.12 100.92 97.92 99.00 99.92 100.83 100.45 102.27 98.45 97.16 97.36");
                            ini.WriteValue(gcpad, "C-Stick/Calibration", "98.19 101.79 101.37 102.32 103.05 101.19 99.56 99.11 98.45 100.60 98.65 100.67 99.85 97.31 97.24 96.36 95.94 97.94 98.17 100.24 99.22 98.10 99.69 98.77 97.14 100.45 99.08 100.13 102.61 101.37 100.55 97.03");
                        }

                        if (prod == USB_PRODUCT.SONY_DS3 ||
                            prod == USB_PRODUCT.SONY_DS4 ||
                            prod == USB_PRODUCT.SONY_DS4_DONGLE ||
                            prod == USB_PRODUCT.SONY_DS4_SLIM ||
                            prod == USB_PRODUCT.SONY_DS5)
                        {
                            ini.WriteValue(gcpad, "Main Stick/Calibration", "100.00 101.96 104.75 107.35 109.13 110.30 105.04 101.96 100.00 101.96 105.65 105.14 105.94 103.89 104.87 101.04 100.00 101.96 107.16 107.49 105.93 103.65 102.31 101.96 100.00 101.96 103.68 108.28 108.05 105.96 103.66 101.48");
                            ini.WriteValue(gcpad, "C-Stick/Calibration", "100.00 101.96 104.31 104.51 105.93 104.41 103.44 101.96 100.00 101.96 104.07 105.45 109.33 107.39 104.91 101.96 100.00 101.96 106.79 107.84 105.66 104.16 102.91 100.38 98.14 101.63 105.29 107.30 106.77 104.73 104.87 100.92");
                        }

                        else
                        {
                            ini.WriteValue(gcpad, "Main Stick/Calibration", "100.00 101.96 104.75 107.35 109.13 110.30 105.04 101.96 100.00 101.96 105.65 105.14 105.94 103.89 104.87 101.04 100.00 101.96 107.16 107.49 105.93 103.65 102.31 101.96 100.00 101.96 103.68 108.28 108.05 105.96 103.66 101.48");
                            ini.WriteValue(gcpad, "C-Stick/Calibration", "100.00 101.96 104.31 104.51 105.93 104.41 103.44 101.96 100.00 101.96 104.07 105.45 109.33 107.39 104.91 101.96 100.00 101.96 106.79 107.84 105.66 104.16 102.91 100.38 98.14 101.63 105.29 107.30 106.77 104.73 104.87 100.92");
                        }

                        // RUMBLE
                        if (rumble)
                        {
                            if (tech == "XInput")
                                ini.WriteValue(gcpad, "Rumble/Motor", "`Motor L`|`Motor R`");
                            else
                                ini.WriteValue(gcpad, "Rumble/Motor", "Motor");
                        }
                    }
                    
                    else
                    {
                        // DEAD ZONE
                        if (Program.SystemConfig.isOptSet("dolphin_wii_deadzone") && !string.IsNullOrEmpty(Program.SystemConfig["dolphin_wii_deadzone"]))
                        {
                            ini.WriteValue(gcpad, "Classic/Right Stick/Dead Zone", Program.SystemConfig["dolphin_wii_deadzone"]);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Dead Zone", Program.SystemConfig["dolphin_wii_deadzone"]);
                            ini.WriteValue(gcpad, "IR/Dead Zone", Program.SystemConfig["dolphin_wii_deadzone"]);
                            ini.WriteValue(gcpad, "Tilt/Dead Zone", Program.SystemConfig["dolphin_wii_deadzone"]);
                            ini.WriteValue(gcpad, "Swing/Dead Zone", Program.SystemConfig["dolphin_wii_deadzone"]);
                            ini.WriteValue(gcpad, "IMUGyroscope/Dead Zone", Program.SystemConfig["dolphin_wii_deadzone"]);
                            ini.WriteValue(gcpad, "Nunchuk/Tilt/Dead Zone", Program.SystemConfig["dolphin_wii_deadzone"]);
                            ini.WriteValue(gcpad, "Nunchuk/Swing/Dead Zone", Program.SystemConfig["dolphin_wii_deadzone"]);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Dead Zone", Program.SystemConfig["dolphin_wii_deadzone"]);
                        }
                        else
                        {
                            ini.WriteValue(gcpad, "Classic/Right Stick/Dead Zone", "10.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Dead Zone", "10.0");
                            ini.WriteValue(gcpad, "IR/Dead Zone", "10.0");
                            ini.WriteValue(gcpad, "Tilt/Dead Zone", "10.0");
                            ini.WriteValue(gcpad, "Swing/Dead Zone", "10.0");
                            ini.WriteValue(gcpad, "IMUGyroscope/Dead Zone", "10.0");
                            ini.WriteValue(gcpad, "Nunchuk/Tilt/Dead Zone", "10.0");
                            ini.WriteValue(gcpad, "Nunchuk/Swing/Dead Zone", "10.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Dead Zone", "10.0");
                        }

                        // SENSITIVITY
                        if (Program.SystemConfig.isOptSet("dolphin_wii_sensitivity") && !string.IsNullOrEmpty(Program.SystemConfig["dolphin_wii_sensitivity"]))
                        {
                            ini.WriteValue(gcpad, "IR/Up/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "IR/Down/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "IR/Left/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "IR/Right/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Tilt/Forward/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Tilt/Left/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Tilt/Backward/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Tilt/Right/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Swing/Up/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Swing/Down/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Swing/Left/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Swing/Right/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Up/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Down/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Left/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Right/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Up/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Down/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Left/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Right/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Classic/Right Stick/Up/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Classic/Right Stick/Down/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Classic/Right Stick/Left/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                            ini.WriteValue(gcpad, "Classic/Right Stick/Right/Range", Program.SystemConfig["dolphin_wii_sensitivity"]);
                        }
                        else
                        {
                            ini.WriteValue(gcpad, "IR/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "IR/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "IR/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "IR/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Tilt/Forward/Range", "100.0");
                            ini.WriteValue(gcpad, "Tilt/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Tilt/Backward/Range", "100.0");
                            ini.WriteValue(gcpad, "Tilt/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Swing/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Swing/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Swing/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Swing/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Right Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Right Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Right Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Right Stick/Right/Range", "100.0");
                        }

                        if (Program.SystemConfig["controller_mode"] == "cc" || Program.SystemConfig["controller_mode"] == "ccp")
                        {
                            if (prod == USB_PRODUCT.NINTENDO_SWITCH_PRO)
                            {
                                ini.WriteValue(gcpad, "Classic/Left Stick/Calibration", "98.50 101.73 102.04 106.46 104.62 102.21 102.00 100.53 97.00 96.50 99.95 100.08 102.40 99.37 99.60 100.17 99.60 100.14 98.87 100.48 102.45 101.12 100.92 97.92 99.00 99.92 100.83 100.45 102.27 98.45 97.16 97.36");
                                ini.WriteValue(gcpad, "Classic/Right Stick/Calibration", "98.19 101.79 101.37 102.32 103.05 101.19 99.56 99.11 98.45 100.60 98.65 100.67 99.85 97.31 97.24 96.36 95.94 97.94 98.17 100.24 99.22 98.10 99.69 98.77 97.14 100.45 99.08 100.13 102.61 101.37 100.55 97.03");
                            }
                            else if (prod == USB_PRODUCT.SONY_DS3 ||
                            prod == USB_PRODUCT.SONY_DS4 ||
                            prod == USB_PRODUCT.SONY_DS4_DONGLE ||
                            prod == USB_PRODUCT.SONY_DS4_SLIM ||
                            prod == USB_PRODUCT.SONY_DS5)
                            {
                                ini.WriteValue(gcpad, "Classic/Left Stick/Calibration", "100.00 101.96 104.75 107.35 109.13 110.30 105.04 101.96 100.00 101.96 105.65 105.14 105.94 103.89 104.87 101.04 100.00 101.96 107.16 107.49 105.93 103.65 102.31 101.96 100.00 101.96 103.68 108.28 108.05 105.96 103.66 101.48");
                                ini.WriteValue(gcpad, "Classic/Right Stick/Calibration", "100.00 101.96 104.31 104.51 105.93 104.41 103.44 101.96 100.00 101.96 104.07 105.45 109.33 107.39 104.91 101.96 100.00 101.96 106.79 107.84 105.66 104.16 102.91 100.38 98.14 101.63 105.29 107.30 106.77 104.73 104.87 100.92");
                            }
                            else
                            {
                                ini.WriteValue(gcpad, "Classic/Left Stick/Calibration", "100.00 101.96 104.75 107.35 109.13 110.30 105.04 101.96 100.00 101.96 105.65 105.14 105.94 103.89 104.87 101.04 100.00 101.96 107.16 107.49 105.93 103.65 102.31 101.96 100.00 101.96 103.68 108.28 108.05 105.96 103.66 101.48");
                                ini.WriteValue(gcpad, "Classic/Right Stick/Calibration", "100.00 101.96 104.31 104.51 105.93 104.41 103.44 101.96 100.00 101.96 104.07 105.45 109.33 107.39 104.91 101.96 100.00 101.96 106.79 107.84 105.66 104.16 102.91 100.38 98.14 101.63 105.29 107.30 106.77 104.73 104.87 100.92");
                            }
                        }
                        if (Program.SystemConfig.isOptSet("wii_motionpad") && Program.SystemConfig.getOptBoolean("wii_motionpad"))
                        {
                            ini.WriteValue(gcpad, "IMUAccelerometer/Up", "`Accel Up`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Down", "`Accel Down`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Left", "`Accel Left`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Right", "`Accel Right`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Forward", "`Accel Forward`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Backward", "`Accel Backward`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Pitch Up", "`Gyro Pitch Up`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Pitch Down", "`Gyro Pitch Down`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Roll Left", "`Gyro Roll Left`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Roll Right", "`Gyro Roll Right`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Yaw Left", "`Gyro Yaw Left`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Yaw Right", "`Gyro Yaw Right`");
                            ini.Remove(gcpad, "Tilt/Forward");
                            ini.Remove(gcpad, "Tilt/Left");
                            ini.Remove(gcpad, "Tilt/Right");
                            ini.Remove(gcpad, "Tilt/Backward");
                            ini.Remove(gcpad, "Shake/X");
                            ini.Remove(gcpad, "Shake/Y");
                            ini.Remove(gcpad, "Shake/Z");
                            ini.Remove(gcpad, "Swing/Down");
                            ini.Remove(gcpad, "Swing/Right");
                            ini.Remove(gcpad, "Swing/Up");
                            ini.Remove(gcpad, "Swing/Left");
                        }
                    }
                }

                ini.Save();
            }

            // Reset hotkeys
            string hotkeyini = Path.Combine(path, "User", "Config", "Hotkeys.ini");
            if (File.Exists(hotkeyini))
                ResetHotkeysToDefault(hotkeyini);
        }

        private static void ResetHotkeysToDefault(string iniFile)
        {
            if (Program.Controllers.Count == 0)
                return;

            using (IniFile ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                var c1 = Program.Controllers.FirstOrDefault(c => c.PlayerIndex == 1);

                string tech = "XInput";
                string deviceName = "Gamepad";

                if (c1.Config.Type == "keyboard")
                {
                    tech = "DInput";
                    deviceName = "Keyboard Mouse";
                }
                else if (!c1.IsXInputDevice)
                {
                    var s = c1.SdlController;
                    if (s != null)
                    {
                        tech = "SDL";
                        deviceName = s.Name;
                    }
                }

                var ssss = "@(" + (GetSDLMappingName(c1, InputKey.hotkey) ?? "") + "&" + (GetSDLMappingName(c1, InputKey.y) ?? "") + ")";

                ini.WriteValue("Hotkeys", "Load State/Load State Slot 1", "F1");
                ini.WriteValue("Hotkeys", "Save State/Save State Slot 1", "@(Shift+F1)");

                if (tech == "XInput")
                {
                    ini.WriteValue("Hotkeys", "Device", tech + "/" + "0" + "/" + deviceName);
                    ini.WriteValue("Hotkeys", "General/Toggle Pause", "Back&`Button B`");
                    ini.WriteValue("Hotkeys", "General/Toggle Fullscreen", "Back&`Button A`");
                    ini.WriteValue("Hotkeys", "General/Exit", "Back&Start");
                                        
                    // SaveStates
                    ini.WriteValue("Hotkeys", "General/Take Screenshot", "@(Back+`Button X`)"); // Use Same value as SaveState....
                    ini.WriteValue("Hotkeys", "Save State/Save to Selected Slot", "@(Back+`Button X`)");
                    ini.WriteValue("Hotkeys", "Load State/Load from Selected Slot", "@(Back+`Button Y`)");                    
                    ini.WriteValue("Hotkeys", "Other State Hotkeys/Increase Selected State Slot", "@(Back+`Pad N`)");
                    ini.WriteValue("Hotkeys", "Other State Hotkeys/Decrease Selected State Slot", "@(Back+`Pad S`)");

                    ini.WriteValue("Hotkeys", "General/Eject Disc", "Back&`Shoulder L`");
                    ini.WriteValue("Hotkeys", "General/Change Disc", "Back&`Shoulder R`");
                }
                else if (tech == "SDL")
                {
                    bool revert = c1.VendorID == USB_VENDOR.NINTENDO;
                    ini.WriteValue("Hotkeys", "Device", tech + "/" + "0" + "/" + deviceName);
                    ini.WriteValue("Hotkeys", "General/Toggle Pause", revert ? "`Button 4`&`Button 0`" : "`Button 4`&`Button 1`");
                    ini.WriteValue("Hotkeys", "General/Toggle Fullscreen", revert ? "`Button 4`&`Button 1`" : "`Button 4`&`Button 0`");

                    ini.WriteValue("Hotkeys", "General/Exit", "`Button 4`&`Button 6`");

                    var save = "@(" + (GetSDLMappingName(c1, InputKey.hotkey) ?? "") + "&" + (GetSDLMappingName(c1, InputKey.y) ?? "") + ")";
                    ini.WriteValue("Hotkeys", "General/Take Screenshot", save); // Use Same value as SaveState....
                    ini.WriteValue("Hotkeys", "Save State/Save to Selected Slot", save);
                    ini.WriteValue("Hotkeys", "Load State/Load from Selected Slot", "@(" + (GetSDLMappingName(c1, InputKey.hotkey) ?? "") + "&" + (GetSDLMappingName(c1, InputKey.x) ?? "") + ")");                    

                    // Save State/Save to Selected Slot = @(`Button 6`+`Button 2`)

                    ini.WriteValue("Hotkeys", "General/Take Screenshot", "`Button 4`&`Full Axis 5+`");

                    ini.WriteValue("Hotkeys", "General/Eject Disc", "`Button 4`&`Button 9`");
                    ini.WriteValue("Hotkeys", "General/Change Disc", "`Button 4`&`Button 10`");
                }
                else        // Keyboard
                {
                    ini.WriteValue("Hotkeys", "Device", "DInput/0/Keyboard Mouse");
                    ini.WriteValue("Hotkeys", "General/Toggle Pause", "`F10`");
                    ini.WriteValue("Hotkeys", "General/Toggle Fullscreen", "@(Alt+RETURN)");
                    ini.WriteValue("Hotkeys", "General/Exit", "ESCAPE");
                 //   ini.WriteValue("Hotkeys", "Load State/Load State Slot 1", "`F1`");
                 //   ini.WriteValue("Hotkeys", "Save State/Save State Slot 1", "Alt&`F1");
                    ini.WriteValue("Hotkeys", "General/Take Screenshot", "`F9`");
                    ini.WriteValue("Hotkeys", "General/Eject Disc", "Alt&E");
                    ini.WriteValue("Hotkeys", "General/Change Disc", "Alt&S");
                }
            }
        }

        private static void removeControllerConfig_gamecube(string path, string filename)
        {
            string iniFile = Path.Combine(path, "User", "Config", filename);

            using (var ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                for (int i = 0; i < 4; i++)
                    ini.WriteValue("Core", "SIDevice" + i, "0");
            }
        }

        private static string ToDolphinKey(long id)
        {
            if (id >= 97 && id <= 122)
            {
                List<int> azertyLayouts = new List<int>() { 1036, 2060, 3084, 5132, 4108 };
                if (azertyLayouts.Contains(CultureInfo.CurrentCulture.KeyboardLayoutId))
                {
                    if (id == 'a')
                        id = 'q';
                    else if (id == 'q')
                        id = 'a';
                    else if (id == 'w')
                        id = 'z';
                    else if (id == 'z')
                        id = 'w';
                }
                return ((char)id).ToString().ToUpper();
            }
                        
            switch (id)
            {
                case 32: return "SPACE";
                case 13:
                case 0x4000009e: return "RETURN"; // Return2

                case 0x400000e1: return "LSHIFT"; // Shift = 
                case 0x400000e0: return "LCONTROL"; // Ctrl = 
                case 0x400000e2: return "LMENU"; // Alt = 

                case 0x4000004b: return "PRIOR"; // PageUp = ,
                case 0x4000004e: return "NEXT"; // PageDown = ,
                case 0x4000004d: return "END"; // End = ,
                case 0x4000004a: return "HOME"; // Home = ,
                case 0x40000050: return "LEFT"; // Left = ,
                case 0x40000052: return "UP"; // Up = ,
                case 0x4000004f: return "RIGHT"; // Right = ,
                case 0x40000051: return "DOWN"; // Down = 0x40000051,

                case 0x40000049: return "INSERT"; // Insert = 0x40000049,
                case 0x0000007f: return "DELETE"; // Delete = 0x0000007f,

                case 0x40000059: return "NUMPAD1";  //KP_1 = 0x40000059,
                case 0X4000005A: return "NUMPAD2";  //KP_2 = 0X4000005A,
                case 0x4000005b: return "NUMPAD3";  // KP_3 = ,
                case 0x4000005c: return "NUMPAD4"; // KP_4 = ,
                case 0x4000005d: return "NUMPAD5"; // KP_5 = ,
                case 0x4000005e: return "NUMPAD6"; // KP_6 = ,
                case 0x4000005f: return "NUMPAD7"; // KP_7 = ,
                case 0x40000060: return "NUMPAD8"; // KP_8 = ,
                case 0x40000061: return "NUMPAD9"; // KP_9 = ,
                case 0x40000062: return "NUMPAD0";  // KP_0 = 0x40000062,
                case 0x40000055: return "MULTIPLY"; // KP_Multiply
                case 0x40000057: return "ADD"; // KP_Plus
                case 0x40000056: return "SUBSTRACT"; // KP_Minus

                case 0x4000003a: return "F1"; // F1
                case 0x4000003b: return "F2"; // F2
                case 0x4000003c: return "F3"; // F3
                case 0x4000003d: return "F4"; // F4
                case 0x4000003e: return "F5"; // F5
                case 0x4000003f: return "F6"; // F6
                case 0x40000040: return "F7"; // F7
                case 0x40000041: return "F8"; // F8
                case 0x40000042: return "F9"; // F9
                case 0x40000043: return "F10"; // F10
                case 0x40000044: return "F11"; // F11
                case 0x40000045: return "F12"; // F12
                case 0x400000e6: return "RMENU"; // RightAlt
                case 0x400000e4: return "RCONTROL"; // RightCtrl
                case 0x400000e5: return "RSHIFT"; // RightShift
                case 0x40000058: return "NUMPADENTER"; // Kp_ENTER
                /*        
                KP_Period = 0x40000063,
                KP_Divide = 0x40000054,
                   
                NumlockClear = 0x40000053,
                ScrollLock = 0x40000047,                
                 * //Select = 0x40000077,
                //PrintScreen = 0x40000046,
                //LeftGui = 0x400000e3,
                //RightGui = 0x400000e7,
                //Application = 0x40000065,            
                //Gui = 0x400000e3,
                //Pause = 0x40000048,
                //Capslock = 0x40000039,

                 */
            }

            return null;
        }
    }
}
