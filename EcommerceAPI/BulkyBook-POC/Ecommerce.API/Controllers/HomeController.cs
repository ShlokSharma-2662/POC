using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        [HttpGet("home")]
        public IActionResult Index()
        {
            return Ok(new
            {
                message = "Ecommerce API is running successfully!",
                version = "1.0.0",
                timestamp = DateTime.UtcNow,
                endpoints = new
                {
                    swagger = "/swagger",
                    api = "/api",
                    health = "/api/health"
                }
            });
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                service = "Ecommerce API"
            });
        }
    }
}






