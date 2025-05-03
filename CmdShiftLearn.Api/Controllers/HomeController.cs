using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmdShiftLearn.Api.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        /// <summary>
        /// Root endpoint that returns a simple message indicating the API is running
        /// </summary>
        /// <returns>A simple status message</returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return Ok(new { status = "API is live", version = "1.0" });
        }
    }
}
