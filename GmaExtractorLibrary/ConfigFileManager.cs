using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GmaExtractorLibrary
{
    public class ConfigStructure
    {
        public string Key;
        public string Value;
    }

    public class ConfigFileManager
    {
        public static List<ConfigStructure> ReadConfig(string filePath)
        {
            List<ConfigStructure> config = new List<ConfigStructure>();

            string s;
            using (var f = new StreamReader(filePath))
            {
                while ((s = f.ReadLine()) != null)
                {
                    string[] KeyAndValue = s.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                    if (KeyAndValue != null && KeyAndValue.Length == 2)
                        config.Add(new ConfigStructure
                        {
                            Key = KeyAndValue[0],
                            Value = KeyAndValue[1]
                        });
                }
            }

            return config;
        }

        public static void WriteConfig(string filePath, List<ConfigStructure> config)
        {
            using (StreamWriter file = new StreamWriter(filePath))
            {
                foreach (var line in config)
                {
                    file.WriteLine(line.Key + " " + line.Value);
                }
            }
        }
    }
}
