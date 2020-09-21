using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace GmaExtractorLibrary
{
    public class Workshop
    {
        public class AddonData
        {
            public string Uid = "";
            public string Title = "None";
            public string Description = "None";
            public string ImageUrl = "None";
            public List<string> Types = new List<string>();
            public List<string> Tags = new List<string>();
            public string FileSize = "None";
            public string UploadDate = "None";
            public string UpdateDate = "None";
            public string UniqueVisitors = "None";
            public string Subscribers = "None";
            public string Favorites = "None";
        }

        public static AddonData GetAddonData(string workshopAddonId, bool cacheIgnore = false)
        {
            string currentDirectoryPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string fileCachePath = Path.Combine(currentDirectoryPath, "cache.json");

            List<AddonData> caches = new List<AddonData>();

            if (!cacheIgnore)
            {
                if (File.Exists(fileCachePath))
                {
                    string fileJson = File.ReadAllText(fileCachePath);
                    caches = JsonConvert.DeserializeObject<List<AddonData>>(fileJson);

                    AddonData getAddonsCache = caches.Find(x => x.Uid == workshopAddonId);
                    if (getAddonsCache != null)
                        return getAddonsCache;
                }
            }

            try
            {
                AddonData addonData = new AddonData();
                HtmlDocument htmlDocument = new HtmlDocument();
                string htmlBody = null;

                using (WebClient client = new WebClient())
                {
                    htmlBody = client.DownloadString("https://steamcommunity.com/sharedfiles/filedetails/?id=" + workshopAddonId.ToString());
                }

                if (htmlBody == null)
                    return new AddonData();

                htmlDocument.LoadHtml(htmlBody);

                HtmlNode docNode = htmlDocument.DocumentNode;

                addonData.Title = docNode.SelectSingleNode("//div[@class='workshopItemTitle']").InnerText;
                addonData.Description = docNode.SelectSingleNode("//div[@class='workshopItemDescription']").InnerText;

                try
                {
                    addonData.ImageUrl = docNode.SelectSingleNode("//img[@id='previewImageMain']").Attributes["src"].Value;
                }
                catch
                {
                    addonData.ImageUrl = docNode.SelectSingleNode("//img[@id='previewImage']").Attributes["src"].Value;
                }

                int workshop_tags_num = 0;
                foreach (HtmlNode workshopTag in docNode.SelectNodes("//div[@class='workshopTags']").Descendants("a"))
                {
                    if (workshop_tags_num <= 1)
                        addonData.Types.Add(workshopTag.InnerText);
                    else
                        addonData.Tags.Add(workshopTag.InnerText);

                    workshop_tags_num++;
                }

                int workshop_details_num = 0;
                foreach (HtmlNode workshopDetails in docNode.SelectNodes("//div[@class='detailsStatRight']"))
                {
                    if (workshop_details_num == 0)
                        addonData.FileSize = workshopDetails.InnerText;

                    if (workshop_details_num == 1)
                        addonData.UploadDate = workshopDetails.InnerText;

                    if (workshop_details_num == 2)
                        addonData.UpdateDate = workshopDetails.InnerText;

                    workshop_details_num++;
                }

                HtmlNodeCollection cells = docNode.SelectNodes("//table[@class='stats_table']/tr/td");
                addonData.UniqueVisitors = cells[0].InnerText;
                addonData.Subscribers = cells[2].InnerText;
                addonData.Favorites = cells[4].InnerText;
                addonData.Uid = workshopAddonId;

                if (caches.Count == 0 && File.Exists(fileCachePath))
                {
                    string fileJson = File.ReadAllText(fileCachePath);
                    caches = JsonConvert.DeserializeObject<List<AddonData>>(fileJson);
                }

                if (cacheIgnore)
                {
                    bool isExists = false;
                    for (int i = 0; i < caches.Count; i++)
                    {
                        if (caches[i].Uid == workshopAddonId)
                        {
                            caches[i] = addonData;
                            isExists = true;
                            break;
                        }
                    }

                    if (!isExists)
                        caches.Add(addonData);

                    File.WriteAllText(fileCachePath, JsonConvert.SerializeObject(caches, Formatting.Indented));
                }
                else
                {
                    caches.Add(addonData);
                    File.WriteAllText(fileCachePath, JsonConvert.SerializeObject(caches, Formatting.Indented));
                }

                return addonData;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Parsing addon webpage error:\n" + ex);
                return new AddonData();
            }
        }
    }
}
