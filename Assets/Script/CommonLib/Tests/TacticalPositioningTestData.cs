using System.Collections.Generic;
using Script.CommonLib.Map;

namespace Script.CommonLib.Tests
{
    internal static class TacticalPositioningTestData
    {
        internal static bool Verify<TTest>(bool result, string testName)
        {
            if (!result)
                LogHelper.Error($"[{typeof(TTest).Name}] {testName} failed.");

            return result;
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
            return CreateMapData(new[] { 0, -6, 6, 10 }, 10, 25, 20);
        }

        internal static BattleMapData CreateMapData()
        {
            return CreateMapData(new[] { 0, -4, 4 }, 6, 20, 15);
        }

        private static BattleMapData CreateMapData(
            int[] lateralPositions,
            int startX,
            int destinationX,
            int mapHalfHeight)
        {
            var battlePositions = new List<BattlePositionData>();
            AddBattlePositions(battlePositions, "BlueStart", -startX, lateralPositions);
            AddBattlePositions(battlePositions, "RedStart", startX, lateralPositions);
            AddBattlePositions(battlePositions, "BlueEnd", destinationX, lateralPositions);
            AddBattlePositions(battlePositions, "RedEnd", -destinationX, lateralPositions);

            var entities = new List<EntityData>();
            AddTeamEntities(entities, TeamFlag.Blue, lateralPositions.Length);
            AddTeamEntities(entities, TeamFlag.Red, lateralPositions.Length);
            return new BattleMapData
            {
                minGridPos = new GridPos(-30, -mapHalfHeight),
                maxGridPos = new GridPos(30, mapHalfHeight),
                battlePositions = battlePositions,
                obstacles = new List<ObstacleData>(),
                entities = entities,
            };
        }

        private static void AddBattlePositions(
            List<BattlePositionData> battlePositions,
            string namePrefix,
            int x,
            int[] lateralPositions)
        {
            for (var i = 0; i < lateralPositions.Length; i++)
            {
                battlePositions.Add(CreateBattlePosition(
                    $"{namePrefix}{i + 1}",
                    x,
                    lateralPositions[i]));
            }
        }

        private static void AddTeamEntities(
            List<EntityData> entities,
            TeamFlag teamFlag,
            int entityCount)
        {
            var teamName = teamFlag == TeamFlag.Blue ? "Blue" : "Red";
            for (var i = 0; i < entityCount; i++)
            {
                entities.Add(CreateEntityData(
                    teamFlag,
                    $"{teamName}Start{i + 1}",
                    $"{teamName}End{i + 1}",
                    i == 0 ? (ushort)5000 : (ushort)12000));
            }
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
