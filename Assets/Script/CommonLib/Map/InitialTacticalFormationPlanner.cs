using System;
using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    internal sealed class InitialTacticalFormationPlanner
    {
        private const int SafeAttackRangePercent = 90;
        private const int MinimumAllySpacing = 5500;
        private const int PreferredAllySpacing = 6000;
        private const int SideOffsetStep = 4500;
        private const int MoveDistanceScoreWeight = 15;
        private const int SideCrossingScorePenalty = PreferredAllySpacing * MoveDistanceScoreWeight * 2;
        private const int RelativeSideOrderScorePenalty = PreferredAllySpacing * MoveDistanceScoreWeight * 8;

        private static readonly int[] CandidateRangePercents = { 90, 85, 80 };
        private static readonly int[] SideOffsetIndices = { 0, 1, -1, 2, -2, 3, -3 };

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
            // 양 팀이 전열 한 명뿐이면 재배치할 대상이 없으며, 전열 교전 예측도 재귀할 필요가 없다.
            if (blueEntities.Count <= 1 && redEntities.Count <= 1)
                return false;

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

            placementOrder.Sort((first, second) => ComparePlacementOrder(
                first,
                second,
                frontlinePosition,
                enemyFrontlinePosition));

            var plannedDestinations = new List<PlannedDestination>();

            for (var i = 0; i < placementOrder.Count; i++)
            {
                var entity = placementOrder[i];
                if (!TryFindBestPosition(
                        entity,
                        frontlinePosition,
                        enemyFrontlinePosition,
                        reservedPositions,
                        plannedDestinations,
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
            List<PlannedDestination> plannedDestinations,
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

                for (var sideOffsetIndex = 0; sideOffsetIndex < SideOffsetIndices.Length; sideOffsetIndex++)
                {
                    var sideOffset = SideOffsetIndices[sideOffsetIndex] * SideOffsetStep;
                    if (Math.Abs(sideOffset) >= candidateRange)
                        continue;

                    var forwardDistance = MathHelper.IntSqrt(
                        (long)candidateRange * candidateRange - (long)sideOffset * sideOffset);
                    var candidate = CreateCandidatePosition(
                        enemyFrontlinePosition,
                        frontlineDelta,
                        frontlineDistance,
                        forwardDistance,
                        sideOffset).ToGridPos().ToFixedPos();

                    if (!IsValidCandidate(
                            entity,
                            candidate,
                            enemyFrontlinePosition,
                            safeAttackRange,
                            reservedPositions,
                            out var candidatePaths))
                        continue;

                    var score = EvaluateCandidate(
                        entity,
                        currentPosition,
                        candidate,
                        frontlinePosition,
                        enemyFrontlinePosition,
                        safeAttackRange,
                        reservedPositions,
                        plannedDestinations);

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

            for (var i = 0; i < reservedPositions.Count; i++)
            {
                if (candidate.GetDistance(reservedPositions[i]) < MinimumAllySpacing)
                    return false;
            }

            var candidatePaths = new List<GridPos>();
            if (!_battleMapPathFinder.TryFindWaypointsBetweenAnyPositions(entity.GetPos().ToGridPos(), gridPosition, candidatePaths))
                return false;

            paths = candidatePaths;
            return true;
        }

        private long EvaluateCandidate(
            Entity entity,
            FixedPos currentPosition,
            FixedPos candidate,
            FixedPos frontlinePosition,
            FixedPos enemyFrontlinePosition,
            long safeAttackRange,
            List<FixedPos> reservedPositions,
            List<PlannedDestination> plannedDestinations)
        {
            var enemyDistance = candidate.GetDistance(enemyFrontlinePosition);
            var attackRangeError = Math.Abs(safeAttackRange - enemyDistance);
            var nearestAllyDistance = GetNearestDistance(candidate, reservedPositions);
            var allySpacingError = Math.Abs(PreferredAllySpacing - nearestAllyDistance);
            var moveDistance = currentPosition.GetDistance(candidate);
            var centerGridZ = _battleMapData.minGridPos.y + _battleMapData.maxGridPos.y;
            var centerZ = centerGridZ * PositionConverter.FixedPosMultiplier / 2L;
            var centerDistance = Math.Abs(candidate.Z - centerZ);

            var frontlineDelta = enemyFrontlinePosition - frontlinePosition;
            var frontlineDistance = frontlinePosition.GetDistance(enemyFrontlinePosition);
            var candidateDelta = candidate - frontlinePosition;
            var forwardDot = candidateDelta.X * frontlineDelta.X + candidateDelta.Z * frontlineDelta.Z;
            var forwardProjection = forwardDot / Math.Max(1, frontlineDistance);
            var excessiveAdvance = Math.Max(0, forwardProjection);
            var currentSideOffset = GetSignedSideOffset(currentPosition, frontlinePosition, frontlineDelta, frontlineDistance);
            var candidateSideOffset = GetSignedSideOffset(candidate, frontlinePosition, frontlineDelta, frontlineDistance);
            var sideCrossingPenalty = currentSideOffset != 0 &&
                                      candidateSideOffset != 0 &&
                                      Math.Sign(currentSideOffset) != Math.Sign(candidateSideOffset)
                ? SideCrossingScorePenalty
                : 0;
            var orderInversionCount = GetRelativeSideOrderInversionCount(
                entity, candidate, frontlinePosition, enemyFrontlinePosition, plannedDestinations);
            var relativeSideOrderPenalty = orderInversionCount * RelativeSideOrderScorePenalty;

            return -attackRangeError * 100
                   - allySpacingError * 20
                   - excessiveAdvance * 200
                   - moveDistance * MoveDistanceScoreWeight
                   - centerDistance * 10
                   - sideCrossingPenalty
                   - relativeSideOrderPenalty;
        }

        private static int GetRelativeSideOrderInversionCount(
            Entity entity,
            FixedPos candidate,
            FixedPos frontlinePosition,
            FixedPos enemyFrontlinePosition,
            List<PlannedDestination> plannedDestinations)
        {
            var frontlineDelta = enemyFrontlinePosition - frontlinePosition;
            var frontlineDistance = frontlinePosition.GetDistance(enemyFrontlinePosition);
            if (frontlineDistance == 0)
                return plannedDestinations.Count;

            var currentSideOffset = GetSignedSideOffset(
                entity.GetPos(), frontlinePosition, frontlineDelta, frontlineDistance);
            var candidateSideOffset = GetSignedSideOffset(
                candidate, frontlinePosition, frontlineDelta, frontlineDistance);
            var inversionCount = 0;

            for (var i = 0; i < plannedDestinations.Count; i++)
            {
                var other = plannedDestinations[i];
                var otherCurrentSideOffset = GetSignedSideOffset(
                    other.Entity.GetPos(), frontlinePosition, frontlineDelta, frontlineDistance);
                var otherCandidateSideOffset = GetSignedSideOffset(
                    other.Destination, frontlinePosition, frontlineDelta, frontlineDistance);
                var currentOrder = currentSideOffset.CompareTo(otherCurrentSideOffset);
                var candidateOrder = candidateSideOffset.CompareTo(otherCandidateSideOffset);
                if (currentOrder != 0 && candidateOrder != 0 && Math.Sign(currentOrder) != Math.Sign(candidateOrder))
                    inversionCount++;
            }

            return inversionCount;
        }

        private static long GetSignedSideOffset(
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
            long sideOffset)
        {
            var forwardX = frontlineDelta.X * forwardDistance / frontlineDistance;
            var forwardZ = frontlineDelta.Z * forwardDistance / frontlineDistance;
            var sideOffsetX = -frontlineDelta.Z * sideOffset / frontlineDistance;
            var sideOffsetZ = frontlineDelta.X * sideOffset / frontlineDistance;

            return new FixedPos(
                enemyFrontlinePosition.X - forwardX + sideOffsetX,
                enemyFrontlinePosition.Y,
                enemyFrontlinePosition.Z - forwardZ + sideOffsetZ);
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

        internal static Entity GetFrontlineEntity(List<Entity> entities)
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

        private static int ComparePlacementOrder(
            Entity first,
            Entity second)
        {
            var rangeComparison = first.AttackRange.CompareTo(second.AttackRange);
            return rangeComparison != 0 ? rangeComparison : first.Id.CompareTo(second.Id);
        }

        private static int ComparePlacementOrder(
            Entity first,
            Entity second,
            FixedPos frontlinePosition,
            FixedPos enemyFrontlinePosition)
        {
            var rangeComparison = first.AttackRange.CompareTo(second.AttackRange);
            if (rangeComparison != 0)
                return rangeComparison;

            var frontlineDelta = enemyFrontlinePosition - frontlinePosition;
            var frontlineDistance = frontlinePosition.GetDistance(enemyFrontlinePosition);
            frontlineDistance = Math.Max(1, frontlineDistance);
            var firstSideOffset = Math.Abs(GetSignedSideOffset(
                first.GetPos(), frontlinePosition, frontlineDelta, frontlineDistance));
            var secondSideOffset = Math.Abs(GetSignedSideOffset(
                second.GetPos(), frontlinePosition, frontlineDelta, frontlineDistance));
            var sideOffsetComparison = firstSideOffset.CompareTo(secondSideOffset);
            return sideOffsetComparison != 0 ? sideOffsetComparison : first.Id.CompareTo(second.Id);
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

}
