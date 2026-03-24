using Microsoft.AspNetCore.Mvc;

namespace Netaq.Api.Controllers;

/// <summary>
/// Health check endpoint for monitoring and load balancer probes.
/// This endpoint is public (no authentication required).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Basic health check — returns 200 OK if the API is running.
    /// Used by Docker health checks, Nginx, and monitoring scripts.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "NETAQ Portal API",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
