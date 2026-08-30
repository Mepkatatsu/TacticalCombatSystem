using System.Collections.Generic;
using Script.CommonLib.Map;

namespace Script.CommonLib.Tests
{
    internal static class TacticalPositioningTestData
    {
        internal static bool HasAuthoredDestinations(IReadOnlyList<IEntityContext> entityContexts)
        {
            for (var i = 0; i < entityContexts.Count; i++)
            {
                var entity = (Entity)entityContexts[i];
                var expectedX = entity.GetTeamFlag() == TeamFlag.Blue ? 20000 : -20000;
                if (entity.GetDestinationForTest().X != expectedX)
                    return false;
            }

            return true;
        }

        internal static Dictionary<uint, FixedPos> GetDestinationsById(IReadOnlyList<IEntityContext> entityContexts)
        {
            var result = new Dictionary<uint, FixedPos>();

            for (var i = 0; i < entityContexts.Count; i++)
            {
                var entity = (Entity)entityContexts[i];
                result.Add(entity.Id, entity.GetDestinationForTest());
            }

            return result;
        }

        internal static bool HaveSameDestinations(
            Dictionary<uint, FixedPos> first,
            Dictionary<uint, FixedPos> second)
        {
            if (first.Count != second.Count)
                return false;

            foreach (var pair in first)
            {
                if (!second.TryGetValue(pair.Key, out var destination) || destination != pair.Value)
                    return false;
            }

            return true;
        }

        internal static List<Entity> GetEntities(IReadOnlyList<IEntityContext> entityContexts)
        {
            var entities = new List<Entity>();

            for (var i = 0; i < entityContexts.Count; i++)
            {
                entities.Add((Entity)entityContexts[i]);
            }

            return entities;
        }

        internal static BattleMapSimulator CreateSimulator()
        {
            return new BattleMapSimulator(
                NullBattleMapEventHandler.Instance,
                CreateMapData());
        }

        internal static bool AdvanceUntilFormationAttempted(BattleMapSimulator simulator)
        {
            for (var i = 0; i < 2000 && !simulator.WasInitialTacticalPositioningAttemptedForTest; i++)
            {
                simulator.Update(50);
            }

            return simulator.WasInitialTacticalPositioningAttemptedForTest;
        }

        internal static BattleMapData CreateFourEntityTeamMapData()
        {
            var battlePositions = new List<BattlePositionData>
            {
                CreateBattlePosition("BlueStart1", -10, 0),
                CreateBattlePosition("BlueStart2", -10, -6),
                CreateBattlePosition("BlueStart3", -10, 6),
                CreateBattlePosition("BlueStart4", -10, 10),
                CreateBattlePosition("RedStart1", 10, 0),
                CreateBattlePosition("RedStart2", 10, -6),
                CreateBattlePosition("RedStart3", 10, 6),
                CreateBattlePosition("RedStart4", 10, 10),
                CreateBattlePosition("BlueEnd1", 25, 0),
                CreateBattlePosition("BlueEnd2", 25, -6),
                CreateBattlePosition("BlueEnd3", 25, 6),
                CreateBattlePosition("BlueEnd4", 25, 10),
                CreateBattlePosition("RedEnd1", -25, 0),
                CreateBattlePosition("RedEnd2", -25, -6),
                CreateBattlePosition("RedEnd3", -25, 6),
                CreateBattlePosition("RedEnd4", -25, 10),
            };

            return new BattleMapData
            {
                minGridPos = new GridPos(-30, -20),
                maxGridPos = new GridPos(30, 20),
                battlePositions = battlePositions,
                obstacles = new List<ObstacleData>(),
                entities = new List<EntityData>
                {
                    CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 5000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart2", "BlueEnd2", 12000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart3", "BlueEnd3", 12000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart4", "BlueEnd4", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart1", "RedEnd1", 5000),
                    CreateEntityData(TeamFlag.Red, "RedStart2", "RedEnd2", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart3", "RedEnd3", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart4", "RedEnd4", 12000),
                },
            };
        }

        internal static BattleMapData CreateMapData()
        {
            var battlePositions = new List<BattlePositionData>
            {
                CreateBattlePosition("BlueStart1", -6, 0),
                CreateBattlePosition("BlueStart2", -6, -4),
                CreateBattlePosition("BlueStart3", -6, 4),
                CreateBattlePosition("RedStart1", 6, 0),
                CreateBattlePosition("RedStart2", 6, -4),
                CreateBattlePosition("RedStart3", 6, 4),
                CreateBattlePosition("BlueEnd1", 20, 0),
                CreateBattlePosition("BlueEnd2", 20, -4),
                CreateBattlePosition("BlueEnd3", 20, 4),
                CreateBattlePosition("RedEnd1", -20, 0),
                CreateBattlePosition("RedEnd2", -20, -4),
                CreateBattlePosition("RedEnd3", -20, 4),
            };

            return new BattleMapData
            {
                minGridPos = new GridPos(-30, -15),
                maxGridPos = new GridPos(30, 15),
                battlePositions = battlePositions,
                obstacles = new List<ObstacleData>(),
                entities = new List<EntityData>
                {
                    CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 5000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart2", "BlueEnd2", 12000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart3", "BlueEnd3", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart1", "RedEnd1", 5000),
                    CreateEntityData(TeamFlag.Red, "RedStart2", "RedEnd2", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart3", "RedEnd3", 12000),
                },
            };
        }

        internal static ObstacleData CreateCenterObstacle()
        {
            return new ObstacleData
            {
                blockedPoints = new List<GridPos>
                {
                    new(0, -1),
                    new(0, 0),
                    new(0, 1),
                },
                waypoints = new List<GridPos>
                {
                    new(-6, -3),
                    new(6, -3),
                    new(-6, 3),
                    new(6, 3),
                },
            };
        }

        private static BattlePositionData CreateBattlePosition(string name, int x, int y)
        {
            return new BattlePositionData
            {
                name = name,
                gridPos = new GridPos(x, y),
                positionType = BattlePositionData.PositionType.Waypoint,
            };
        }

        internal static EntityData CreateEntityData(
            TeamFlag teamFlag,
            string startPositionName,
            string endPositionName,
            ushort attackRange)
        {
            return new EntityData
            {
                teamFlag = teamFlag,
                name = startPositionName,
                startPositionName = startPositionName,
                endPositionName = endPositionName,
                maxHp = 100,
                attackDamage = 0,
                attackDelayMs = ushort.MaxValue,
                attackRange = attackRange,
                moveSpeed = 5000,
            };
        }

    }
}
