using System;
using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    internal static class InitialEncounterDetector
    {
        public static bool HasEncounter(List<Entity> blueEntities, List<Entity> redEntities)
        {
            for (var blueIndex = 0; blueIndex < blueEntities.Count; blueIndex++)
            {
                var blue = blueEntities[blueIndex];

                for (var redIndex = 0; redIndex < redEntities.Count; redIndex++)
                {
                    var red = redEntities[redIndex];
                    var distance = blue.GetPos().GetDistance(red.GetPos());

                    if (distance <= blue.AttackRange || distance <= red.AttackRange)
                        return true;
                }
            }

            return false;
        }
    }

    internal sealed class InitialTacticalFormationPlanner
    {
        private const int SafeAttackRangePercent = 90;
        private const int MinimumAllySpacing = 3000;
        private const int PreferredAllySpacing = 6000;
        private const int LateralCandidateStep = 3000;
        private const int MoveDistanceScoreWeight = 15;
        private const int LateralCrossingScorePenalty = PreferredAllySpacing * MoveDistanceScoreWeight * 2;

        private static readonly int[] CandidateRangePercents = { 90, 85, 80 };
        private static readonly int[] LateralCandidateIndices = { 0, 1, -1, 2, -2, 3, -3 };

        private readonly BattleMapData _battleMapData;
        private readonly BattleMapPathFinder _battleMapPathFinder;
        private readonly FrontlineEncounterPredictor _frontlineEncounterPredictor;

        public InitialTacticalFormationPlanner(
            BattleMapData battleMapData,
            BattleMapPathFinder battleMapPathFinder)
        {
            _battleMapData = battleMapData;
            _battleMapPathFinder = battleMapPathFinder;
            _frontlineEncounterPredictor = new FrontlineEncounterPredictor(battleMapData);
        }

        public bool TryApply(List<Entity> blueEntities, List<Entity> redEntities)
        {
            var blueFrontline = GetFrontlineEntity(blueEntities);
            var redFrontline = GetFrontlineEntity(redEntities);

            if (blueFrontline == null || redFrontline == null)
                return false;

            if (!_frontlineEncounterPredictor.TryPredict(
                    blueFrontline,
                    redFrontline,
                    out var blueFrontlinePosition,
                    out var redFrontlinePosition))
            {
                return false;
            }

            var blueApplied = TryPlanAndApplyTeam(
                blueEntities,
                blueFrontline,
                blueFrontlinePosition,
                redFrontlinePosition);
            var redApplied = TryPlanAndApplyTeam(
                redEntities,
                redFrontline,
                redFrontlinePosition,
                blueFrontlinePosition);
            return blueApplied || redApplied;
        }

        private bool TryPlanAndApplyTeam(
            List<Entity> entities,
            Entity frontline,
            FixedPos frontlinePosition,
            FixedPos enemyFrontlinePosition)
        {
            var reservedPositions = new List<FixedPos> { frontlinePosition };
            var placementOrder = new List<Entity>();

            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity != frontline)
                    placementOrder.Add(entity);
            }

            placementOrder.Sort(ComparePlacementOrder);

            var plannedDestinations = new List<PlannedDestination>();

            for (var i = 0; i < placementOrder.Count; i++)
            {
                var entity = placementOrder[i];
                if (!TryFindBestPosition(
                        entity,
                        frontlinePosition,
                        enemyFrontlinePosition,
                        reservedPositions,
                        out var destination,
                        out var paths))
                {
                    return false;
                }

                plannedDestinations.Add(new PlannedDestination(entity, destination, paths));
                reservedPositions.Add(destination);
            }

            for (var i = 0; i < plannedDestinations.Count; i++)
            {
                var plannedDestination = plannedDestinations[i];
                plannedDestination.Entity.SetTacticalDestination(
                    plannedDestination.Destination,
                    plannedDestination.Paths);
            }

            return true;
        }

        private bool TryFindBestPosition(
            Entity entity,
            FixedPos frontlinePosition,
            FixedPos enemyFrontlinePosition,
            List<FixedPos> reservedPositions,
            out FixedPos bestPosition,
            out List<GridPos> bestPaths)
        {
            bestPosition = default;
            bestPaths = null;

            var frontlineDelta = enemyFrontlinePosition - frontlinePosition;
            var frontlineDistance = frontlinePosition.GetDistance(enemyFrontlinePosition);
            if (frontlineDistance == 0)
                return false;

            var safeAttackRange = entity.AttackRange * SafeAttackRangePercent / 100;
            var hasBestPosition = false;
            var bestScore = long.MinValue;
            var currentPosition = entity.GetPos();

            for (var rangeIndex = 0; rangeIndex < CandidateRangePercents.Length; rangeIndex++)
            {
                var candidateRange = entity.AttackRange * CandidateRangePercents[rangeIndex] / 100;

                for (var lateralIndex = 0; lateralIndex < LateralCandidateIndices.Length; lateralIndex++)
                {
                    var lateralDistance = LateralCandidateIndices[lateralIndex] * LateralCandidateStep;
                    if (Math.Abs(lateralDistance) >= candidateRange)
                        continue;

                    var forwardDistance = MathHelper.IntSqrt(
                        (long)candidateRange * candidateRange - (long)lateralDistance * lateralDistance);
                    var candidate = CreateCandidatePosition(
                        enemyFrontlinePosition,
                        frontlineDelta,
                        frontlineDistance,
                        forwardDistance,
                        lateralDistance).ToGridPos().ToFixedPos();

                    if (!IsValidCandidate(
                            entity,
                            candidate,
                            enemyFrontlinePosition,
                            safeAttackRange,
                            reservedPositions,
                            out var candidatePaths))
                        continue;

                    var score = EvaluateCandidate(
                        currentPosition,
                        candidate,
                        frontlinePosition,
                        enemyFrontlinePosition,
                        safeAttackRange,
                        reservedPositions);

                    if (!hasBestPosition || score > bestScore ||
                        score == bestScore && IsPositionBefore(candidate, bestPosition))
                    {
                        bestPosition = candidate;
                        bestPaths = candidatePaths;
                        bestScore = score;
                        hasBestPosition = true;
                    }
                }
            }

            return hasBestPosition;
        }

        private bool IsValidCandidate(
            Entity entity,
            FixedPos candidate,
            FixedPos enemyFrontlinePosition,
            long safeAttackRange,
            List<FixedPos> reservedPositions,
            out List<GridPos> paths)
        {
            paths = null;
            var gridPosition = candidate.ToGridPos();
            if (gridPosition.x < _battleMapData.minGridPos.x || gridPosition.x > _battleMapData.maxGridPos.x ||
                gridPosition.y < _battleMapData.minGridPos.y || gridPosition.y > _battleMapData.maxGridPos.y)
            {
                return false;
            }

            if (candidate.GetDistance(enemyFrontlinePosition) > safeAttackRange)
                return false;

            var candidatePaths = new List<GridPos>();
            if (!_battleMapPathFinder.TryFindWaypoints(entity.GetPos().ToGridPos(), gridPosition, candidatePaths))
                return false;

            for (var i = 0; i < reservedPositions.Count; i++)
            {
                if (candidate.GetDistance(reservedPositions[i]) < MinimumAllySpacing)
                    return false;
            }

            paths = candidatePaths;
            return true;
        }

        private long EvaluateCandidate(
            FixedPos currentPosition,
            FixedPos candidate,
            FixedPos frontlinePosition,
            FixedPos enemyFrontlinePosition,
            long safeAttackRange,
            List<FixedPos> reservedPositions)
        {
            var attackRangeError = Math.Abs(safeAttackRange - candidate.GetDistance(enemyFrontlinePosition));
            var nearestAllyDistance = GetNearestDistance(candidate, reservedPositions);
            var allySpacingError = Math.Abs(PreferredAllySpacing - nearestAllyDistance);
            var moveDistance = currentPosition.GetDistance(candidate);
            var centerZ = (_battleMapData.minGridPos.y + _battleMapData.maxGridPos.y) * PositionConverter.FixedPosMultiplier / 2L;
            var centerDistance = Math.Abs(candidate.Z - centerZ);

            var frontlineDelta = enemyFrontlinePosition - frontlinePosition;
            var frontlineDistance = frontlinePosition.GetDistance(enemyFrontlinePosition);
            var candidateDelta = candidate - frontlinePosition;
            var forwardProjection = (candidateDelta.X * frontlineDelta.X + candidateDelta.Z * frontlineDelta.Z) /
                                    Math.Max(1, frontlineDistance);
            var excessiveAdvance = Math.Max(0, forwardProjection);
            var currentLateralProjection = GetLateralProjection(
                currentPosition,
                frontlinePosition,
                frontlineDelta,
                frontlineDistance);
            var candidateLateralProjection = GetLateralProjection(
                candidate,
                frontlinePosition,
                frontlineDelta,
                frontlineDistance);
            var lateralCrossingPenalty = currentLateralProjection != 0 &&
                                         candidateLateralProjection != 0 &&
                                         Math.Sign(currentLateralProjection) != Math.Sign(candidateLateralProjection)
                ? LateralCrossingScorePenalty
                : 0;

            return -attackRangeError * 100
                   - allySpacingError * 20
                   - excessiveAdvance * 200
                   - moveDistance * MoveDistanceScoreWeight
                   - centerDistance * 10
                   - lateralCrossingPenalty;
        }

        private static long GetLateralProjection(
            FixedPos position,
            FixedPos frontlinePosition,
            FixedPos frontlineDelta,
            long frontlineDistance)
        {
            var positionDelta = position - frontlinePosition;
            return (-positionDelta.X * frontlineDelta.Z + positionDelta.Z * frontlineDelta.X) /
                   Math.Max(1, frontlineDistance);
        }

        private static FixedPos CreateCandidatePosition(
            FixedPos enemyFrontlinePosition,
            FixedPos frontlineDelta,
            long frontlineDistance,
            long forwardDistance,
            long lateralDistance)
        {
            var forwardX = frontlineDelta.X * forwardDistance / frontlineDistance;
            var forwardZ = frontlineDelta.Z * forwardDistance / frontlineDistance;
            var lateralX = -frontlineDelta.Z * lateralDistance / frontlineDistance;
            var lateralZ = frontlineDelta.X * lateralDistance / frontlineDistance;

            return new FixedPos(
                enemyFrontlinePosition.X - forwardX + lateralX,
                enemyFrontlinePosition.Y,
                enemyFrontlinePosition.Z - forwardZ + lateralZ);
        }

        private static long GetNearestDistance(FixedPos position, List<FixedPos> otherPositions)
        {
            var nearestDistance = long.MaxValue;

            for (var i = 0; i < otherPositions.Count; i++)
            {
                var distance = position.GetDistance(otherPositions[i]);
                if (distance < nearestDistance)
                    nearestDistance = distance;
            }

            return nearestDistance;
        }

        private static Entity GetFrontlineEntity(List<Entity> entities)
        {
            Entity frontline = null;

            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.IsAlive())
                    continue;

                if (frontline == null || ComparePlacementOrder(entity, frontline) < 0)
                    frontline = entity;
            }

            return frontline;
        }

        private static int ComparePlacementOrder(Entity first, Entity second)
        {
            var rangeComparison = first.AttackRange.CompareTo(second.AttackRange);
            return rangeComparison != 0 ? rangeComparison : first.Id.CompareTo(second.Id);
        }

        private static bool IsPositionBefore(FixedPos first, FixedPos second)
        {
            if (first.X != second.X)
                return first.X < second.X;

            return first.Z < second.Z;
        }

        private sealed class PlannedDestination
        {
            public PlannedDestination(Entity entity, FixedPos destination, List<GridPos> paths)
            {
                Entity = entity;
                Destination = destination;
                Paths = paths;
            }

            public Entity Entity { get; }
            public FixedPos Destination { get; }
            public List<GridPos> Paths { get; }
        }
    }

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
                useInitialTacticalPositioning = false,
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

            if (!pathFinder.TryFindWaypoints(
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
