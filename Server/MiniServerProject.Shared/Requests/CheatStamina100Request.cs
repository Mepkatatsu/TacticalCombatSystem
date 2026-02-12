namespace MiniServerProject.Shared.Requests
{
    public sealed class CheatStamina100Request
    {
        public ulong UserId { get; init; }
        public string RequestId { get; init; }

        public CheatStamina100Request(ulong userId, string requestId)
        {
            UserId = userId;
            RequestId = requestId;
        }
    }
}
