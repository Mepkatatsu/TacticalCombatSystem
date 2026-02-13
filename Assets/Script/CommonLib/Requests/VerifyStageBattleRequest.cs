using System.Collections.Generic;

namespace Script.CommonLib.Requests
{
    public sealed class VerifyStageBattleRequest
    {
        public ulong UserId { get; }
        public string RequestId { get; }
        public List<float> UpdateIntervals { get; }

        public VerifyStageBattleRequest(ulong userId, string requestId, List<float> updateIntervals)
        {
            UserId = userId;
            RequestId = requestId;
            UpdateIntervals = updateIntervals;
        }
    }
}
