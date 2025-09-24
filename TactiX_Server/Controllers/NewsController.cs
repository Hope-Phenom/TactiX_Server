using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Net;
using TactiX_Server.Data;
using TactiX_Server.Models.News;

namespace TactiX_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : ControllerBase
    {
        private readonly ILogger<NewsController> _logger;
        private readonly NewsDbContext _context;

        public NewsController(
            ILogger<NewsController> logger,
            NewsDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet("GetNews")]
        public async Task<IActionResult> GetNews()
        {
            try
            {
                var results = await _context.NewsCommunityItems
                    .Where(n => n.Type == 0 || n.Type == 1)
                    .GroupBy(n => n.Type)
                    .Select(g => g.OrderByDescending(n => n.Update_DateTime).FirstOrDefault())
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetNews Error: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}