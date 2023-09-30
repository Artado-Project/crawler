using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using crawler;

public class Robots
{
    public static List<string> GetDisallowedUrls(string websiteUrl)
    {
        List<string> disallowedUrls = new List<string>();
        string robotsUrl = new Uri(new Uri(websiteUrl), "/robots.txt").AbsoluteUri;

        try
        {
            WebClient webClient = new WebClient();
            string robotsTxt = webClient.DownloadString(robotsUrl);

            using (StringReader reader = new StringReader(robotsTxt))
            {
                string line;
                bool userAgentMatches = false;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
                    {
                        userAgentMatches = false;
                        string userAgent = line.Substring("User-agent:".Length).Trim();

                        // You can customize the user agent you want to check here.
                        if (userAgent == "*" || userAgent == "ArtadoBot")
                        {
                            userAgentMatches = true;
                        }
                    }
                    else if (userAgentMatches && line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
                    {
                        string disallowedPath = line.Substring("Disallow:".Length).Trim();
                        if (!string.IsNullOrWhiteSpace(disallowedPath))
                        {
                            disallowedUrls.Add(new Uri(new Uri(websiteUrl), disallowedPath).AbsoluteUri);
                        }
                    }
                }
            }
        }
        catch (WebException ex)
        {
            Console.WriteLine("Error downloading robots.txt: " + ex.Message);
        }

        return disallowedUrls;
    }

    public static bool IsUrlDisallowed(string url, List<string> disallowedUrls)
    {
        // Normalize the URL to ensure it matches the format used in disallowedUrls
        string normalizedUrl = new Uri(url).AbsoluteUri;

        // Check if the normalized URL starts with any of the disallowed URLs
        return disallowedUrls.Any(disallowedUrl => normalizedUrl.StartsWith(disallowedUrl, StringComparison.OrdinalIgnoreCase));
    }
}
