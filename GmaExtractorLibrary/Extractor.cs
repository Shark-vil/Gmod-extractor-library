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

        public static string BinPath = null;
        public static string ContentPath = null;
        public static string ExtractPath = null;
        public static string SevenZipExePath = null;

        private static List<ExtractData> ContentData = new List<ExtractData>();

        public static List<ExtractData> GetContentData()
        {
            return ContentData;
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

                        ContentData.Add(new ExtractData
                        {
                            AddonFileName = Path.GetFileNameWithoutExtension(ContentFiles[0]),
                            AddonDirectoryName = Path.GetFileName(dir),
                            AddonPath = ContentFiles[0],
                            AddonUid = uid,
                            IsBin = isBin
                        });
                    }
                }
            }
        }

        public static ExtractData GetDataByUid(string uid)
        {
            return ContentData.Find(x => x.AddonUid == uid);
        }

        public static ExtractData GetDataByFilename(string filename)
        {
            return ContentData.Find(x => x.AddonFileName == filename);
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

        public static Process UnpackingBin(string binfile_path, string output_path)
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
                    startInfo.Arguments = string.Format("7z x \"{0}\" -o\"{1}\"", binfile_path, output_path);
                else
                    startInfo.Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", binfile_path, output_path);

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

        public static Process ExtractSingle(string uid)
        {
            string gmad_exe_path = GetBinGmodFolder();

            if (gmad_exe_path == null)
                return null;

            ExtractData Addon = GetDataByUid(uid);

            if (Addon == null)
            {
                Console.WriteLine($"Failed to get addon information.");
                return null;
            }

            string FullExtractPath = ExtractPath + "\\" + Addon.AddonFileName + "_" + Addon.AddonDirectoryName;

            WorkshopChecker(ref FullExtractPath, Addon, uid);

            ExtractData NewAddon = ExtractBinAndGetAddon(FullExtractPath, Addon);
            if (NewAddon != null)
                Addon = NewAddon;

            if (!Directory.Exists(FullExtractPath))
            {
                Directory.CreateDirectory(FullExtractPath);
                Console.WriteLine($"Addon directory created: " + FullExtractPath);
            }

            return ExtractGma(gmad_exe_path, FullExtractPath, Addon);
        }

        public static Process ExtractSingleFile(string filepath)
        {
            string gmad_exe_path = GetBinGmodFolder();

            if (gmad_exe_path == null)
                return null;

            Console.WriteLine(Path.GetFileNameWithoutExtension(filepath));

            ExtractData Addon = GetDataByFilename(Path.GetFileNameWithoutExtension(filepath));

            if (Addon == null)
            {
                Console.WriteLine($"Failed to get addon information.");
                return null;
            }

            string FullExtractPath = ExtractPath + "\\" + Addon.AddonFileName + "_" + Addon.AddonDirectoryName;

            WorkshopChecker(ref FullExtractPath, Addon, Addon.AddonUid);

            ExtractData NewAddon = ExtractBinAndGetAddon(FullExtractPath, Addon);
            if (NewAddon != null)
                Addon = NewAddon;

            if (!Directory.Exists(FullExtractPath))
            {
                Directory.CreateDirectory(FullExtractPath);
                Console.WriteLine($"Addon directory created: " + FullExtractPath);
            }

            return ExtractGma(gmad_exe_path, FullExtractPath, Addon);
        }

        private static string GetBinGmodFolder()
        {
            if (BinPath == null)
                return null;

            Console.WriteLine("-------------------------------------------");

            if (ExtractPath == null)
                ExtractPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\Extract";

            if (!Directory.Exists(ExtractPath))
            {
                Directory.CreateDirectory(ExtractPath);
            }

            string gmad_exe_path = BinPath + "\\gmad.exe";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                gmad_exe_path = BinPath + "\\gmad_linux";

            if (Directory.Exists(BinPath))
                if (!File.Exists(gmad_exe_path))
                {
                    Console.WriteLine($"File \"{gmad_exe_path}\" not exists!");
                    return null;
                }

            return gmad_exe_path;
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

                string titleRep = addonData.Title.ToLower().Replace('-', '_');
                titleRep = titleRep.Replace(' ', '_');
                titleRep = titleRep.Replace("'", "_");
                titleRep = titleRep.Replace("&quot;", "_");
                titleRep = titleRep.Replace("&amp;", "_");
                titleRep = titleRep.Replace("/", "_");
                titleRep = titleRep.Replace("\\", "_");
                titleRep = titleRep.Replace(":", "_");
                titleRep = titleRep.Replace("?", "_");
                titleRep = titleRep.Replace("*", "_");
                titleRep = titleRep.Replace("\"", "_");
                titleRep = titleRep.Replace("<", "_");
                titleRep = titleRep.Replace(">", "_");
                titleRep = titleRep.Replace("|", "_");

                fullPath = ExtractPath + "\\" + titleRep + "_" + Addon.AddonDirectoryName;
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

                string TempPath = FullExtractPath + "\\Temp";

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
                    Directory.Delete(FullExtractPath + "\\Temp", true);
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
