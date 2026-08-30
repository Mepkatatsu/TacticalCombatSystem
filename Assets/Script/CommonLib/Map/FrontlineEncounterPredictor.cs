using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    internal sealed class FrontlineEncounterPredictor
    {
        private const ushort PredictionDeltaMs = 50;
        private const uint MaxPredictionMs = 60000;

        private readonly BattleMapData _battleMapData;

        public FrontlineEncounterPredictor(BattleMapData battleMapData)
        {
            _battleMapData = battleMapData;
        }

        public bool TryPredict(
            Entity blueFrontline,
            Entity redFrontline,
            out FixedPos blueFrontlinePosition,
            out FixedPos redFrontlinePosition)
        {
            blueFrontlinePosition = default;
            redFrontlinePosition = default;

            var predictionMapData = new BattleMapData
            {
                minGridPos = _battleMapData.minGridPos,
                maxGridPos = _battleMapData.maxGridPos,
                battlePositions = _battleMapData.battlePositions,
                obstacles = _battleMapData.obstacles,
                entities = new List<EntityData>(),
            };
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, predictionMapData);
            var pathFinder = new BattleMapPathFinder(predictionMapData);
            var bluePrediction = CreatePredictionEntity(1, simulator, blueFrontline);
            var redPrediction = CreatePredictionEntity(2, simulator, redFrontline);

            simulator.OnEntityAdded(bluePrediction.Id, bluePrediction);
            simulator.OnEntityAdded(redPrediction.Id, redPrediction);
            bluePrediction.SetPos(blueFrontline.GetPos());
            redPrediction.SetPos(redFrontline.GetPos());

            if (!TrySetPredictionPath(bluePrediction, blueFrontline, pathFinder) ||
                !TrySetPredictionPath(redPrediction, redFrontline, pathFinder))
            {
                return false;
            }

            for (uint elapsedMs = 0; elapsedMs < MaxPredictionMs; elapsedMs += PredictionDeltaMs)
            {
                simulator.Update(PredictionDeltaMs);

                if (bluePrediction.IsMainTargetInRange() && redPrediction.IsMainTargetInRange())
                {
                    blueFrontlinePosition = bluePrediction.GetPos();
                    redFrontlinePosition = redPrediction.GetPos();
                    return true;
                }

                if (bluePrediction.HasArrived() && redPrediction.HasArrived())
                    return false;
            }

            return false;
        }

        private static bool TrySetPredictionPath(
            Entity prediction,
            Entity source,
            BattleMapPathFinder pathFinder)
        {
            var destination = source.GetDestinationForTest();
            var paths = new List<GridPos>();

            if (!pathFinder.TryFindWaypointsFromArbitraryPositions(
                    prediction.GetPos().ToGridPos(),
                    destination.ToGridPos(),
                    paths))
            {
                return false;
            }

            prediction.SetPredictionDestination(destination, paths);
            return true;
        }

        private static Entity CreatePredictionEntity(uint entityId, IBattleMapContext context, Entity source)
        {
            return new Entity(entityId, context, new EntityData
            {
                teamFlag = source.GetTeamFlag(),
                name = source.name,
                endPositionName = source.endPositionName,
                projectileName = source.projectileName,
                maxHp = 1,
                attackDamage = 0,
                attackDelayMs = ushort.MaxValue,
                attackRange = source.AttackRange,
                moveSpeed = source.MoveSpeed,
            });
        }
    }

    internal sealed class NullBattleMapEventHandler : IBattleMapEventHandler
    {
        public static NullBattleMapEventHandler Instance { get; } = new();

        private NullBattleMapEventHandler() { }

        public void OnEntityAdded(uint entityId, Entity entity) { }
        public void OnEntityPositionChanged(uint entityId, FixedPos pos) { }
        public void OnEntityDirectionChanged(uint entityId, FixedDir dir) { }
        public void OnEntityStartMove(uint entityId) { }
        public void OnEntityStopMove(uint entityId) { }
        public void OnEntityStartAttack(uint attackerId, uint targetId) { }
        public void OnEntityGetDamage(uint entityId, uint damage) { }
        public void OnEntityRetired(uint entityId) { }
        public void OnProjectileAdded(ulong projectileId, Projectile projectile) { }
        public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos) { }
        public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir) { }
        public void OnProjectileTriggered(ulong projectileId) { }
        public void OnBattleEnd(TeamFlag winner) { }
        public void OnBattleMapUpdated(ushort deltaMs) { }
    }
}
