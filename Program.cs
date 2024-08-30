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

string visitlist = string.Empty;
string resultsjson = string.Empty;

for (int i = 0; i >= 0; i++)
{
    //Get the Results JSON
    string resultspath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\results.json";
    if (File.Exists(resultspath))
        resultsjson = File.ReadAllText(resultspath);

    //Get the Visitlist
    string visitpath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\visitlist.json";
    if (File.Exists(visitpath))
        visitlist = File.ReadAllText(visitpath);
    if (!Crawl.IsUrlInLocal(websiteUrl, resultsjson))
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

            Main.MainFunc(websiteUrl, disallowedUrls, visitlist);
        }
    }
    else
    {
        Console.WriteLine("Link already saved");

        Main.MainFunc(websiteUrl, disallowedUrls, visitlist);
    }
}

class Main
{
    public async static Task MainFunc(string websiteUrl, List<string> disallowedUrls, string jsonContent)
    {
        List<WebsiteLink> links = new List<WebsiteLink>();

        if (jsonContent != string.Empty)
        {
            links = JsonConvert.DeserializeObject<List<WebsiteLink>>(jsonContent);
        }

        var tasks = links.Where(link => !link.Visited).Select(async link =>
        {
            Console.WriteLine("Getting the links from the list");
            int permalink = link.Url.IndexOf("#");

            bool linkcheck = UrlComparer.AreUrlsDifferent(websiteUrl, link.Url)
                ? Robots.IsUrlDisallowed(link.Url, Robots.GetDisallowedUrls(link.Url + "/robots.txt"))
                : Robots.IsUrlDisallowed(link.Url, disallowedUrls);

            if (!linkcheck && !Crawl.IsUrlInLocal(link.Url, jsonContent) && permalink < 0)
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
        }).ToList();

        await Task.WhenAll(tasks);

        // Serialize and save the updated links to the JSON file
        string jsonFilePath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\visitlist.json";
        string updatedJsonContent = JsonConvert.SerializeObject(links, Formatting.Indented);
        File.WriteAllText(jsonFilePath, updatedJsonContent);
    }

}