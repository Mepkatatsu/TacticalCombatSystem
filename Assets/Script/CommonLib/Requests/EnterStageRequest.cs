namespace Script.CommonLib.Requests
{
    public sealed class EnterStageRequest
    {
        public ulong UserId { get; }
        public string RequestId { get; }

        public EnterStageRequest(ulong userId, string requestId)
        {
            UserId = userId;
            RequestId = requestId;
        }
    }
}
