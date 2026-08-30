using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TacticalPositioningTestData;

namespace Script.CommonLib.Tests
{
    public sealed class InitialTacticalPositioningRuntimeTest : ITest
    {
        public bool Test()
        {
            return Verify<InitialTacticalPositioningRuntimeTest>(
                TestRuntimeLifecycleAppliesOnceAndCompletesPlannedMovement(),
                nameof(TestRuntimeLifecycleAppliesOnceAndCompletesPlannedMovement));
        }

        private static bool TestRuntimeLifecycleAppliesOnceAndCompletesPlannedMovement()
        {
            return TestRuntimeSimulationCompletesPlannedMovement() &&
                   TestFormationIsNotReappliedAfterFirstEncounter();
        }

        private static bool TestRuntimeSimulationCompletesPlannedMovement()
        {
            var json = File.ReadAllText("Assets/Data/MapData/TEST-001-NORMAL_Data.json");
            var mapData = JsonConvert.DeserializeObject<BattleMapData>(json);
            if (mapData == null)
                return false;

            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            if (simulator.WasInitialTacticalPositioningAttemptedForTest)
                return false;

            var authoredDestinations = GetDestinationsById(simulator.GetAliveEntities());

            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            var plannedDestinations = GetDestinationsById(simulator.GetAliveEntities());
            if (HaveSameDestinations(authoredDestinations, plannedDestinations))
                return false;

            var plannedEntities = new List<Entity>();
            var positionsAfterPlanning = new Dictionary<uint, FixedPos>();
            var entities = GetEntities(simulator.GetAliveEntities());
            for (var i = 0; i < entities.Count; i++)
            {
                if (!entities[i].ShouldPrioritizeMovement)
                    continue;

                plannedEntities.Add(entities[i]);
                positionsAfterPlanning.Add(entities[i].Id, entities[i].GetPos());
            }

            if (plannedEntities.Count == 0)
                return false;

            for (var tick = 0; tick < 2000; tick++)
            {
                var hasMovingEntity = false;
                for (var i = 0; i < plannedEntities.Count; i++)
                {
                    if (plannedEntities[i].ShouldPrioritizeMovement)
                    {
                        hasMovingEntity = true;
                        break;
                    }
                }

                if (!hasMovingEntity)
                    break;

                simulator.Update(50);
            }

            for (var i = 0; i < plannedEntities.Count; i++)
            {
                var entity = plannedEntities[i];
                if (entity.ShouldPrioritizeMovement ||
                    entity.GetPos() == positionsAfterPlanning[entity.Id] ||
                    entity.GetPos() != entity.GetDestinationForTest())
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TestFormationIsNotReappliedAfterFirstEncounter()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            var entities = GetEntities(simulator.GetAliveEntities());
            Entity plannedEntity = null;
            for (var i = 0; i < entities.Count; i++)
            {
                if (!entities[i].ShouldPrioritizeMovement)
                    continue;

                plannedEntity = entities[i];
                break;
            }

            if (plannedEntity == null)
                return false;

            // 첫 배치 결과를 덮어쓴 뒤에도 같은 교전에서 다시 계획하지 않아야 한다.
            var overrideDestination = plannedEntity.GetPos();
            plannedEntity.SetDestination(overrideDestination);
            simulator.Update(0);

            return plannedEntity.GetDestinationForTest() == overrideDestination;
        }

    }
}
