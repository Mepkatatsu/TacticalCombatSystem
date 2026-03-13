using System;
using System.Collections.Generic;

namespace Script.CommonLib.Requests
{
    public sealed class VerifyStageBattleRequest
    {
        public ulong UserId { get; }
        public string RequestId { get; }
        public List<ushort> UpdateIntervals { get; }
        public List<Tuple<uint, uint>> AliveEntities { get; set; }
        public TeamFlag Winner { get; set; }

        public VerifyStageBattleRequest(ulong userId, string requestId, List<ushort> updateIntervals, List<Tuple<uint, uint>> aliveEntities, TeamFlag winner)
        {
            UserId = userId;
            RequestId = requestId;
            UpdateIntervals = updateIntervals;
            AliveEntities = aliveEntities;
            Winner = winner;
        }
    }
}
