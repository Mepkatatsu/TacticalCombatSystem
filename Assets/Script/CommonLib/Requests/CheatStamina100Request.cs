namespace MiniServerProject.Shared.Requests
{
    public sealed class CheatStamina100Request
    {
        public ulong UserId { get; }
        public string RequestId { get; }

        public CheatStamina100Request(ulong userId, string requestId)
        {
            UserId = userId;
            RequestId = requestId;
        }
    }
}
