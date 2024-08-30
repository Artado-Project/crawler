using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crawler
{
    internal class PageRank
    {
        public static double CalculatePageRank(string url, List<string> hrefs, List<string> backlinksMap, double dampingFactor = 0.85, int iterations = 10)
        {
            int totalUrls = hrefs.Count;

            try
            {
                if (backlinksMap.Count > 0)
                {
                    // Initialize PageRank values
                    double rank = 1.0 / totalUrls;

                    double newRank = (1 - dampingFactor) / totalUrls;

                    // Perform iterations to update PageRank values
                    for (int i = 0; i < iterations; i++)
                    {
                        if (backlinksMap.Contains(url))
                        {
                            newRank += dampingFactor * (rank / backlinksMap.Count);
                        }
                    }
                    return newRank;
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}
