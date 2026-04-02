using System.Text.Json;
using Contentful.Core.Configuration;
using FGFB.Models;

namespace FGFB.Services
{
    public class ContentfulService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _cacheFilePath;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(4);

        public ContentfulService(IWebHostEnvironment environment)
        {
            _environment = environment;

            var cacheFolder = Path.Combine(_environment.ContentRootPath, "App_Data");
            Directory.CreateDirectory(cacheFolder);

            _cacheFilePath = Path.Combine(cacheFolder, "contentful-blog-cache.json");
        }

        public async Task<ContentfulResponse?> GetContentfulEntriesAsync(bool forceRefresh = false)
        {
            var json = await GetCachedOrFreshJsonAsync(forceRefresh);

            return JsonSerializer.Deserialize<ContentfulResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        private async Task<string> GetCachedOrFreshJsonAsync(bool forceRefresh)
        {
            if (!forceRefresh && File.Exists(_cacheFilePath))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(_cacheFilePath);
                var age = DateTime.UtcNow - lastWriteTime;

                if (age < CacheDuration)
                {
                    return await File.ReadAllTextAsync(_cacheFilePath);
                }
            }

            var freshJson = await FetchFromContentfulAsync();
            await File.WriteAllTextAsync(_cacheFilePath, freshJson);

            return freshJson;
        }

        private async Task<string> FetchFromContentfulAsync()
        {
            using var httpClient = new HttpClient();

            var options = new ContentfulOptions
            {
                DeliveryApiKey = "CNCPeStp5hELxc6tifknROVpGWAAIr93W31eNr3Pfro",
                SpaceId = "3hrbdcg88kgt"
            };

            var url =
                $"https://cdn.contentful.com/spaces/{options.SpaceId}/entries" +
                $"?access_token={options.DeliveryApiKey}" +
                $"&content_type=blogPost";

            return await httpClient.GetStringAsync(url);
        }
    }
}