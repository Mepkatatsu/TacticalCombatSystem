using System.Collections.Generic;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TestResultVerifier;
using static Script.CommonLib.Tests.TacticalPositioningTestHelper;

namespace Script.CommonLib.Tests
{
    public sealed class InitialTacticalFormationPlannerPlacementTest : ITest
    {
        public bool Test()
        {
            var success = true;

            success &= Verify<InitialTacticalFormationPlannerPlacementTest>(TestDiagonalPlacementCreatesValidFormation(),
                nameof(TestDiagonalPlacementCreatesValidFormation));
            success &= Verify<InitialTacticalFormationPlannerPlacementTest>(
                TestPlacementOrderIsDeterministicWhenInputOrderChanges(),
                nameof(TestPlacementOrderIsDeterministicWhenInputOrderChanges));
            return success;
        }

        private static bool TestDiagonalPlacementCreatesValidFormation()
        {
            var mapData = CreateMapData();
            SetDiagonalStartPositions(mapData);
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();

            var entities = GetEntities(simulator.GetAliveEntities());
            var blueEntities = new List<Entity> { entities[0], entities[1], entities[2] };
            var redEntities = new List<Entity> { entities[3], entities[4], entities[5] };
            var blueFrontline = InitialTacticalFormationPlanner.GetFrontlineEntity(blueEntities);
            var redFrontline = InitialTacticalFormationPlanner.GetFrontlineEntity(redEntities);
            if (blueFrontline == null || redFrontline == null)
                return false;

            var starts = GetPositionsById(entities);
            var authoredDestinations = GetDestinationsById(simulator.GetAliveEntities());
            var predictor = new FrontlineEncounterPredictor(mapData);
            if (!predictor.TryPredict(
                    blueFrontline, redFrontline, out var blueFrontlinePosition, out var redFrontlinePosition))
                return false;

            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));
            if (!planner.TryApply(blueEntities, redEntities))
                return false;

            var formationPositions = new HashSet<FixedPos>();
            if (!formationPositions.Add(blueFrontlinePosition) || !formationPositions.Add(redFrontlinePosition))
                return false;

            return HasValidTeamFormation(
                       blueEntities, blueFrontline, blueFrontlinePosition, redFrontlinePosition,
                       starts, authoredDestinations, formationPositions) &&
                   HasValidTeamFormation(
                       redEntities, redFrontline, redFrontlinePosition, blueFrontlinePosition,
                       starts, authoredDestinations, formationPositions) &&
                   blueFrontline.GetDestinationForTest() == authoredDestinations[blueFrontline.Id] &&
                   redFrontline.GetDestinationForTest() == authoredDestinations[redFrontline.Id] &&
                   !blueFrontline.ShouldPrioritizeMovement &&
                   !redFrontline.ShouldPrioritizeMovement;
        }

        private static bool TestPlacementOrderIsDeterministicWhenInputOrderChanges()
        {
            var firstMapData = CreateMapData();
            var secondMapData = CreateMapData();
            var firstSimulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, firstMapData);
            var secondSimulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, secondMapData);
            firstSimulator.Init();
            secondSimulator.Init();

            var firstEntities = GetEntities(firstSimulator.GetAliveEntities());
            var secondEntities = GetEntities(secondSimulator.GetAliveEntities());
            var firstPlanner = new InitialTacticalFormationPlanner(firstMapData, new BattleMapPathFinder(firstMapData));
            var secondPlanner = new InitialTacticalFormationPlanner(secondMapData, new BattleMapPathFinder(secondMapData));

            if (!firstPlanner.TryApply(
                    new List<Entity> { firstEntities[0], firstEntities[1], firstEntities[2] },
                    new List<Entity> { firstEntities[3], firstEntities[4], firstEntities[5] }) ||
                !secondPlanner.TryApply(
                    new List<Entity> { secondEntities[2], secondEntities[0], secondEntities[1] },
                    new List<Entity> { secondEntities[5], secondEntities[3], secondEntities[4] }))
            {
                return false;
            }

            return HaveSameDestinations(GetDestinationsById(firstSimulator.GetAliveEntities()),
                GetDestinationsById(secondSimulator.GetAliveEntities()));
        }

        private static void SetDiagonalStartPositions(BattleMapData mapData)
        {
            mapData.battlePositions[0].gridPos = new GridPos(-8, -2);
            mapData.battlePositions[1].gridPos = new GridPos(-10, 4);
            mapData.battlePositions[2].gridPos = new GridPos(-6, -8);
            mapData.battlePositions[3].gridPos = new GridPos(8, 2);
            mapData.battlePositions[4].gridPos = new GridPos(10, -4);
            mapData.battlePositions[5].gridPos = new GridPos(6, 8);
        }

        private static Dictionary<uint, FixedPos> GetPositionsById(List<Entity> entities)
        {
            var positions = new Dictionary<uint, FixedPos>();
            for (var i = 0; i < entities.Count; i++)
            {
                positions.Add(entities[i].Id, entities[i].GetPos());
            }

            return positions;
        }

        private static bool HasValidTeamFormation(
            List<Entity> entities, Entity frontline,
            FixedPos frontlinePosition, FixedPos opposingFrontlinePosition,
            Dictionary<uint, FixedPos> starts, Dictionary<uint, FixedPos> authoredDestinations,
            HashSet<FixedPos> formationPositions)
        {
            var startProjections = new List<long>();
            var destinationProjections = new List<long>();
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity == frontline)
                    continue;

                var destination = entity.GetDestinationForTest();
                var startProjection = GetLateralProjection(starts[entity.Id], frontlinePosition, opposingFrontlinePosition);
                var destinationProjection = GetLateralProjection(destination, frontlinePosition, opposingFrontlinePosition);
                if (destination == authoredDestinations[entity.Id] ||
                    destination.GetDistance(opposingFrontlinePosition) > entity.AttackRange ||
                    startProjection == 0 ||
                    destinationProjection == 0 ||
                    (startProjection > 0) != (destinationProjection > 0) ||
                    !formationPositions.Add(destination))
                {
                    return false;
                }

                startProjections.Add(startProjection);
                destinationProjections.Add(destinationProjection);
            }

            for (var i = 0; i < startProjections.Count; i++)
            {
                for (var j = i + 1; j < startProjections.Count; j++)
                {
                    if (destinationProjections[i] == destinationProjections[j] ||
                        (startProjections[i] < startProjections[j]) !=
                        (destinationProjections[i] < destinationProjections[j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static long GetLateralProjection(FixedPos position, FixedPos frontline, FixedPos opposingFrontline)
        {
            var axis = opposingFrontline - frontline;
            var delta = position - frontline;
            return -delta.X * axis.Z + delta.Z * axis.X;
        }
    }
}
