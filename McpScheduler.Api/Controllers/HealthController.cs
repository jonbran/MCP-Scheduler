using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace McpScheduler.Api.Controllers
{
    /// <summary>
    /// Controller for system health checks
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Gets the health status of the service
        /// </summary>
        /// <returns>The health status</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "healthy" });
        }

        /// <summary>
        /// Configures the health check response writer
        /// </summary>
        /// <returns>A task that completes when the writing is done</returns>
        public static Task WriteResponse(HttpContext context, HealthReport healthReport)
        {
            context.Response.ContentType = MediaTypeNames.Application.Json;

            var response = new Dictionary<string, object>
            {
                { "status", healthReport.Status.ToString() },
                { "checks", healthReport.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration
                    })
                },
                { "totalDuration", healthReport.TotalDuration }
            };

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true })
            );
        }
    }
}
