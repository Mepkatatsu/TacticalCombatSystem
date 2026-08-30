using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.CommonLib.Map;

namespace Script.CommonLib.Tests
{
    public partial class InitialTacticalFormationPlannerTest
    {
        private static bool TestTest001RuntimeSimulationCompletesPlannedMovement()
        {
            var json = File.ReadAllText("Assets/Data/MapData/TEST-001-NORMAL_Data.json");
            var mapData = JsonConvert.DeserializeObject<BattleMapData>(json);
            if (mapData == null)
                return false;

            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var authoredDestinations = GetDestinationsById(simulator.GetAliveEntities());

            for (var i = 0; i < 2000 && !simulator.WasInitialTacticalPositioningAttemptedForTest; i++)
            {
                simulator.Update(50);
            }

            if (!simulator.WasInitialTacticalPositioningAttemptedForTest)
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
    }
}
