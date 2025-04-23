using HackerNewsApplication.Models;

namespace HackerNewsApplication.Services.Interfaces
{
    public interface IHackerNewsService
    {
        Task<List<story>> GetNewestStoriesAsync(int pageNumber, int pageSize, string searchTerm = null);
        Task<int> GetTotalStoriesCountAsync(string searchTerm = null);
    }
}
