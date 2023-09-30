using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crawler
{
    public static class UrlComparer
    {
        public static bool AreUrlsDifferent(string url1, string url2)
        {
            // Parse the URLs into Uri objects
            Uri uri1, uri2;
            if (!Uri.TryCreate(url1, UriKind.Absolute, out uri1) ||
                !Uri.TryCreate(url2, UriKind.Absolute, out uri2))
            {
                throw new ArgumentException("Invalid URL format");
            }

            // Compare the host (domain) and subdomains
            string[] hostSegments1 = uri1.Host.Split('.');
            string[] hostSegments2 = uri2.Host.Split('.');

            // Compare the last two segments of the host, which should be the main domain and top-level domain
            if (!string.Equals(hostSegments1[hostSegments1.Length - 2], hostSegments2[hostSegments2.Length - 2], StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(hostSegments1[hostSegments1.Length - 1], hostSegments2[hostSegments2.Length - 1], StringComparison.OrdinalIgnoreCase))
            {
                return true; // URLs have different domains
            }

            // URLs are the same
            return false;
        }
    }
}
