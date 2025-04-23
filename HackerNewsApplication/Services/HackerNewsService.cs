using HackerNewsApplication.Models;
using HackerNewsApplication.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace HackerNewsApi.Services
{
    public class HackerNewsService : IHackerNewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HackerNewsService> _logger;
        private const string ApiBaseUrl = "https://hacker-news.firebaseio.com/v0/";
        private const string CacheKeyNewestStories = "newest_stories";
        private const string CacheKeyStoryDetails = "story_details_{0}";
        private const int CacheExpirationMinutes = 5;

        public HackerNewsService(HttpClient httpClient, IMemoryCache cache, ILogger<HackerNewsService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<story>> GetNewestStoriesAsync(int pageNumber, int pageSize, string searchTerm = null)
        {
            try
            {
                var storyIds = await GetNewestStoryIdsAsync();
                var stories = new List<story>();

                foreach (var id in storyIds)
                {
                    var story = await GetStoryDetailsAsync(id);
                    if (story != null)
                    {
                        stories.Add(story);
                    }
                }

                // Apply search filter if search term provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    stories = stories.Where(s =>
                        (s.Title != null && s.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (s.By != null && s.By.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // Apply paging
                return stories
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting newest stories");
                throw;
            }
        }

        public async Task<int> GetTotalStoriesCountAsync(string searchTerm = null)
        {
            var storyIds = await GetNewestStoryIdsAsync();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var stories = new List<story>();
                foreach (var id in storyIds)
                {
                    var story = await GetStoryDetailsAsync(id);
                    if (story != null)
                    {
                        stories.Add(story);
                    }
                }

                return stories.Count(s =>
                    (s.Title != null && s.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (s.By != null && s.By.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                );
            }

            return storyIds.Count;
        }

        private async Task<List<int>> GetNewestStoryIdsAsync()
        {
            if (!_cache.TryGetValue(CacheKeyNewestStories, out List<int> storyIds))
            {
                var response = await _httpClient.GetStringAsync($"{ApiBaseUrl}newstories.json");
                storyIds = JsonSerializer.Deserialize<List<int>>(response);

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes));

                _cache.Set(CacheKeyNewestStories, storyIds, cacheOptions);
            }

            return storyIds;
        }

        private async Task<story> GetStoryDetailsAsync(int id)
        {
            var cacheKey = string.Format(CacheKeyStoryDetails, id);

            if (!_cache.TryGetValue(cacheKey, out story story))
            {
                try
                {
                    var response = await _httpClient.GetStringAsync($"{ApiBaseUrl}item/{id}.json");
                    story = JsonSerializer.Deserialize<story>(response, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (story != null)
                    {
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes));

                        _cache.Set(cacheKey, story, cacheOptions);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting story details for ID: {id}");
                    return null;
                }
            }

            return story;
        }
    }
}