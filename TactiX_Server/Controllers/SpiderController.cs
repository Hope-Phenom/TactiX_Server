
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Net;
using TactiX_Server.Models.News;

namespace TactiX_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpiderController : ControllerBase
    {
        private readonly ILogger<SpiderController> _logger;

        public SpiderController(
            ILogger<SpiderController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetNews")]
        public async Task<IActionResult> GetNews()
        {
            return Ok();
        }
    }
}