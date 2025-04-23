using HackerNewsApplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController : ControllerBase
    {
        private readonly IHackerNewsService _hackerNewsService;
        private readonly ILogger<StoriesController> _logger;

        public StoriesController(IHackerNewsService hackerNewsService, ILogger<StoriesController> logger)
        {
            _hackerNewsService = hackerNewsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetNewestStories([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchTerm = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 50) pageSize = 50; // Limit max page size

                var stories = await _hackerNewsService.GetNewestStoriesAsync(pageNumber, pageSize, searchTerm);
                var totalCount = await _hackerNewsService.GetTotalStoriesCountAsync(searchTerm);

                return Ok(new
                {
                    items = stories,
                    totalCount = totalCount,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stories");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
