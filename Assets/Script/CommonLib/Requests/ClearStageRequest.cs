namespace Script.CommonLib.Requests
{
    public class ClearStageRequest
    {
        public ulong UserId { get; }
        public string RequestId { get; }

        public ClearStageRequest(ulong userId, string requestId)
        {
            UserId = userId;
            RequestId = requestId;
        }
    }
}
