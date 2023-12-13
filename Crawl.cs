using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
                double rank = 0;

                WebClient webClient = new WebClient();
                webClient.Headers["UserAgent"] = "ArtadoBot/1.0";
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
                if (IsMainDirectory(url))
                {
                    rank ++;
                }

                //Priotize the wikipedia results
                int wiki = url.IndexOf("wikipedia.org");
                if(wiki >= 0)
                {
                    rank ++;
                }

                // Find all <a> tags
                List<WebsiteLink> links = new List<WebsiteLink>();
                //For the backlinks
                List<Backlinks> backlinks = new List<Backlinks>();

                Console.WriteLine("Getting the a links");
                try
                {
                    foreach (HtmlNode? linkNode in doc.DocumentNode.SelectNodes("//a[@href]"))
                    {
                        string href = linkNode.GetAttributeValue("href", "");
                        string linkUrl = new Uri(new Uri(url), href).AbsoluteUri;

                        Console.WriteLine(linkUrl);

                        string jsonFilePath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\visitlist.json";
                        string jsonContent;

                        // Load existing results from JSON file
                        if (File.Exists(jsonFilePath))
                        {
                            jsonContent = File.ReadAllText(jsonFilePath);
                            links = JsonConvert.DeserializeObject<List<WebsiteLink>>(jsonContent);
                        }

                        int permalink = linkUrl.IndexOf("#");

                        if (!IsUrlInVisitList(linkUrl) && !IsUrlInLocal(linkUrl) && permalink < 0 && IsURL(linkUrl) && url != linkUrl)
                        {
                            // Initialize links as not visited
                            links.Add(new WebsiteLink { Url = linkUrl, Visited = false });

                            //Add href links to backlinks
                            if(!GetBacklinks(url).Contains(linkUrl))
                            {
                                backlinks.Add(new Backlinks { Source = url, Target = linkUrl });
                            }

                            // Save the links to the JSON file
                            string newContent = JsonConvert.SerializeObject(links, Newtonsoft.Json.Formatting.Indented);
                            File.WriteAllText(jsonFilePath, newContent);

                            Console.WriteLine("Link saved:"  + linkUrl);
                        }
                        else
                        {
                            Console.WriteLine(linkUrl);
                            Console.WriteLine("Link already saved");
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("No a tags in this site. Error: " + ex);
                }

                //Import Backlinks
                ImportBacklinksToDB(backlinks);
                backlinks.Clear();

                //PageRank
                double pagerank = PageRank.CalculatePageRank(url, BacklinkChecker.GetLinks(url), GetBacklinks(url));
                Console.WriteLine("PageRank: " + pagerank);

                //Add pagerank to rank
                rank += pagerank;
                Console.WriteLine("Rank: " + rank);

                result.Rank = rank;

                SaveWebsiteInfoToJson(result);
            }
            catch (WebException ex)
            {
                Console.WriteLine("Error downloading website content: " + ex.Message);
            }

            return result;
        }

        static bool IsURL(string input)
        {
            // Regular expression pattern to match URLs
            string pattern = @"^(https?|ftp|file)://[-a-zA-Z0-9+&@#/%?=~_|!:,.;]*[-a-zA-Z0-9+&@#/%=~_|]";

            // Create Regex object
            Regex regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // Check if the input matches the URL pattern
            return regex.IsMatch(input);
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
            string jsonFilePath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\results.json";
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
            string jsonFilePath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\results.json";

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
            string jsonFilePath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\results.json";

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

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

                connection.Close();

                File.Delete(jsonFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error importing results to MSSQL: " + ex.Message);
            }
        }

        public static void ImportBacklinksToDB(List<Backlinks> links)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(links);
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(json);

            string connectionString = Config.conString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                if(connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    if(dt.Rows.Count > 0)
                    {
                        bulkCopy.DestinationTableName = "Backlinks"; // Specify your table name

                        // Map the DataTable columns with the database table columns
                        bulkCopy.ColumnMappings.Add("Source", "Source");
                        bulkCopy.ColumnMappings.Add("Target", "Target");

                        // Write the DataTable to the database
                        bulkCopy.WriteToServer(dt);

                        connection.Close();
                    }
                }
            }
        }

        static List<string> GetBacklinks(string url)
        {
            List<string> targets = new List<string>();

            string connectionString = Config.conString;
            SqlConnection connection = new SqlConnection(connectionString);

            string sqlQuery = "SELECT Target FROM Backlinks WHERE Target = @Url";
            using (SqlCommand command = new SqlCommand(sqlQuery, connection))
            {
                command.Parameters.AddWithValue("@Url", url);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string target = Convert.ToString(reader["Target"]);
                        targets.Add(target);
                    }
                }
            }

            return targets;
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
            string jsonFilePath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\visitlist.json";

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
            string jsonFilePath = "C:\\Users\\ardam\\Documents\\GitHub\\crawler\\results.json";

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
