using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace crawler
{
    internal class Crawl
    {
        public static Result result(string url)
        {
            Result result = new Result();

            try
            {
                //Rank
                int rank = 0;

                WebClient webClient = new WebClient();
                string htmlContent = webClient.DownloadString(url);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // Extract title
                result.Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
                Console.WriteLine(result.Title);

                //URL
                result.URL = url;
                Console.WriteLine(result.URL);

                // Extract meta description
                HtmlNode? metaDescriptionNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
                if (metaDescriptionNode != null)
                {
                    result.Description = metaDescriptionNode.GetAttributeValue("content", "");
                    if(result.Description == null && result.Description == string.Empty)
                    {
                        HtmlNode h1 = doc.DocumentNode.Descendants("h1").FirstOrDefault();
                        if(h1 != null)
                        {
                            result.Description = h1.InnerText;
                        }
                        else
                        {
                            HtmlNode firstElementInBody = doc.DocumentNode.Descendants("body").FirstOrDefault()?.Descendants().FirstOrDefault();
                            result.Description = firstElementInBody.InnerText;
                        }
                    }
                    rank++;
                    Console.WriteLine(result.Description);
                }

                // Extract meta keywords
                HtmlNode? metaKeywordsNode = doc.DocumentNode.SelectSingleNode("//meta[@name='keywords']");
                if (metaKeywordsNode != null)
                {
                    result.Keywords = metaKeywordsNode.GetAttributeValue("content", "");
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

                //Mobile Support
                HtmlNode? mobileNode = doc.DocumentNode.SelectSingleNode("//meta[@name='viewport']");
                if (mobileNode != null)
                {
                    rank++;
                }

                //Homepage detection
                if (url.EndsWith("/"))
                {
                    rank += 3;
                }

                //Priotize the wikipedia results
                int wiki = url.IndexOf("wikipedia.org");
                if(wiki >= 0)
                {
                    rank += 5;
                }

                //Priotize the reddit results
                int reddit = url.IndexOf("reddit.com");
                if (reddit >= 0)
                {
                    rank += 4;
                }

                //Priotize the artado results
                int artado = url.IndexOf("artadosearch.com");
                int artadoxyz = url.IndexOf("artado.xyz");
                if (artado >= 0 || artadoxyz >= 0)
                {
                    rank += 2;
                }

                result.Rank = rank;

                // Find all <a> tags
                List<WebsiteLink> links = new List<WebsiteLink>();

                try
                {
                    foreach (HtmlNode? linkNode in doc.DocumentNode.SelectNodes("//a[@href]"))
                    {
                        string href = linkNode.GetAttributeValue("href", "");
                        string linkUrl = new Uri(new Uri(url), href).AbsoluteUri;

                        Console.WriteLine(linkUrl);

                        string jsonFilePath = "C:\\Users\\arda.DESKTOP-73M5J5F\\source\\repos\\crawler\\crawler\\visitlist.json";
                        string jsonContent;

                        // Load existing results from JSON file
                        if (File.Exists(jsonFilePath))
                        {
                            jsonContent = File.ReadAllText(jsonFilePath);
                            links = JsonConvert.DeserializeObject<List<WebsiteLink>>(jsonContent);
                        }

                        int permalink = linkUrl.IndexOf("#");

                        if (!IsUrlInVisitList(linkUrl) && !IsUrlInLocal(linkUrl) && permalink < 0)
                        {
                            // Initialize links as not visited
                            links.Add(new WebsiteLink { Url = linkUrl, Visited = false });

                            // Save the links to the JSON file
                            string newContent = JsonConvert.SerializeObject(links, Newtonsoft.Json.Formatting.Indented);
                            File.WriteAllText(jsonFilePath, newContent);

                            Console.WriteLine("Link saved");
                        }
                        else
                        {
                            Console.WriteLine(linkUrl);
                            Console.WriteLine("Link already saved");
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("No a tags in this site");
                }

                SaveWebsiteInfoToJson(result);
            }
            catch (WebException ex)
            {
                Console.WriteLine("Error downloading website content: " + ex.Message);
            }

            return result;
        }

        static void SaveWebsiteInfoToJson(Result websiteInfo)
        {
            string jsonFilePath = "C:\\Users\\arda.DESKTOP-73M5J5F\\source\\repos\\crawler\\crawler\\results.json";
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
            string jsonFilePath = "C:\\Users\\arda.DESKTOP-73M5J5F\\source\\repos\\crawler\\crawler\\results.json";

            if (File.Exists(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                List<Result> results = JsonConvert.DeserializeObject<List<Result>>(jsonContent);

                return results.Count;
            }

            return 0;
        }

        public static void ImportResultsToDB()
        {
            string connectionString = Config.conString;
            string jsonFilePath = "C:\\Users\\arda.DESKTOP-73M5J5F\\source\\repos\\crawler\\crawler\\results.json";

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                // Read JSON file and insert data into the table
                string jsonContent = File.ReadAllText(jsonFilePath);
                List<Result> results = JsonConvert.DeserializeObject<List<Result>>(jsonContent);

                foreach (Result websiteInfo in results)
                {
                    if (IsUrlInDB(websiteInfo.URL))
                    {
                        string updateQuery = "UPDATE WebResults SET " +
                                                                    "Title = @Title, " +
                                                                    "Description = @Description, " +
                                                                    "Keywords = @Keywords, " +
                                                                    "Rank = @Rank, " +
                                                                    "Lang = @Lang " +
                                                                    "WHERE URL = @URL";

                        using SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                        if (websiteInfo.Title != null)
                            updateCommand.Parameters.AddWithValue("@Title", websiteInfo.Title);
                        else
                            updateCommand.Parameters.AddWithValue("@Title", DBNull.Value);

                        if (websiteInfo.Description != null)
                            updateCommand.Parameters.AddWithValue("@Description", websiteInfo.Description);
                        else
                            updateCommand.Parameters.AddWithValue("@Description", DBNull.Value);

                        if (websiteInfo.Keywords != null)
                            updateCommand.Parameters.AddWithValue("@Keywords", websiteInfo.Keywords);
                        else
                            updateCommand.Parameters.AddWithValue("@Keywords", DBNull.Value);

                        updateCommand.Parameters.AddWithValue("@Rank", websiteInfo.Rank);

                        if (websiteInfo.Lang != null)
                            updateCommand.Parameters.AddWithValue("@Lang", websiteInfo.Lang);
                        else
                            updateCommand.Parameters.AddWithValue("@Lang", DBNull.Value);

                        updateCommand.Parameters.AddWithValue("@URL", websiteInfo.URL);

                        updateCommand.ExecuteNonQuery();
                    }
                    else
                    {
                        string insertQuery = "INSERT INTO WebResults (Title, URL, Description, Keywords, Rank, Lang) VALUES (@Title, @URL, @Description, @Keywords, @Rank, @Lang)";
                        using SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                        if (websiteInfo.Title != null)
                            insertCommand.Parameters.AddWithValue("@Title", websiteInfo.Title);
                        else
                            insertCommand.Parameters.AddWithValue("@Title", DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@URL", websiteInfo.URL);
                        if (websiteInfo.Description != null)
                            insertCommand.Parameters.AddWithValue("@Description", websiteInfo.Description);
                        else
                            insertCommand.Parameters.AddWithValue("@Description", DBNull.Value);
                        if (websiteInfo.Keywords != null)
                            insertCommand.Parameters.AddWithValue("@Keywords", websiteInfo.Keywords);
                        else
                            insertCommand.Parameters.AddWithValue("@Keywords", DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@Rank", websiteInfo.Rank);
                        if (websiteInfo.Lang != null)
                            insertCommand.Parameters.AddWithValue("@Lang", websiteInfo.Lang);
                        else
                            insertCommand.Parameters.AddWithValue("@Lang", DBNull.Value);
                        insertCommand.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("Results imported to MSSQL Database.");

                File.Delete(jsonFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error importing results to MSSQL: " + ex.Message);
            }
        }

        public static bool IsUrlInDB(string url)
        {
            string connectionString = Config.conString;
            string query = "SELECT COUNT(*) FROM WebResults WHERE URL = @Url";

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Url", url);

            int count = (int)command.ExecuteScalar();
            return count > 0;
        }

        public static bool IsUrlInVisitList(string url)
        {
            string jsonFilePath = "C:\\Users\\arda.DESKTOP-73M5J5F\\source\\repos\\crawler\\crawler\\visitlist.json";

            if (File.Exists(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                List<WebsiteLink> links = JsonConvert.DeserializeObject<List<WebsiteLink>>(jsonContent);

                return links.Exists(link => link.Url == url);
            }
            else
            {
                return false;
            }
        }

        public static bool IsUrlInLocal(string url)
        {
            string jsonFilePath = "C:\\Users\\arda.DESKTOP-73M5J5F\\source\\repos\\crawler\\crawler\\results.json";

            if (File.Exists(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                List<Result> links = JsonConvert.DeserializeObject<List<Result>>(jsonContent);

                return links.Exists(link => link.URL == url);
            }
            else
            {
                return false;
            }
        }
    }
}
