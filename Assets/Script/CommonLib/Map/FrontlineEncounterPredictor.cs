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
            out FixedPos blueEncounterPosition,
            out FixedPos redEncounterPosition)
        {
            blueEncounterPosition = default;
            redEncounterPosition = default;

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
            var blueSimulatedFrontline = CreatePredictionEntity(1, simulator, blueFrontline);
            var redSimulatedFrontline = CreatePredictionEntity(2, simulator, redFrontline);

            simulator.OnEntityAdded(blueSimulatedFrontline.Id, blueSimulatedFrontline);
            simulator.OnEntityAdded(redSimulatedFrontline.Id, redSimulatedFrontline);
            blueSimulatedFrontline.SetPos(blueFrontline.GetPos());
            redSimulatedFrontline.SetPos(redFrontline.GetPos());

            if (!TrySetSimulationPath(blueSimulatedFrontline, blueFrontline, pathFinder) ||
                !TrySetSimulationPath(redSimulatedFrontline, redFrontline, pathFinder))
            {
                return false;
            }

            for (uint elapsedMs = 0; elapsedMs < MaxPredictionMs; elapsedMs += PredictionDeltaMs)
            {
                simulator.Update(PredictionDeltaMs);

                if (blueSimulatedFrontline.CanAttackMainTarget() && redSimulatedFrontline.CanAttackMainTarget())
                {
                    blueEncounterPosition = blueSimulatedFrontline.GetPos();
                    redEncounterPosition = redSimulatedFrontline.GetPos();
                    return true;
                }

                if (blueSimulatedFrontline.HasArrived() && redSimulatedFrontline.HasArrived())
                    return false;
            }

            return false;
        }

        private static bool TrySetSimulationPath(
            Entity simulatedFrontline,
            Entity sourceFrontline,
            BattleMapPathFinder pathFinder)
        {
            var destination = sourceFrontline.GetDestinationForTest();
            var paths = new List<GridPos>();

            if (!pathFinder.TryFindWaypointsBetweenAnyPositions(
                    simulatedFrontline.GetPos().ToGridPos(), destination.ToGridPos(), paths))
            {
                return false;
            }

            simulatedFrontline.SetPredictionDestination(destination, paths);
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
