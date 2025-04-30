using crawler;
using HtmlAgilityPack;
using Newtonsoft.Json;

Console.WriteLine("Enter website URL:");
string websiteUrl = Console.ReadLine();
string robotsUrl = websiteUrl + "/robots.txt";

// Get base directory for file operations
string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

List<string> disallowedUrls = Robots.GetDisallowedUrls(robotsUrl);
Console.WriteLine("Disallowed URLs in robots.txt:");
foreach (string url in disallowedUrls)
{
    Console.WriteLine(url);
}

string visitlist = string.Empty;
string resultsjson = string.Empty;

for (int i = 0; i >= 0; i++)
{
    // Get the Results JSON
    string resultspath = Path.Combine(baseDirectory, "results.json");
    if (File.Exists(resultspath))
        resultsjson = File.ReadAllText(resultspath);

    // Get the Visitlist
    string visitpath = Path.Combine(baseDirectory, "visitlist.json");
    if (File.Exists(visitpath))
        visitlist = File.ReadAllText(visitpath);

    if (!Crawl.IsUrlInLocal(websiteUrl, resultsjson) && !Crawl.IsUrlInElasticSearch(websiteUrl))
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

            // Import to ElasticSearch if we have enough results
            if (Crawl.GetResultCountFromJson() > 30)
            {
                await Crawl.ImportResultsToElasticSearch();
            }
            
            CrawlerMain.MainFunc(websiteUrl, disallowedUrls, visitlist);
        }
    }
    else
    {
        Console.WriteLine("Link already savet");
        CrawlerMain.MainFunc(websiteUrl, disallowedUrls, visitlist);
    }
}

class CrawlerMain
{
    public static void MainFunc(string websiteUrl, List<string> disallowedUrls, string jsonContent)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        List<WebsiteLink> links = new List<WebsiteLink>();
        
        if (!string.IsNullOrEmpty(jsonContent))
        {
            links = JsonConvert.DeserializeObject<List<WebsiteLink>>(jsonContent);
        }

        // Process links synchronously instead of using tasks
        foreach (var link in links.Where(link => !link.Visited).ToList())
        {
            Console.WriteLine("Getting the links from the list");
            int permalink = link.Url.IndexOf("#");
            bool linkcheck = UrlComparer.AreUrlsDifferent(websiteUrl, link.Url)
                ? Robots.IsUrlDisallowed(link.Url, Robots.GetDisallowedUrls(link.Url + "/robots.txt"))
                : Robots.IsUrlDisallowed(link.Url, disallowedUrls);

            if (!linkcheck && !Crawl.IsUrlInLocal(link.Url, jsonContent) && 
                !Crawl.IsUrlInElasticSearch(link.Url) && permalink < 0)
            {
                Result a_result = Crawl.result(link.Url);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Visiting links from the list");
                Console.WriteLine(a_result.Title);
                Console.WriteLine(a_result.URL);
                Console.WriteLine(a_result.Description);
                Console.WriteLine(a_result.Keywords);

                // Import to ElasticSearch if we have enough results
                if (Crawl.GetResultCountFromJson() > 30)
                {
                    Crawl.ImportResultsToElasticSearch().Wait(); // Using .Wait() to synchronously wait for task completion
                }
            }
            else
            {
                Console.WriteLine("Link already saved or disallowed.");
            }

            // Mark the link as visited
            link.Visited = true;
        }

        // Serialize and save the updated links to the JSON file
        string jsonFilePath = Path.Combine(baseDirectory, "visitlist.json");
        string updatedJsonContent = JsonConvert.SerializeObject(links, Formatting.Indented);
        File.WriteAllText(jsonFilePath, updatedJsonContent);
    }
}