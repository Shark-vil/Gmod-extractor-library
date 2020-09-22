using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace GmaExtractorLibrary
{
    public class Extractor
    {
        public class ExtractData
        {
            public string AddonUid;
            public string AddonFileName;
            public string AddonDirectoryName;
            public string AddonPath;
            public bool IsBin = false;
        }

        public static string GameFolderPath = null;
        public static string ContentPath = null;
        public static string ExtractPath = null;
        public static string SevenZipExePath = null;

        private static List<ExtractData> ContentData = new List<ExtractData>();
        private static List<ExtractData> AllContentData = new List<ExtractData>();

        public static List<ExtractData> GetContentData()
        {
            return ContentData;
        }

        public static void InitConfig()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string configDirectoryPath = Path.Combine(baseDirectory, "Config");
            string extractDirectoryPath = Path.Combine(baseDirectory, "Extract");

            if (!Directory.Exists(configDirectoryPath))
                Directory.CreateDirectory(configDirectoryPath);

            if (!Directory.Exists(extractDirectoryPath))
                Directory.CreateDirectory(extractDirectoryPath);

            string fileSettingsPath = Path.Combine(configDirectoryPath, "settings.cfg");

            string GameDirectoryPath = @"C:\SteamLibrary\steamapps\common\GarrysMod";
            string GameWorkshopDirectoryPath = @"C:\SteamLibrary\steamapps\workshop\content\4000";
            string GmaExtractPath = Path.Combine(baseDirectory, "Extract");
            string SevenZipPath = Path.Combine(baseDirectory, @"Library\7-Zip-Portable\App\7-Zip64\7z.exe");

            if (!File.Exists(fileSettingsPath))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    DriveInfo[] allDrives = DriveInfo.GetDrives();
                    foreach (DriveInfo d in allDrives)
                    {
                        if (d.IsReady == true)
                        {
                            if (!Directory.Exists(GameDirectoryPath))
                            {
                                string path = d.Name + @"SteamLibrary\steamapps\common\GarrysMod";
                                if (Directory.Exists(path))
                                {
                                    GameDirectoryPath = path;
                                    Console.WriteLine("Found the directory of the game - " + path);
                                }
                            }

                            if (!Directory.Exists(GameWorkshopDirectoryPath))
                            {
                                string path = d.Name + @"SteamLibrary\steamapps\workshop\content\4000";
                                if (Directory.Exists(path))
                                {
                                    GameWorkshopDirectoryPath = path;
                                    Console.WriteLine("Found the directory of the workshop game - " + path);
                                }
                            }
                        }
                    }
                }

                ConfigFileManager.WriteConfig(fileSettingsPath, new List<ConfigStructure>
                {
                    new ConfigStructure
                    {
                        Key = "GameFolderPath",
                        Value = GameDirectoryPath
                    },
                    new ConfigStructure
                    {
                        Key = "ContentPath",
                        Value = GameWorkshopDirectoryPath
                    },
                    new ConfigStructure
                    {
                        Key = "ExtractPath",
                        Value = GmaExtractPath
                    },
                    new ConfigStructure
                    {
                        Key = "SevenZipExePath",
                        Value = SevenZipPath
                    },
                });
            }

            List<ConfigStructure> Config = ConfigFileManager.ReadConfig(fileSettingsPath);

            GameFolderPath = Config.Find(x => x.Key == "GameFolderPath").Value;
            ContentPath = Config.Find(x => x.Key == "ContentPath").Value;
            ExtractPath = Config.Find(x => x.Key == "ExtractPath").Value;
            SevenZipExePath = Config.Find(x => x.Key == "SevenZipExePath").Value;

            if (!Directory.Exists(GameFolderPath))
                Console.WriteLine($"WARNING!\nInvalid directory path - {GameFolderPath}\nPlease edit the path in the config file - {fileSettingsPath}\n");

            if (!Directory.Exists(ContentPath))
                Console.WriteLine($"WARNING!\nInvalid directory path - {ContentPath}\nPlease edit the path in the config file - {fileSettingsPath}\n");

            if (!Directory.Exists(ExtractPath))
                Console.WriteLine($"WARNING!\nInvalid directory path - {ExtractPath}\nPlease edit the path in the config file - {fileSettingsPath}\n");

            if (!File.Exists(SevenZipExePath))
                Console.WriteLine($"WARNING!\nInvalid file path - {SevenZipExePath}\nPlease edit the path in the config file - {fileSettingsPath}\n");
        }

        public static void ParseDirectory()
        {
            if (ContentPath != null && Directory.Exists(ContentPath))
            {
                ContentData.Clear();

                string[] ContentDirs = Directory.GetDirectories(ContentPath);
                foreach (string dir in ContentDirs)
                {
                    string[] ContentFiles = Directory.GetFiles(dir);
                    string uid = Path.GetFileName(dir);

                    if (ContentFiles.Length != 0)
                    {
                        bool isBin = false;

                        if (Path.GetExtension(ContentFiles[0]) == ".bin")
                            isBin = true;

                        if (!ContentData.Exists(x => x.AddonUid == uid))
                        {
                            var data = new ExtractData
                            {
                                AddonFileName = Path.GetFileNameWithoutExtension(ContentFiles[0]),
                                AddonDirectoryName = Path.GetFileName(dir),
                                AddonPath = ContentFiles[0],
                                AddonUid = uid,
                                IsBin = isBin
                            };

                            ContentData.Add(data);
                            AllContentData.Add(data);
                        }
                    }
                }
            }

            string AddonsFolder = Path.Combine(GameFolderPath, "garrysmod", "addons");

            if (AddonsFolder != null && Directory.Exists(AddonsFolder))
            {
                string[] ContentFiles = Directory.GetFiles(AddonsFolder);

                if (ContentFiles.Length != 0)
                {
                    foreach (var ContentFile in ContentFiles)
                    {
                        string[] SplitStr = Path.GetFileName(ContentFile).Split('_');
                        string NormalizeUid = SplitStr[SplitStr.Length - 1].Replace(".gma", string.Empty);

                        var data = new ExtractData
                        {
                            AddonFileName = Path.GetFileNameWithoutExtension(ContentFile),
                            AddonDirectoryName = Path.GetFileName(AddonsFolder),
                            AddonPath = ContentFile,
                            AddonUid = NormalizeUid,
                            IsBin = false
                        };

                        AllContentData.Add(data);

                        if (!ContentData.Exists(x => x.AddonUid == NormalizeUid))
                            ContentData.Add(data);
                    }
                }
            }
        }

        public static ExtractData GetDataByUid(string uid)
        {
            ExtractData data = ContentData.Find(x => x.AddonUid == uid);
            return (data != null) ? data : AllContentData.Find(x => x.AddonUid == uid);
        }

        public static ExtractData GetDataByFilename(string filename)
        {
            ExtractData data = ContentData.Find(x => x.AddonFileName == filename);
            return (data != null) ? data : AllContentData.Find(x => x.AddonFileName == filename);
        }

        public static ExtractData GetDataByPath(string filepath)
        {
            ExtractData data = ContentData.Find(x => x.AddonPath == filepath);
            return (data != null) ? data : AllContentData.Find(x => x.AddonPath == filepath);
        }

        public static void ExtractAll()
        {
            foreach (var addon in ContentData)
            {
                ExtractSingle(addon.AddonUid);
            }
        }

        public static string GetMD5FolderHash(string path)
        {
            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();

            MD5 md5 = MD5.Create();

            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];

                // hash path
                string relativePath = file.Substring(path.Length + 1);
                byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                byte[] contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }

        public static string GetMD5FileHash(string path)
        {
            MD5 md5 = MD5.Create();

            string file = Directory.GetFiles(path)[0];

            // hash path
            string relativePath = file.Substring(path.Length + 1);
            byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
            md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

            // hash contents
            byte[] contentBytes = File.ReadAllBytes(file);
            md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }

        public static Process UnpackingBin(string binFilePath, string outputPath)
        {
            try
            {
                Process gmadProcess = new Process();
                ProcessStartInfo startInfo = null;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    startInfo = new ProcessStartInfo("/bin/bash");
                else
                    startInfo = new ProcessStartInfo(SevenZipExePath);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    startInfo.Arguments = string.Format("7z x \"{0}\" -o\"{1}\"", binFilePath, outputPath);
                else
                    startInfo.Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", binFilePath, outputPath);

                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = false;
                startInfo.CreateNoWindow = true;

                gmadProcess.StartInfo = startInfo;
                gmadProcess.Start();
                gmadProcess.WaitForExit();
                gmadProcess.Close();

                return gmadProcess;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;
        }

        public static Process ExtractSingle(string filename)
        {
            string gmadExePath = GetBinGmodFolder();

            if (gmadExePath == null)
                return null;

            ExtractData Addon = GetDataByUid(filename);

            if (Addon == null)
                Addon = GetDataByFilename(filename);

            if (Addon == null)
                Addon = GetDataByPath(filename);

            if (Addon == null)
            {
                Console.WriteLine($"Failed to get addon information.");
                return null;
            }

            string FullExtractPath = Path.Combine(ExtractPath, Addon.AddonFileName + "_" + Addon.AddonDirectoryName);

            WorkshopChecker(ref FullExtractPath, Addon, filename);

            ExtractData NewAddon = ExtractBinAndGetAddon(FullExtractPath, Addon);
            if (NewAddon != null)
                Addon = NewAddon;

            if (!Directory.Exists(FullExtractPath))
            {
                Directory.CreateDirectory(FullExtractPath);
                Console.WriteLine($"Addon directory created: " + FullExtractPath);
            }

            return ExtractGma(gmadExePath, FullExtractPath, Addon);
        }

        public static Process ExtractSingleFile(string filepath)
        {
            string gmadExePath = GetBinGmodFolder();

            if (gmadExePath == null)
                return null;

            string fileName = Path.GetFileNameWithoutExtension(filepath);

            ExtractData Addon = GetDataByUid(fileName);

            /*
            foreach (var contentValue in ContentData)
                Console.WriteLine($"{fileName} ({fileName.Length}) - {contentValue.AddonFileName} ({contentValue.AddonFileName.Length})");
            */

            if (Addon == null)
                Addon = GetDataByFilename(fileName);

            if (Addon == null)
                Addon = GetDataByPath(fileName);

            if (Addon == null)
            {
                Console.WriteLine($"Failed to get addon information.");
                return null;
            }

            string FullExtractPath = Path.Combine(ExtractPath, Addon.AddonFileName + "_" + Addon.AddonDirectoryName);

            WorkshopChecker(ref FullExtractPath, Addon, Addon.AddonUid);

            ExtractData NewAddon = ExtractBinAndGetAddon(FullExtractPath, Addon);
            if (NewAddon != null)
                Addon = NewAddon;

            if (!Directory.Exists(FullExtractPath))
            {
                Directory.CreateDirectory(FullExtractPath);
                Console.WriteLine($"Addon directory created: " + FullExtractPath);
            }

            return ExtractGma(gmadExePath, FullExtractPath, Addon);
        }

        private static string GetBinGmodFolder()
        {
            string BinPath = Path.Combine(GameFolderPath, "bin");

            Console.WriteLine("-------------------------------------------");

            if (ExtractPath == null)
                ExtractPath = Path.Combine(Directory.GetCurrentDirectory(), "Extract");
                //ExtractPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\Extract";

            if (!Directory.Exists(ExtractPath))
                Directory.CreateDirectory(ExtractPath);

            string gmadExePath = Path.Combine(BinPath, "gmad.exe");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                gmadExePath = Path.Combine(BinPath, "gmad_linux");

            if (Directory.Exists(BinPath))
                if (!File.Exists(gmadExePath))
                {
                    Console.WriteLine($"File \"{gmadExePath}\" not exists!");
                    return null;
                }

            return gmadExePath;
        }

        private static void WorkshopChecker(ref string fullPath, ExtractData Addon, string uid = "")
        {
            Workshop.AddonData addonData = Workshop.GetAddonData(uid);
            if (addonData.Title != "None")
            {
                Console.WriteLine("***********************************************");
                Console.WriteLine($"Addon Information:\n" +
                    $"Title - {addonData.Title}\n" +
                    $"Types - {string.Join(", ", addonData.Types.ToArray())}\n" +
                    $"Tags - {string.Join(", ", addonData.Tags.ToArray())}\n" +
                    $"Upload date - {addonData.UploadDate}\n" +
                    $"Last update - {addonData.UpdateDate}");
                Console.WriteLine("***********************************************");

                string addonName = addonData.Title.ToLower().Replace('-', '_');
                addonName = addonName.Replace(' ', '_');
                addonName = addonName.Replace("'", "_");
                addonName = addonName.Replace("&quot;", "_");
                addonName = addonName.Replace("&amp;", "_");
                addonName = addonName.Replace("/", "_");
                addonName = addonName.Replace("\\", "_");
                addonName = addonName.Replace(":", "_");
                addonName = addonName.Replace("?", "_");
                addonName = addonName.Replace("*", "_");
                addonName = addonName.Replace("\"", "_");
                addonName = addonName.Replace("<", "_");
                addonName = addonName.Replace(">", "_");
                addonName = addonName.Replace("|", "_");

                fullPath = Path.Combine(ExtractPath, addonName + "_" + Addon.AddonDirectoryName);
            }
        }

        private static ExtractData ExtractBinAndGetAddon(string FullExtractPath, ExtractData Addon)
        {
            if (Addon.IsBin)
            {
                Console.WriteLine($"The file extension is \".bin\".");

                if (!Directory.Exists(FullExtractPath))
                {
                    Directory.CreateDirectory(FullExtractPath);
                    Console.WriteLine($"Addon directory created: " + FullExtractPath);
                }

                string TempPath = Path.Combine(FullExtractPath, "temp");

                if (!Directory.Exists(TempPath))
                {
                    DirectoryInfo di = Directory.CreateDirectory(TempPath);
                    di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                }

                Console.WriteLine("Begin the process of unpacking.");
                UnpackingBin(Addon.AddonPath, TempPath);
                Console.WriteLine("Unpacking archive completed.");

                string[] files = Directory.GetFiles(TempPath);

                if (files.Length == 0)
                    return null;

                Addon.AddonPath = files[0] + ".gma";
                File.Move(files[0], Addon.AddonPath);

                return Addon;
            }

            return null;
        }

        private static Process ExtractGma(string gmad_exe_path, string FullExtractPath, ExtractData Addon)
        {
            try
            {
                Process gmadProcess = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo(gmad_exe_path, $"\"{Addon.AddonPath}\" -out \"{FullExtractPath}\"");

                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = false;
                startInfo.CreateNoWindow = true;

                gmadProcess.StartInfo = startInfo;
                Console.WriteLine("Begin the process of GMA unpacking.");
                gmadProcess.Start();
                gmadProcess.WaitForExit();
                gmadProcess.Close();
                Console.WriteLine("Unpacking GMA completed.");

                if (Addon.IsBin)
                {
                    Directory.Delete(Path.Combine(FullExtractPath, "temp"), true);
                }

                return gmadProcess;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return null;
        }
    }
}
