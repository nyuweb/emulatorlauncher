﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.FileFormats;

namespace EmulatorLauncher
{
    class mGBAGenerator : Generator
    {
        private BezelFiles _bezelFileInfo;
        private ScreenResolution _resolution;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            string path = AppConfig.GetFullPath("mgba");

            string exe = Path.Combine(path, "mgba.exe");
            if (!File.Exists(exe))
                return null;

            //Applying bezels
            if (!ReshadeManager.Setup(ReshadeBezelType.opengl, ReshadePlatform.x64, system, rom, path, resolution))
                _bezelFileInfo = BezelFiles.GetBezelFiles(system, rom, resolution);

            _resolution = resolution;
            bool fullscreen = !IsEmulationStationWindowed() || SystemConfig.getOptBoolean("forcefullscreen");

            SetupConfiguration(path, rom, system);

            var commandArray = new List<string>();

            if (fullscreen)
                commandArray.Add("-f");

            commandArray.Add("\"" + rom + "\"");

            string args = string.Join(" ", commandArray);

            return new ProcessStartInfo()
            {
                FileName = exe,
                WorkingDirectory = path,
                Arguments = args,
            };
        }

        public override int RunAndWait(ProcessStartInfo path)
        {
            FakeBezelFrm bezel = null;

            if (_bezelFileInfo != null)
                bezel = _bezelFileInfo.ShowFakeBezel(_resolution);

            int ret = base.RunAndWait(path);

            if (bezel != null)
                bezel.Dispose();

            if (ret == 1)
                return 0;

            return ret;
        }

        private void SetupConfiguration(string path, string rom, string system)
        {
            string conf = Path.Combine(path, "config.ini");

            using (var ini = IniFile.FromFile(conf))
            {
                // Write Paths
                string savestatePath = Path.Combine(AppConfig.GetFullPath("saves"), system, "mgba", "sstates");
                if (!Directory.Exists(savestatePath)) try { Directory.CreateDirectory(savestatePath); }
                    catch { }
                if (!string.IsNullOrEmpty(savestatePath) && Directory.Exists(savestatePath))
                    ini.WriteValue("ports.qt", "savestatePath", savestatePath);

                string cheatsPath = Path.Combine(AppConfig.GetFullPath("cheats"), "mgba");
                if (!Directory.Exists(cheatsPath)) try { Directory.CreateDirectory(cheatsPath); }
                    catch { }
                if (!string.IsNullOrEmpty(cheatsPath) && Directory.Exists(cheatsPath))
                    ini.WriteValue("ports.qt", "cheatsPath", cheatsPath);

                string screenshotPath = Path.Combine(AppConfig.GetFullPath("screenshots"), "mgba");
                if (!Directory.Exists(screenshotPath)) try { Directory.CreateDirectory(screenshotPath); }
                    catch { }
                if (!string.IsNullOrEmpty(screenshotPath) && Directory.Exists(screenshotPath))
                    ini.WriteValue("ports.qt", "screenshotPath", screenshotPath);

                string savegamePath = Path.Combine(AppConfig.GetFullPath("saves"), system);
                if (!Directory.Exists(savegamePath)) try { Directory.CreateDirectory(savegamePath); }
                    catch { }
                if (!string.IsNullOrEmpty(savegamePath) && Directory.Exists(savegamePath))
                    ini.WriteValue("ports.qt", "savegamePath", savegamePath);

                // Bios files
                string gbBIOS = Path.Combine(AppConfig.GetFullPath("bios"), "gb_bios.bin");
                if (File.Exists(gbBIOS))
                    ini.WriteValue("ports.qt", "gb.bios", gbBIOS);

                string sgbBIOS = Path.Combine(AppConfig.GetFullPath("bios"), "sgb2_boot.bin");
                if (File.Exists(sgbBIOS))
                    ini.WriteValue("ports.qt", "sgb.bios", sgbBIOS);

                string gbcbBIOS = Path.Combine(AppConfig.GetFullPath("bios"), "gbc_bios.bin");
                if (File.Exists(gbcbBIOS))
                    ini.WriteValue("ports.qt", "gbc.bios", gbcbBIOS);

                string gbabBIOS = Path.Combine(AppConfig.GetFullPath("bios"), "gba_bios.bin");
                if (File.Exists(gbabBIOS))
                    ini.WriteValue("ports.qt", "gba.bios", gbabBIOS);

                // General Settings
                ini.WriteValue("ports.qt", "pauseOnMinimize", "1");
                ini.WriteValue("ports.qt", "pauseOnFocusLost", "1");

                if (SystemConfig.isOptSet("smooth") && SystemConfig.getOptBoolean("smooth"))
                    ini.WriteValue("ports.qt", "resampleVideo", "1");
                else
                    ini.WriteValue("ports.qt", "resampleVideo", "0");

                if (SystemConfig.isOptSet("mgba_fps") && SystemConfig.getOptBoolean("mgba_fps"))
                    ini.WriteValue("ports.qt", "showFps", "1");
                else
                    ini.WriteValue("ports.qt", "showFps", "0");

                if (SystemConfig.isOptSet("discord") && SystemConfig.getOptBoolean("discord"))
                    ini.WriteValue("ports.qt", "useDiscordPresence", "1");
                else
                    ini.WriteValue("ports.qt", "useDiscordPresence", "0");

                if (SystemConfig.isOptSet("mgba_skipbios") && SystemConfig.getOptBoolean("mgba_skipbios"))
                    ini.WriteValue("ports.qt", "skipBios", "1");
                else
                    ini.WriteValue("ports.qt", "skipBios", "0");

                // Drivers
                if (SystemConfig.isOptSet("mgba_renderer") && SystemConfig.getOptBoolean("mgba_renderer"))
                    ini.WriteValue("ports.qt", "hwaccelVideo", "0");
                else
                    ini.WriteValue("ports.qt", "hwaccelVideo", "1");

                // Internal resolution
                if (SystemConfig.isOptSet("internal_resolution") && !string.IsNullOrEmpty(SystemConfig["internal_resolution"]))
                    ini.WriteValue("ports.qt", "videoScale", SystemConfig["internal_resolution"]);
                else
                    ini.WriteValue("ports.qt", "videoScale", "4");

                // Shaders
                if (SystemConfig.isOptSet("mgba_shader") && !string.IsNullOrEmpty(SystemConfig["mgba_shader"]))
                {
                    string shaderpath = Path.Combine(path, "shaders", SystemConfig["mgba_shader"]);
                    if (Directory.Exists(shaderpath))
                        ini.WriteValue("ports.qt", "shader", shaderpath);
                    else
                        ini.WriteValue("ports.qt", "shader", "");
                }
                else
                    ini.WriteValue("ports.qt", "shader", "");

            }

        }
    }
}
