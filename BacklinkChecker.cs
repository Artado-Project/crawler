using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace crawler
{
    internal class BacklinkChecker
    {
        public static List<string> GetLinks(string targetURL)
        {
            List<string> backlinks = new List<string>();
            try
            {
                WebClient client = new WebClient();
                string html = client.DownloadString(targetURL);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Find all anchor tags
                var links = doc.DocumentNode.SelectNodes("//a[@href]");
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        string href = link.GetAttributeValue("href", "");
                        // Check if the link points to the targetURL
                        backlinks.Add(href);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return backlinks;
        }
    }
}
