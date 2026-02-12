using Microsoft.AspNetCore.Mvc;
using MiniServerProject.Application;
using MiniServerProject.Application.Cheats;
using MiniServerProject.Shared.Requests;

namespace MiniServerProject.Controllers
{
    [ApiController]
    [Route("cheat")]
    public sealed class CheatController : ControllerBase
    {
        private readonly ICheatService _cheatService;

        public CheatController(ICheatService cheatService)
        {
            _cheatService = cheatService;
        }

        [HttpPost("{userId}/stamina100")]
        [HttpPost]
        public async Task<IActionResult> CheatStamina100([FromBody] CheatStamina100Request request, CancellationToken ct)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                              ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? "Development";

            if (environment != "Development")
                throw new DomainException(ErrorType.DevelopmentEnvironmentOnly);
            
            var resp = await _cheatService.CheatStamina100(request.UserId, request.RequestId, ct);
            return Ok(resp);
        }
    }
}
