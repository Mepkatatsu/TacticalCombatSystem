using System.Collections.Generic;

namespace Script.CommonLib.Requests
{
    public sealed class VerifyStageBattleRequest
    {
        public ulong UserId { get; }
        public string RequestId { get; }
        public List<ushort> UpdateIntervals { get; }

        public VerifyStageBattleRequest(ulong userId, string requestId, List<ushort> updateIntervals)
        {
            UserId = userId;
            RequestId = requestId;
            UpdateIntervals = updateIntervals;
        }
    }
}
