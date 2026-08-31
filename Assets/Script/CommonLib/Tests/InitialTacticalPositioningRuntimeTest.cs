using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TestResultVerifier;
using static Script.CommonLib.Tests.TacticalPositioningTestHelper;

namespace Script.CommonLib.Tests
{
    public sealed class InitialTacticalPositioningRuntimeTest : ITest
    {
        public bool Test()
        {
            return Verify<InitialTacticalPositioningRuntimeTest>(
                TestRuntimeSimulationCompletesPlannedMovement(),
                nameof(TestRuntimeSimulationCompletesPlannedMovement));
        }

        private static bool TestRuntimeSimulationCompletesPlannedMovement()
        {
            var json = File.ReadAllText("Assets/Data/MapData/TEST-001-NORMAL_Data.json");
            var mapData = JsonConvert.DeserializeObject<BattleMapData>(json);
            if (mapData == null)
                return false;

            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();

            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            var plannedEntities = new List<Entity>();
            var entities = GetEntities(simulator.GetAliveEntities());
            for (var i = 0; i < entities.Count; i++)
            {
                if (!entities[i].ShouldPrioritizeMovement)
                    continue;

                plannedEntities.Add(entities[i]);
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
                    entity.GetPos() != entity.GetDestinationForTest())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
