using System;

namespace crawler
{
    public static class Config
    {
        // ElasticSearch configuration
        public static string ElasticSearchUrl { get; set; } = "http://localhost:9200";
        
        // Add any other configuration parameters here
        public static int MaxConcurrentRequests { get; set; } = 5;
        public static int RequestTimeoutSeconds { get; set; } = 30;
        
        // Initialize configuration from environment variables or config file
        static Config()
        {
            // Read from environment variables if available
            string esUrl = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL");
            if (!string.IsNullOrEmpty(esUrl))
            {
                ElasticSearchUrl = esUrl;
            }
        }
    }
}