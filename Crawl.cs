using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace crawler
{
    internal class Crawl
    {
        public static string visitlist = string.Empty;
        public static string resultsjson = string.Empty;
        private static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public static Result result(string url)
        {
            // Get the Results JSON
            string resultspath = Path.Combine(BaseDirectory, "results.json");
            if (File.Exists(resultspath))
                resultsjson = File.ReadAllText(resultspath);

            // Get the Visitlist
            string visitpath = Path.Combine(BaseDirectory, "visitlist.json");
            if (File.Exists(visitpath))
                visitlist = File.ReadAllText(visitpath);

            Result result = new Result();

            try
            {
                // Rank
                double rank = 0;

                WebClient webClient = new WebClient();
                webClient.Headers["UserAgent"] = "ArtadoBot/1.0";
                string htmlContent = webClient.DownloadString(url);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // Extract title
                result.Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
                Console.WriteLine(result.Title);

                // URL
                result.URL = url;
                Console.WriteLine(result.URL);

                // Extract meta description
                HtmlNode? metaDescriptionNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
                if (metaDescriptionNode != null)
                {
                    result.Description = metaDescriptionNode.GetAttributeValue("content", "");
                    rank++;
                    Console.WriteLine(result.Description);
                }
                else
                {
                    HtmlNode h1 = doc.DocumentNode.Descendants("h1").FirstOrDefault();
                    if (h1 != null)
                    {
                        result.Description = h1.InnerText;
                    }
                    else
                    {
                        HtmlNode firstElementInBody = doc.DocumentNode.Descendants("body").FirstOrDefault().Descendants().FirstOrDefault();
                        // Remove extra spaces
                        string trimmed = System.Text.RegularExpressions.Regex.Replace(firstElementInBody.InnerText.Trim(), @"\s+", " ");

                        // Take only the first 100 characters
                        string truncated = trimmed.Length > 100 ? trimmed.Substring(0, 100) : trimmed;
                        result.Description = truncated.Trim();
                    }
                    Console.WriteLine(result.Description);
                }

                // Extract meta keywords
                HtmlNode? metaKeywordsNode = doc.DocumentNode.SelectSingleNode("//meta[@name='keywords']");
                if (metaKeywordsNode != null)
                {
                    result.Keywords = metaKeywordsNode.GetAttributeValue("content", "").Split();
                    rank++;
                    Console.WriteLine(result.Keywords);
                }

                // Extract lang
                HtmlNode? langNode = doc.DocumentNode.SelectSingleNode("//html");
                if (langNode != null)
                {
                    result.Lang = langNode.GetAttributeValue("lang", "");
                    if (result.Lang != null)
                        rank++;
                    Console.WriteLine(result.Lang);
                }

                // Mobile Support
                HtmlNode? mobileNode = doc.DocumentNode.SelectSingleNode("//meta[@name='viewport']");
                if (mobileNode != null)
                {
                    rank++;
                }

                // Homepage detection
                if (IsMainDirectory(url))
                {
                    rank++;
                }

                // Prioritize the wikipedia results
                int wiki = url.IndexOf("wikipedia.org");
                if(wiki >= 0)
                {
                    rank++;
                }

                // Find all <a> tags
                List<WebsiteLink> links = new List<WebsiteLink>();

                Console.WriteLine("Getting the a links");
                try
                {
                    string jsonFilePath = Path.Combine(BaseDirectory, "visitlist.json");

                    // Load existing results from JSON file once
                    if (File.Exists(jsonFilePath))
                    {
                        string jsonContent = File.ReadAllText(jsonFilePath);
                        links = JsonConvert.DeserializeObject<List<WebsiteLink>>(jsonContent);
                    }

                    var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]")?.ToList();
                    if (linkNodes != null)
                    {
                        Parallel.ForEach(linkNodes, linkNode =>
                        {
                            string href = linkNode.GetAttributeValue("href", "");
                            string linkUrl = new Uri(new Uri(url), href).AbsoluteUri;

                            int permalink = linkUrl.IndexOf("#");

                            if (!IsUrlInVisitList(linkUrl, visitlist) && !IsUrlInLocal(linkUrl, resultsjson) && permalink < 0 && IsURL(linkUrl) && url != linkUrl)
                            {
                                // Initialize links as not visited
                                links.Add(new WebsiteLink { Url = linkUrl, Visited = false });
                                Console.WriteLine("Link saved:" + linkUrl);
                            }
                            else
                            {
                                Console.WriteLine(linkUrl);
                                Console.WriteLine("Link already saved");
                            }
                        });
                    }

                    // Save all links back to the JSON file once
                    string newContent = JsonConvert.SerializeObject(links, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(jsonFilePath, newContent);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("No a tags in this site. Error: " + ex);
                }

                // Final rank calculation
                Console.WriteLine("Rank: " + rank);

                result.Rank = rank;

                SaveWebsiteInfoToJson(result);
                
                // Index in ElasticSearch
                IndexWebsiteInElasticSearch(result);
            }
            catch (WebException ex)
            {
                Console.WriteLine("Error downloading website content: " + ex.Message);
            }

            return result;
        }

        private static readonly Regex UrlRegex = new Regex(@"^(https?|ftp|file)://[-a-zA-Z0-9+&@#/%?=~_|!:,.;]*[-a-zA-Z0-9+&@#/%=~_|]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static bool IsURL(string input)
        {
            return UrlRegex.IsMatch(input);
        }

        static bool IsMainDirectory(string url)
        {
            // Parse the URL
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                // Check if the path component is empty or "/"
                return string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/";
            }

            // If the URL is invalid or cannot be parsed, return false
            return false;
        }

        static void SaveWebsiteInfoToJson(Result websiteInfo)
        {
            string jsonFilePath = Path.Combine(BaseDirectory, "results.json");
            List<Result> results = new List<Result>();

            // Load existing results from JSON file
            if (File.Exists(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                results = JsonConvert.DeserializeObject<List<Result>>(jsonContent);
            }

            // Add the new websiteInfo to the results list
            results.Add(websiteInfo);

            // Serialize and save the updated results to the JSON file
            string updatedJsonContent = JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(jsonFilePath, updatedJsonContent);
        }

        public static int GetResultCountFromJson()
        {
            string jsonFilePath = Path.Combine(BaseDirectory, "results.json");

            if (File.Exists(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                List<Result> results = JsonConvert.DeserializeObject<List<Result>>(jsonContent);

                return results.Count;
            }

            return 0;
        }

        // ElasticSearch Methods
        public static async Task ImportResultsToElasticSearch()
        {
            string jsonFilePath = Path.Combine(BaseDirectory, "results.json");

            try
            {
                // Read JSON file
                string jsonContent = File.ReadAllText(jsonFilePath);
                List<Result> results = JsonConvert.DeserializeObject<List<Result>>(jsonContent);

                // Process each result and index it in ElasticSearch
                foreach (Result websiteInfo in results)
                {
                    await IndexWebsiteInElasticSearch(websiteInfo);
                }

                Console.WriteLine("Results imported to ElasticSearch.");

                // Optionally delete the file after successful import
                File.Delete(jsonFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error importing results to ElasticSearch: " + ex.Message);
            }
        }

        private static async Task IndexWebsiteInElasticSearch(Result result)
        {
            try
            {
                // Convert the result to JSON
                string jsonData = JsonConvert.SerializeObject(result);
                
                // ElasticSearch endpoint - should be configurable
                string elasticSearchUrl = Config.ElasticSearchUrl;
                string indexUrl = $"{elasticSearchUrl}/artadosearch/_doc";
                
                // Check if document exists to determine update or insert
                bool exists = await CheckIfDocumentExists(elasticSearchUrl, result.URL);
                
                // Use appropriate URL for update or insert
                string finalUrl = exists 
                    ? $"{indexUrl}/{WebUtility.UrlEncode(result.URL)}/_update" 
                    : $"{indexUrl}/{WebUtility.UrlEncode(result.URL)}";

                // Format data for update if needed
                string requestData = exists
                    ? $"{{\"doc\":{jsonData}}}"
                    : jsonData;

                // Use WebClient to send data to ElasticSearch
                using (WebClient client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    
                    if (exists)
                    {
                        // For update
                        string response = client.UploadString(finalUrl, "POST", requestData);
                        Console.WriteLine($"Updated document in ElasticSearch: {result.URL}");
                    }
                    else
                    {
                        // For insert
                        string response = client.UploadString(finalUrl, "PUT", requestData);
                        Console.WriteLine($"Indexed new document in ElasticSearch: {result.URL}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error indexing document in ElasticSearch: {ex.Message}");
            }
        }

        private static async Task<bool> CheckIfDocumentExists(string elasticSearchBaseUrl, string url)
        {
            try
            {
                string encodedUrl = WebUtility.UrlEncode(url);
                string requestUrl = $"{elasticSearchBaseUrl}/artadosearch/_doc/{encodedUrl}";
                
                WebClient client = new WebClient();
                try
                {
                    string response = client.DownloadString(requestUrl);
                    // Parse the response to check if document exists
                    dynamic jsonResponse = JsonConvert.DeserializeObject(response);
                    return jsonResponse.found == true;
                }
                catch (WebException ex)
                {
                    // 404 indicates document doesn't exist
                    if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                    {
                        return false;
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking document existence: {ex.Message}");
                return false;
            }
        }

        public static bool IsUrlInElasticSearch(string url)
        {
            try
            {
                string encodedUrl = WebUtility.UrlEncode(url);
                string elasticSearchUrl = Config.ElasticSearchUrl;
                string requestUrl = $"{elasticSearchUrl}/artadosearch/_doc/{encodedUrl}";
                
                WebClient client = new WebClient();
                try
                {
                    string response = client.DownloadString(requestUrl);
                    dynamic jsonResponse = JsonConvert.DeserializeObject(response);
                    return jsonResponse.found == true;
                }
                catch (WebException ex)
                {
                    if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                    {
                        return false;
                    }
                    throw;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsUrlInVisitList(string url, string jsonContent)
        {
            if (!string.IsNullOrEmpty(jsonContent))
            {
                List<WebsiteLink> links = JsonConvert.DeserializeObject<List<WebsiteLink>>(jsonContent);
                return links != null && links.Exists(link => link.Url == url);
            }
            return false;
        }

        public static bool IsUrlInLocal(string url, string jsonContent)
        {
            if (!string.IsNullOrEmpty(jsonContent))
            {
                List<Result> results = JsonConvert.DeserializeObject<List<Result>>(jsonContent);
                return results != null && results.Exists(result => result.URL == url);
            }
            return false;
        }
    }
}