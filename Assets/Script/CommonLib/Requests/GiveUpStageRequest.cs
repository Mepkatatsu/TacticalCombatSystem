namespace MiniServerProject.Shared.Requests
{
    public sealed class GiveUpStageRequest
    {
        public ulong UserId { get; }
        public string RequestId { get; }

        public GiveUpStageRequest(ulong userId, string requestId)
        {
            UserId = userId;
            RequestId = requestId;
        }
    }
}
