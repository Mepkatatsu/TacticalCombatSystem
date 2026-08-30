using System.Collections.Generic;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TacticalPositioningTestData;

namespace Script.CommonLib.Tests
{
    public sealed class InitialTacticalPositioningFlowTest : ITest
    {
        public bool Test()
        {
            var success = true;

            success &= Verify<InitialTacticalPositioningFlowTest>(
                TestLongRangeBacklineDoesNotStartEncounterEarly(),
                nameof(TestLongRangeBacklineDoesNotStartEncounterEarly));
            success &= Verify<InitialTacticalPositioningFlowTest>(
                TestUnequalRangePredictionReachesMutualAttackRange(),
                nameof(TestUnequalRangePredictionReachesMutualAttackRange));
            success &= Verify<InitialTacticalPositioningFlowTest>(
                TestPredictionFailureKeepsAuthoredDestinations(),
                nameof(TestPredictionFailureKeepsAuthoredDestinations));
            return success;
        }

        private static bool TestLongRangeBacklineDoesNotStartEncounterEarly()
        {
            var mapData = CreateMapData();
            mapData.entities[0].attackRange = 3000;
            mapData.entities[1].attackRange = 20000;
            mapData.entities[3].attackRange = 3000;
            mapData.entities[4].attackRange = 20000;
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            entities[0].SetPos(new FixedPos(0, 0, 0));
            entities[1].SetPos(new FixedPos(0, 0, 0));
            entities[3].SetPos(new FixedPos(20000, 0, 0));
            entities[4].SetPos(new FixedPos(20000, 0, 0));

            var blueEntities = new List<Entity> { entities[0], entities[1], entities[2] };
            var redEntities = new List<Entity> { entities[3], entities[4], entities[5] };
            return !InitialEncounterDetector.HasEncounter(blueEntities, redEntities) &&
                   !InitialEncounterDetector.HasEncounter(redEntities, blueEntities);
        }

        private static bool TestUnequalRangePredictionReachesMutualAttackRange()
        {
            var mapData = CreateMapData();
            mapData.entities = new List<EntityData>
            {
                CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 10000),
                CreateEntityData(TeamFlag.Red, "RedStart1", "RedEnd1", 3000),
            };
            mapData.battlePositions[0].gridPos = new GridPos(-10, 0);
            mapData.battlePositions[3].gridPos = new GridPos(10, 0);

            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueStart = entities[0].GetPos();
            var redStart = entities[1].GetPos();
            var predictor = new FrontlineEncounterPredictor(mapData);

            if (!predictor.TryPredict(
                    entities[0],
                    entities[1],
                    out var bluePosition,
                    out var redPosition))
            {
                return false;
            }

            var blueMoveDistance = blueStart.GetDistance(bluePosition);
            var redMoveDistance = redStart.GetDistance(redPosition);
            return blueMoveDistance < redMoveDistance &&
                   bluePosition.GetDistance(redPosition) <= entities[1].AttackRange;
        }

        private static bool TestPredictionFailureKeepsAuthoredDestinations()
        {
            var mapData = CreateMapData();
            for (var i = 0; i < mapData.entities.Count; i++)
            {
                mapData.entities[i].moveSpeed = 0;
            }

            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var authoredDestinations = GetDestinationsById(simulator.GetAliveEntities());
            var entities = GetEntities(simulator.GetAliveEntities());
            entities[0].SetPos(new GridPos(-3, 0));
            entities[3].SetPos(new GridPos(3, 0));
            simulator.Update(50);

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   HaveSameDestinations(
                       authoredDestinations,
                       GetDestinationsById(simulator.GetAliveEntities()));
        }

    }
}
