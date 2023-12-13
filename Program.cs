using crawler;
using HtmlAgilityPack;
using Newtonsoft.Json;

string websiteUrl = Console.ReadLine();

string robotsUrl = websiteUrl + "/robots.txt";

List<string> disallowedUrls = Robots.GetDisallowedUrls(robotsUrl);

Console.WriteLine("Disallowed URLs in robots.txt:");
foreach (string url in disallowedUrls)
{
    Console.WriteLine(url);
}


for(int i = 0; i >= 0; i++)
{
    if (!Crawl.IsUrlInVisitList(websiteUrl) && !Crawl.IsUrlInLocal(websiteUrl))
    {
        bool urlcheck = Robots.IsUrlDisallowed(websiteUrl, disallowedUrls);

        if (urlcheck == true)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Disallowed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Allowed");

            Result result = Crawl.result(websiteUrl);
            Console.WriteLine(result.Title);
            Console.WriteLine(result.URL);
            Console.WriteLine(result.Description);
            Console.WriteLine(result.Keywords);

            if (Crawl.GetResultCountFromJson() > 30)
            {
                Crawl.ImportResultsToDB();
            }

            Main.MainFunc(websiteUrl, disallowedUrls);
        }
    }
    else
    {
        Console.WriteLine("Link already saved");

        Main.MainFunc(websiteUrl, disallowedUrls);
    }
}


class Main
{
    public static void MainFunc(string websiteUrl, List<string> disallowedUrls)
    {
        string jsonFilePath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\visitlist.json";

        try
        {
            if (File.Exists(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                List<WebsiteLink> links = JsonConvert.DeserializeObject<List<WebsiteLink>>(jsonContent);

                foreach (var link in links)
                {
                    Console.WriteLine("Getting the links from the list");
                    if (!link.Visited)
                    {
                        int permalink = link.Url.IndexOf("#");

                        bool linkcheck;

                        if (UrlComparer.AreUrlsDifferent(websiteUrl, link.Url))
                        {
                            string robotstxt = link.Url + "/robots.txt";

                            List<string> disallowedlinks = Robots.GetDisallowedUrls(robotstxt);

                            Console.WriteLine("Disallowed URLs in robots.txt:");
                            foreach (string url in disallowedlinks)
                            {
                                Console.WriteLine(url);
                            }

                            linkcheck = Robots.IsUrlDisallowed(link.Url, disallowedlinks);
                        }
                        else
                        {
                            linkcheck = Robots.IsUrlDisallowed(link.Url, disallowedUrls);
                        }

                        if (!linkcheck && !Crawl.IsUrlInLocal(link.Url) && permalink < 0)
                        {
                            Result a_result = Crawl.result(link.Url);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Visiting links from the list");
                            Console.WriteLine(a_result.Title);
                            Console.WriteLine(a_result.URL);
                            Console.WriteLine(a_result.Description);
                            Console.WriteLine(a_result.Keywords);

                            if (Crawl.GetResultCountFromJson() > 30)
                            {
                                Crawl.ImportResultsToDB();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Link already saved.");
                        }

                        // Mark the link as visited
                        link.Visited = true;

                        // Serialize and save the updated links to the JSON file
                        string updatedJsonContent = JsonConvert.SerializeObject(links, Formatting.Indented);
                        File.WriteAllText(jsonFilePath, updatedJsonContent);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error checking and marking visited links: " + ex.Message);
        }
    }
}