using Script.CommonLib.Responses;

namespace MiniServerProject.Application.Cheats
{
    public interface ICheatService
    {
        Task<CheatStamina100Response> CheatStamina100(ulong userId, string requestId, CancellationToken ct);
    }
}
