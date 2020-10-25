using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace geo_auth_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("no-authentication")]
        public IActionResult GetUnauthenticatedResponse()
        {
            return Ok("No-authentication test succeeded");
        }

        [Authorize]
        [HttpGet("authentication")]
        public IActionResult GetAuthenticatedResponse()
        {
            return Ok("Authentication test succeeded");
        }
    }
}
