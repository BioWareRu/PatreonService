using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PatreonService.Core;

namespace PatreonService.Controllers
{
    [Route("v1/[controller]")]
    public class GoalsController : Controller
    {
        [HttpGet("current")]
        public async Task<IActionResult> CurrentAsync([FromServices] IMemoryCache cache,
            [FromServices] PatreonApi patreonApi, [FromServices] ILogger<GoalsController> logger)
        {
            var currentGoal = cache.Get<PatreonGoal>("patreonCurrentGoal");
            if (currentGoal == null)
            {
                try
                {
                    currentGoal = await patreonApi.GetCurrentGoalAsync();
                    cache.Set("patreonCurrentGoal", currentGoal, TimeSpan.FromHours(1));
                }
                catch (Exception ex)
                {
                    logger.LogError("Error while loading patreon goals: {ErrorText}", ex.Message);
                }
            }

            return Ok(currentGoal);
        }
    }
}