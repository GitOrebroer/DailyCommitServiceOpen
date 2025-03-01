using Core.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DailyCommitService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RefreshController : Controller
    {
        private readonly ILoggerHelper logger;

        public RefreshController(ILoggerHelper logger)
        {
            this.logger = logger;
        }


        [HttpGet]
        public IActionResult RefreshApp()
        {
            logger.Log($"Application has been refreshed at: {DateTime.UtcNow} (utc now time)",false);
            return Ok("You successfully refreshed app");
        }
    }
}
