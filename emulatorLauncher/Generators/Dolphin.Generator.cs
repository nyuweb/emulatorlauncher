﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.FileFormats;

namespace EmulatorLauncher
{
    class DolphinGenerator : Generator
    {
        public DolphinGenerator()
        {
            DependsOnDesktopResolution = true;
        }

        private SaveStatesWatcher _saveStatesWatcher;

        public override void Cleanup()
        {
            if (_saveStatesWatcher != null)
            {
                _saveStatesWatcher.Dispose();
                _saveStatesWatcher = null;
            }

            base.Cleanup();
        }

        private BezelFiles _bezelFileInfo;
        private ScreenResolution _resolution;
        private bool _triforce = false;
        private Rectangle _windowRect = Rectangle.Empty;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            _triforce = (emulator == "dolphin-triforce" || core == "dolphin-triforce" || emulator == "triforce" || core == "triforce");

            string folderName = _triforce ? "dolphin-triforce" : "dolphin-emu";

            string path = AppConfig.GetFullPath(folderName);
            if (!_triforce && string.IsNullOrEmpty(path))
                path = AppConfig.GetFullPath("dolphin");

            if (string.IsNullOrEmpty(path))
                return null;

            string exe = Path.Combine(path, "Dolphin.exe");
            if (!File.Exists(exe))
            {                
                exe = Path.Combine(path, "DolphinWX.exe");
                _triforce = File.Exists(exe);
            }

            if (!File.Exists(exe))
                return null;

            string portableFile = Path.Combine(path, "portable.txt");
            if (!File.Exists(portableFile))
                File.WriteAllText(portableFile, "");

            if ((system == "gamecube" && SystemConfig["ratio"] == "") || SystemConfig["ratio"] == "4/3")
                _bezelFileInfo = BezelFiles.GetBezelFiles(system, rom, resolution);

            _resolution = resolution;

            if (system == "wii")
            {
                string sysconf = Path.Combine(path, "User", "Wii", "shared2", "sys", "SYSCONF");
                if (File.Exists(sysconf))
                    writeWiiSysconfFile(sysconf);
            }
            
            SetupGeneralConfig(path, system, emulator, core, rom);
            SetupGfxConfig(path);
            SetupStateSlotConfig(path);

            DolphinControllers.WriteControllersConfig(path, system, rom, _triforce);

            if (Path.GetExtension(rom).ToLowerInvariant() == ".m3u")
                rom = rom.Replace("\\", "/");

            string saveState = "";
            if (File.Exists(SystemConfig["state_file"]))
                saveState = " --save_state=\"" + Path.GetFullPath(SystemConfig["state_file"]) + "\"";

            return new ProcessStartInfo()
            {
                FileName = exe,
                Arguments = "-b -e \"" + rom + "\"" + saveState,
                WorkingDirectory = path,
                WindowStyle = (_bezelFileInfo == null ? ProcessWindowStyle.Normal : ProcessWindowStyle.Maximized)
            };
        }

        /// <summary>
        /// Works since to this PR : https://github.com/dolphin-emu/dolphin/pull/12201
        /// </summary>
        /// <param name="path"></param>
        private void SetupStateSlotConfig(string path)
        {
            if (_saveStatesWatcher == null)
                return;

            var slot = _saveStatesWatcher.Slot;

            int id = Math.Max(1, ((slot - 1) % 10) + 1);

            string iniFile = Path.Combine(path, "User", "Config", "QT.ini");
            using (var ini = new IniFile(iniFile, IniOptions.UseSpaces))
                ini.WriteValue("Emulation", "StateSlot", id.ToString());
        }

        private void SetupGfxConfig(string path)
        {
            string iniFile = Path.Combine(path, "User", "Config", "GFX.ini");

            try
            {
                using (var ini = new IniFile(iniFile, IniOptions.UseSpaces))
                {
					
					if (SystemConfig.isOptSet("ratio"))
					{
						if (SystemConfig["ratio"] == "4/3")
						{
							ini.WriteValue("Settings", "AspectRatio", "2");
						}
						else if (SystemConfig["ratio"] == "16/9")
							ini.WriteValue("Settings", "AspectRatio", "1");
						else if (SystemConfig["ratio"] == "Stretched")
							ini.WriteValue("Settings", "AspectRatio", "3");
					}
					else
						ini.WriteValue("Settings", "AspectRatio", "0");
					
					// widescreen hack but only if enable cheats is not enabled - Default Off
					if (SystemConfig.isOptSet("widescreen_hack") && SystemConfig.getOptBoolean("widescreen_hack"))
					{
						ini.WriteValue("Settings", "wideScreenHack", "True");

                        // Set Stretched only if ratio is not forced to 16/9 
                        if (!SystemConfig.isOptSet("ratio") || SystemConfig["ratio"] != "16/9")
                        {
                            _bezelFileInfo = null;
                            ini.WriteValue("Settings", "AspectRatio", "3");
                        }
					}
                    else
                        ini.Remove("Settings", "wideScreenHack");

                    // draw or not FPS
                    BindBoolIniFeature(ini, "Settings", "ShowFPS", "DrawFramerate", "True", "False");

                    if (_bezelFileInfo != null)
                        ini.WriteValue("Settings", "BorderlessFullscreen", "True");
                    else 
                        ini.WriteValue("Settings", "BorderlessFullscreen", "False");

                    ini.WriteValue("Hardware", "VSync", SystemConfig["VSync"] != "false" ? "True" : "False");

                    // internal resolution
                    BindIniFeature(ini, "Settings", "InternalResolution", "internal_resolution", "0");

                    // HiResTextures
                    BindBoolIniFeature(ini, "Settings", "HiresTextures", "hires_textures", "True", "False");
                    BindBoolIniFeature(ini, "Settings", "CacheHiresTextures", "CacheHiresTextures", "True", "False");

                    // anisotropic filtering - Auto 0
                    BindIniFeature(ini, "Enhancements", "MaxAnisotropy", "anisotropic_filtering", "0");

                    // antialiasing (new dolhpin version adds SSAA)
                    BindBoolIniFeature(ini, "Settings", "SSAA", "ssaa", "true", "false");
                    
                    if (SystemConfig.isOptSet("antialiasing"))
                        ini.WriteValue("Settings", "MSAA", "0x0000000" + SystemConfig["antialiasing"]);
                    else
                    {
                        ini.WriteValue("Settings", "MSAA", "0x00000001");
                        ini.WriteValue("Settings", "SSAA", "false");
                    }

                    // various performance hacks - Default Off
                    if (SystemConfig.isOptSet("perf_hacks"))
                    {
                        if (SystemConfig.getOptBoolean("perf_hacks"))
                        {
                            ini.WriteValue("Hacks", "BBoxEnable", "False");
                            ini.WriteValue("Hacks", "SkipDuplicateXFBs", "True");
                            ini.WriteValue("Hacks", "XFBToTextureEnable", "True");
                            ini.WriteValue("Enhancements", "ArbitraryMipmapDetection", "True");
                            ini.WriteValue("Enhancements", "DisableCopyFilter", "True");
                            ini.WriteValue("Enhancements", "ForceTrueColor", "True");
                        }
                        else
                        {
                            ini.Remove("Hacks", "BBoxEnable");
                            ini.Remove("Hacks", "SkipDuplicateXFBs");
                            ini.Remove("Hacks", "XFBToTextureEnable");
                            ini.Remove("Enhancements", "ArbitraryMipmapDetection");
                            ini.Remove("Enhancements", "DisableCopyFilter");
                            ini.Remove("Enhancements", "ForceTrueColor");
                        }
                    }

                    // shaders compilation
                    BindBoolIniFeature(ini, "Settings", "WaitForShadersBeforeStarting", "WaitForShadersBeforeStarting", "True", "False");
                    BindIniFeature(ini, "Settings", "ShaderCompilationMode", "ShaderCompilationMode", "2");

                    // Skip EFB Access
                    BindIniFeature(ini, "Hacks", "EFBAccessEnable", "EFBAccessEnable", "False");

                    // Scaled EFB copy
                    BindIniFeature(ini, "Hacks", "EFBScaledCopy", "EFBScaledCopy", "True");

                    // EFB emulate format
                    BindIniFeature(ini, "Hacks", "EFBEmulateFormatChanges", "EFBEmulateFormatChanges", "True");

                    // Store EFB Copies
                    if (Features.IsSupported("EFBCopies"))
                    {
                        if (SystemConfig["EFBCopies"] == "efb_to_texture_defer")
                        {
                            ini.WriteValue("Hacks", "EFBToTextureEnable", "True");
                            ini.WriteValue("Hacks", "DeferEFBCopies", "True");
                        }
                        else if (SystemConfig["EFBCopies"] == "efb_to_ram_defer")
                        {
                            ini.WriteValue("Hacks", "EFBToTextureEnable", "False");
                            ini.WriteValue("Hacks", "DeferEFBCopies", "True");
                        }
                        else if (SystemConfig["EFBCopies"] == "efb_to_ram")
                        {
                            ini.WriteValue("Hacks", "EFBToTextureEnable", "False");
                            ini.WriteValue("Hacks", "DeferEFBCopies", "False");
                        }
                        else
                        {
                            ini.Remove("Hacks", "EFBToTextureEnable");
                            ini.Remove("Hacks", "DeferEFBCopies");
                        }
                    }

                    // Force texture filtering
                    BindIniFeature(ini, "Enhancements", "ForceFiltering", "ForceFiltering", "False");

                    // Shaders
                    BindIniFeature(ini, "Enhancements", "PostProcessingShader", "dolphin_shaders", "(off)");

                    // Hack vertex rounding
                    BindBoolIniFeature(ini, "Hacks", "VertexRounding", "VertexRounding", "True", "False");
                    BindBoolIniFeature(ini, "Hacks", "VISkip", "VISkip", "True", "False");
                    BindBoolIniFeature(ini, "Hacks", "FastTextureSampling", "manual_texture_sampling", "False", "True");
                }
            }
            catch { }
        }
    
        private string getGameCubeLangFromEnvironment()
        {
            var availableLanguages = new Dictionary<string, string>() 
            { 
                {"en", "0" }, { "de", "1" }, { "fr", "2" }, { "es", "3" }, { "it", "4" }, { "nl", "5" } 
            };

            var lang = GetCurrentLanguage();
            if (!string.IsNullOrEmpty(lang))
            {
                string ret;
                if (availableLanguages.TryGetValue(lang, out ret))
                    return ret;
            }

            return "0";
        }

        private int getWiiLangFromEnvironment()
        {
            var availableLanguages = new Dictionary<string, int>()
            {
                {"jp", 0 }, {"en", 1 }, { "de", 2 }, { "fr", 3 }, { "es", 4 }, { "it", 5 }, { "nl", 6 }
            };

            var lang = GetCurrentLanguage();
            if (!string.IsNullOrEmpty(lang))
            {
                int ret;
                if (availableLanguages.TryGetValue(lang, out ret))
                    return ret;
            }

            return 1;
        }

        private void writeWiiSysconfFile(string path)
        {
            if (!File.Exists(path))
                return;

            int langId = 1;
            int barPos = 0;

            if (SystemConfig.isOptSet("wii_language") && !string.IsNullOrEmpty(SystemConfig["wii_language"]))
                langId = SystemConfig["wii_language"].ToInteger();
            else
                langId = getWiiLangFromEnvironment();

            if (SystemConfig.isOptSet("sensorbar_position") && !string.IsNullOrEmpty(SystemConfig["sensorbar_position"]))
                barPos = SystemConfig["sensorbar_position"].ToInteger();

            // Read SYSCONF file
            byte[] bytes = File.ReadAllBytes(path);

            // Search IPL.LNG pattern and replace with target language
            byte[] langPattern = new byte[] { 0x49, 0x50, 0x4C, 0x2E, 0x4C, 0x4E, 0x47 };
            int index = bytes.IndexOf(langPattern);
            if (index >= 0 && index + langPattern.Length + 1 < bytes.Length)
            {
                var toSet = new byte[] { 0x49, 0x50, 0x4C, 0x2E, 0x4C, 0x4E, 0x47, (byte)langId };
                for (int i = 0; i < toSet.Length; i++)
                    bytes[index + i] = toSet[i];
            }

            // Search BT.BAR pattern and replace with target position
            byte[] barPositionPattern = new byte[] { 0x42, 0x54, 0x2E, 0x42, 0x41, 0x52 };
            int index2 = bytes.IndexOf(barPositionPattern);
            if (index >= 0 && index + langPattern.Length + 1 < bytes.Length)
            {
                var toSet = new byte[] { 0x42, 0x54, 0x2E, 0x42, 0x41, 0x52, (byte)barPos };
                for (int i = 0; i < toSet.Length; i++)
                    bytes[index2 + i] = toSet[i];
            }

            File.WriteAllBytes(path, bytes);
        }

        private void SetupGeneralConfig(string path, string system, string emulator, string core, string rom)
        {
            string iniFile = Path.Combine(path, "User", "Config", "Dolphin.ini");
            
            try
            {
                using (var ini = new IniFile(iniFile, IniOptions.UseSpaces | IniOptions.KeepEmptyValues))
                {
                    Rectangle emulationStationBounds;
                    if (IsEmulationStationWindowed(out emulationStationBounds, true) && !SystemConfig.getOptBoolean("forcefullscreen"))
                    {
                        _windowRect = emulationStationBounds;
                        _bezelFileInfo = null;
                        ini.WriteValue("Display", "Fullscreen", "False");
                    }
                    else
                        ini.WriteValue("Display", "Fullscreen", "True");

                    // Draw FPS
                    if (SystemConfig.isOptSet("DrawFramerate") && SystemConfig.getOptBoolean("DrawFramerate"))
                    {
                        ini.WriteValue("General", "ShowLag", "True");
                        ini.WriteValue("General", "ShowFrameCount", "True");
                    }
                    else
                    {
                        ini.WriteValue("General", "ShowLag", "False");
                        ini.WriteValue("General", "ShowFrameCount", "False");
                    }

                    // Discord
                    BindBoolIniFeature(ini, "General", "UseDiscordPresence", "discord", "True", "False");

                    // Skip BIOS
                    BindBoolIniFeature(ini, "Core", "SkipIPL", "skip_bios", "False", "True");

                    // OSD Messages
                    BindBoolIniFeature(ini, "Interface", "OnScreenDisplayMessages", "OnScreenDisplayMessages", "False", "True");

                    // don't ask about statistics
                    ini.WriteValue("Analytics", "PermissionAsked", "True");

                    // don't confirm at stop
                    ini.WriteValue("Interface", "ConfirmStop", "False");

                    ini.WriteValue("Display", "KeepWindowOnTop", "False");

                    // language (for gamecube at least)
                    if (Features.IsSupported("gamecube_language") && SystemConfig.isOptSet("gamecube_language"))
                    {
                        ini.WriteValue("Core", "SelectedLanguage", SystemConfig["gamecube_language"]);
                        ini.WriteValue("Core", "GameCubeLanguage", SystemConfig["gamecube_language"]);
                    }
                    else
                    {
                        ini.WriteValue("Core", "SelectedLanguage", getGameCubeLangFromEnvironment());
                        ini.WriteValue("Core", "GameCubeLanguage", getGameCubeLangFromEnvironment());
                    }

                    // Audio
                    if (SystemConfig.isOptSet("enable_dpl2") && SystemConfig.getOptBoolean("enable_dpl2"))
                    {
                        ini.WriteValue("Core", "DSPHLE", "False");
                        ini.WriteValue("Core", "DPL2Decoder", "True");
                        ini.WriteValue("DSP", "EnableJIT", "True");
                    }
                    else
                    {
                        ini.WriteValue("Core", "DSPHLE", "True");
                        ini.WriteValue("Core", "DPL2Decoder", "False");
                        ini.WriteValue("DSP", "EnableJIT", "False");
                    }

                    BindIniFeature(ini, "DSP", "Backend", "audiobackend", "Cubeb");

                    // Video backend - Default
                    BindIniFeature(ini, "Core", "GFXBackend", "gfxbackend", "Vulkan");

                    // Cheats - default false
                    if (!_triforce)
                        BindBoolIniFeature(ini, "Core", "EnableCheats", "enable_cheats", "True", "False");

                    // Fast Disc Speed - Default Off
                    BindBoolIniFeature(ini, "Core", "FastDiscSpeed", "enable_fastdisc", "True", "False");

                    // Enable MMU - Default On
                    BindBoolIniFeature(ini, "Core", "MMU", "enable_mmu", "True", "False");

                    // CPU Thread (Dual Core)
                    BindBoolIniFeature(ini, "Core", "CPUThread", "CPUThread", "True", "False");

                    // gamecube pads forced as standard pad
                    bool emulatedWiiMote = (system == "wii" && Program.SystemConfig.isOptSet("emulatedwiimotes") && Program.SystemConfig.getOptBoolean("emulatedwiimotes"));
                    
                    // wiimote scanning
                    if (emulatedWiiMote || system == "gamecube" || _triforce)
                        ini.WriteValue("Core", "WiimoteContinuousScanning", "False");
                    else
                        ini.WriteValue("Core", "WiimoteContinuousScanning", "True");

                    // Write texture paths
                    if (!_triforce)
                    {
                        string savesPath = AppConfig.GetFullPath("saves");
                        string dolphinLoadPath = Path.Combine(savesPath, "gamecube", "User", "Load");
                        if (!Directory.Exists(dolphinLoadPath)) try { Directory.CreateDirectory(dolphinLoadPath); }
                            catch { }
                        string dolphinResourcesPath = Path.Combine(savesPath, "gamecube", "User", "ResourcePacks");
                        if (!Directory.Exists(dolphinResourcesPath)) try { Directory.CreateDirectory(dolphinResourcesPath); }
                            catch { }

                        ini.WriteValue("General", "LoadPath", dolphinLoadPath);
                        ini.WriteValue("General", "ResourcePackPath", dolphinResourcesPath);

                        if (Program.HasEsSaveStates && Program.EsSaveStates.IsEmulatorSupported(emulator))
                        {
                            string localPath = Program.EsSaveStates.GetSavePath(system, emulator, core);

                            _saveStatesWatcher = new DolphinSaveStatesMonitor(rom, Path.Combine(path, "User", "StateSaves"), localPath);
                            _saveStatesWatcher.PrepareEmulatorRepository();
                        }
                    }

                    // Add rom path to isopath
                    AddPathToIsoPath(Path.GetFullPath(Path.GetDirectoryName(rom)), ini);

                    // Triforce specifics (AM-baseboard in SID devices, panic handlers)
                    if (_triforce)
                    {
                        ini.WriteValue("Core", "SerialPort1", "6");                     // AM Baseboard
                        ini.WriteValue("Core", "SIDevice0", "11");                      // AM Baseboard player 1
                        ini.WriteValue("Core", "SIDevice1", "11");                      // AM Baseboard player 2
                        ini.WriteValue("Core", "SIDevice2", "0");
                        ini.WriteValue("Core", "SIDevice3", "0");
                        ini.WriteValue("Interface", "UsePanicHandlers", "False");       // Disable panic handlers
                        ini.WriteValue("Core", "EnableCheats", "True");                 // Cheats must be enabled
                    }

                    // Set SID devices (controllers)
                    else if (!((Program.SystemConfig.isOptSet("disableautocontrollers") && Program.SystemConfig["disableautocontrollers"] == "1")))
                    {
                        bool wiiGCPad = system == "wii" && SystemConfig.isOptSet("wii_gamecube") && SystemConfig.getOptBoolean("wii_gamecube");
                        for (int i = 0; i < 4; i++)
                        {
                            var ctl = Controllers.FirstOrDefault(c => c.PlayerIndex == i + 1);
                            bool gcPad = (system == "gamecube" && SystemConfig.isOptSet("gamecubepad" + i) && SystemConfig.getOptBoolean("gamecubepad" + i));

                            if (wiiGCPad || gcPad)
                                ini.WriteValue("Core", "SIDevice" + i, "12");

                            else if (ctl != null && ctl.Config != null)
                                ini.WriteValue("Core", "SIDevice" + i, "6");
                            
                            else
                                ini.WriteValue("Core", "SIDevice" + i, "0");
                        }
                    }
                    // Disable auto updates
                    //ini.AppendValue("AutoUpdate", "UpdateTrack", "");
                }
            }
            catch { }
        }

        private static void AddPathToIsoPath(string romPath, IniFile ini)
        {
            int isoPathsCount = (ini.GetValue("General", "ISOPaths") ?? "0").ToInteger();
            for (int i = 0; i < isoPathsCount; i++)
            {
                var isoPath = ini.GetValue("General", "ISOPath" + i);
                if (isoPath != null && Path.GetFullPath(isoPath).Equals(romPath, StringComparison.InvariantCultureIgnoreCase))
                    return;
            }

            ini.WriteValue("General", "ISOPaths", (isoPathsCount + 1).ToString());
            ini.WriteValue("General", "ISOPath" + isoPathsCount, romPath);
        }

        public override int RunAndWait(ProcessStartInfo path)
        {
            FakeBezelFrm bezel = null;

            if (_bezelFileInfo != null)
                bezel = _bezelFileInfo.ShowFakeBezel(_resolution);

            int ret = 0;

            if (_windowRect.IsEmpty)
                ret = base.RunAndWait(path);
            else
            {
                var process = Process.Start(path);

                while (process != null)
                {
                    try
                    {
                        var hWnd = process.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
                            User32.SetWindowPos(hWnd, IntPtr.Zero, _windowRect.Left, _windowRect.Top, _windowRect.Width, _windowRect.Height, SWP.NOZORDER);
                            break;
                        }
                    }
                    catch { }

                    if (process.WaitForExit(1))
                    {
                        try { ret = process.ExitCode; }
                        catch { }
                        process = null;
                        break;
                    }

                }

                if (process != null)
                {
                    process.WaitForExit();
                    try { ret = process.ExitCode; }
                    catch { }
                }
            }

            if (bezel != null)
                bezel.Dispose();

            return ret;
        }
    }
}
