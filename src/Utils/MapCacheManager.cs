using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TarkovClient;

namespace TarkovClient.Utils
{
    public static class MapCacheManager
    {
        private static readonly HttpClient _httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
            return client;
        }

        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(Env.CacheFolder))
                {
                    Directory.CreateDirectory(Env.CacheFolder);
                }
            }
            catch (Exception) { }
        }
        public struct CacheResult
        {
            public Stream Stream;
            public string ContentType;
        }

        public static async Task<CacheResult> GetCachedResourceAsync(string url)
        {
            try
            {
                Initialize();

                // 1. Determine Content-Type and Extension from URL
                string extension = GetExtension(url);
                string contentType = GetContentType(extension);
                
                // 2. Generate filename from URL hash
                string filename = GetHashString(url) + extension;
                string cachePath = Path.Combine(Env.CacheFolder, filename);

                // 3. Check if exists in cache
                if (File.Exists(cachePath))
                {
                    return new CacheResult { Stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read), ContentType = contentType };
                }

                // 4. Download if not exists
                using (var response = await _httpClient.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var contentBytes = await response.Content.ReadAsByteArrayAsync();

                        // PATCHING JS FILES
                        if (extension == ".js")
                        {
                            string content = Encoding.UTF8.GetString(contentBytes);
                            
                            // 1. Remove Quest Selection Limit (Ds=3 -> Ds=999)
                            // We replace potential variable definitions of Ds=3
                            content = content.Replace("Ds=3", "Ds=999");
                            content = content.Replace("Ds = 3", "Ds = 999");

                            // 2. Remove "length" checks (generic fallback) - SAFE
                            content = content.Replace("length>2", "length>999");
                            content = content.Replace("length>=3", "length>=999");

                            /* REVERTED: Aggressive Pro patching caused UI breakages
                            content = content.Replace(".pro", ".pro||true");
                            content = content.Replace("[\"pro\"]", "[\"pro\"]||true");
                            */

                            contentBytes = Encoding.UTF8.GetBytes(content);
                        }

                        using (var fileStream = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
                        {
                             await fileStream.WriteAsync(contentBytes, 0, contentBytes.Length);
                        }

                        return new CacheResult { Stream = new MemoryStream(contentBytes), ContentType = contentType };
                    }
                }
            }
            catch (Exception) { }

            return new CacheResult { Stream = null, ContentType = null };
        }

        private static string GetExtension(string url)
        {
            try {
                // Ignore query parameters
                int queryIndex = url.IndexOf('?');
                string cleanUrl = queryIndex >= 0 ? url.Substring(0, queryIndex) : url;
                string lowerUrl = cleanUrl.ToLowerInvariant();

                // Force JSON for API or Data endpoints
                if (lowerUrl.Contains("/api/") || lowerUrl.Contains("/data/"))
                {
                    return ".json";
                }

                if (lowerUrl.EndsWith(".js")) return ".js";
                if (lowerUrl.EndsWith(".css")) return ".css";
                if (lowerUrl.EndsWith(".svg")) return ".svg";
                if (lowerUrl.EndsWith(".png")) return ".png";
                if (lowerUrl.EndsWith(".jpg") || lowerUrl.EndsWith(".jpeg")) return ".jpg";
                if (lowerUrl.EndsWith(".gif")) return ".gif";
                if (lowerUrl.EndsWith(".webp")) return ".webp";
                if (lowerUrl.EndsWith(".ico")) return ".ico";
                if (lowerUrl.EndsWith(".woff2")) return ".woff2";
                if (lowerUrl.EndsWith(".woff")) return ".woff";
                if (lowerUrl.EndsWith(".ttf")) return ".ttf";
                if (lowerUrl.EndsWith(".eot")) return ".eot";
                if (lowerUrl.EndsWith(".json")) return ".json";
                
                // If the URL looks like a path without extension (e.g. /maps/factory), assume HTML
                var uri = new Uri(cleanUrl);
                string path = uri.AbsolutePath;
                string ext = Path.GetExtension(path);
                
                if (string.IsNullOrEmpty(ext))
                {
                   return ".html";
                }
                
                return ext.ToLower();
            } catch {}

            return ".dat";
        }

        private static string GetContentType(string extension)
        {
            switch (extension)
            {
                case ".html": return "text/html";
                case ".js": return "application/javascript";
                case ".css": return "text/css";
                case ".svg": return "image/svg+xml";
                case ".png": return "image/png";
                case ".jpg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".webp": return "image/webp";
                case ".ico": return "image/x-icon";
                case ".woff2": return "font/woff2";
                case ".woff": return "font/woff";
                case ".ttf": return "font/ttf";
                case ".eot": return "application/vnd.ms-fontobject";
                case ".json": return "application/json";
                default: return "application/octet-stream";
            }
        }

        private static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        private static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
    }
}
