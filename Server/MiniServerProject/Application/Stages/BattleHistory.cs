using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MiniServerProject.Domain.Battle;
using MiniServerProject.Domain.ServerLogs;
using MiniServerProject.Infrastructure;
using MiniServerProject.Infrastructure.Persistence;
using Script.CommonLib;
using Script.CommonLib.Map;
using Script.CommonLib.Responses;
using Script.CommonLib.Tables;

namespace MiniServerProject.Application.Stages
{
    public class BattleHistory
    {
        public bool IsVerified;
        public long VerifyMs;
        public TeamFlag Winner;
        public uint BattleMs;
        public uint BattleFrames;
        public float AvgFps;

        public BattleHistory(bool isVerified, long verifyMs, TeamFlag winner, uint battleMs, uint battleFrames)
        {
            IsVerified = isVerified;
            VerifyMs = verifyMs;
            Winner = winner;
            BattleMs = battleMs;
            BattleFrames = battleFrames;
            
            var battleSec = battleMs / 1000f;
            AvgFps = (float)Math.Round(battleFrames / battleSec, 2);
        }

        public override string ToString()
        {
            return $"isVerified: {IsVerified}, verifyMs: {VerifyMs}ms, winner: {Winner}, battleMs: {BattleMs}, battleFrames: {BattleFrames}, avgFps: {AvgFps}";
        }
    }
}
