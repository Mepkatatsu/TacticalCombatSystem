using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.CommonLib.Map;

namespace Script.CommonLib.Tests
{
    public partial class InitialTacticalFormationPlannerTest
    {
        private static bool TestPlannedEntityMovesEvenWhenEnemyIsInRange()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());

            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            return HasEntityPrioritizingMovementInAttackRange(entities, TeamFlag.Blue) &&
                   HasEntityPrioritizingMovementInAttackRange(entities, TeamFlag.Red);
        }

        private static bool TestTacticalMovementReachesDestinationAfterCurrentTargetDeathAndDamageTaken()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            simulator.Update(50);
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueRanged = entities[1];
            var destination = blueRanged.GetDestinationForTest();
            var currentTargetId = blueRanged.GetMainTargetIdForTest();
            if (!currentTargetId.HasValue)
                return false;

            Entity currentTarget = null;
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].Id == currentTargetId.Value)
                {
                    currentTarget = entities[i];
                    break;
                }
            }

            if (currentTarget == null)
                return false;

            currentTarget.Hit(currentTarget.MaxHp);
            blueRanged.Hit(1);

            for (var i = 0; i < 500 && blueRanged.ShouldPrioritizeMovement; i++)
            {
                simulator.Update(50);
            }

            return blueRanged.IsAlive() &&
                   !currentTarget.IsAlive() &&
                   !blueRanged.ShouldPrioritizeMovement &&
                   blueRanged.GetPos() == destination;
        }

        private static bool TestUnplannedEntityKeepsAttackPriority()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            var blueFrontline = entities[0];

            for (var i = 0; i < 30; i++)
            {
                simulator.Update(50);
            }

            return !blueFrontline.ShouldPrioritizeMovement &&
                   blueFrontline.CurrentStateType == Script.CommonLib.Battle.EntityStateType.Attack;
        }

        private static bool TestImmobilePlannedEntityReleasesMovementPriority()
        {
            return TestInvalidMovementReleasesPriority(0);
        }

        private static bool TestSubStepTacticalMovementReleasesMovementPriority()
        {
            return TestInvalidMovementReleasesPriority(1);
        }

        private static bool TestInvalidMovementReleasesPriority(ushort moveSpeed)
        {
            var mapData = CreateMapData();
            mapData.entities[1].moveSpeed = moveSpeed;
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            var blueRanged = (Entity)simulator.GetAliveEntities()[1];

            for (var i = 0; i < 20 && blueRanged.ShouldPrioritizeMovement; i++)
            {
                simulator.Update(50);
            }

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   !blueRanged.ShouldPrioritizeMovement;
        }

        private static bool TestExhaustedTacticalPathReleasesMovementPriority()
        {
            var context = new ResumePathTestContext(false);
            var entity = new Entity(
                1,
                context,
                CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 12000));
            context.AddEntity(entity);
            var currentPosition = new GridPos(-6, 0).ToFixedPos();
            entity.SetPos(currentPosition);
            entity.SetDestination(new GridPos(20, 0).ToFixedPos());
            var unreachableDestination = new FixedPos(
                currentPosition.X + 10000,
                currentPosition.Y,
                currentPosition.Z);
            entity.SetTacticalDestination(
                unreachableDestination,
                new List<GridPos> { currentPosition.ToGridPos() });

            context.Update(entity, 50);

            return entity.GetPos() == currentPosition &&
                   !entity.ShouldPrioritizeMovement;
        }

        private static bool TestFailedMovementPathStopsAndIsNotRetried()
        {
            var context = new ResumePathTestContext(false, false);
            var entity = new Entity(
                1,
                context,
                CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 12000));
            context.AddEntity(entity);
            var start = new GridPos(-6, 0).ToFixedPos();
            entity.SetPos(start);
            entity.SetDestination(new GridPos(20, 0).ToFixedPos());

            context.Update(entity, 50);
            context.Update(entity, 50);
            context.Update(entity, 50);

            return entity.GetPos() == start &&
                   entity.CurrentStateType == Script.CommonLib.Battle.EntityStateType.Idle &&
                   entity.HasPathSearchFailed &&
                   context.PathRequestCount == 1;
        }

        private static bool TestEntityResumesAuthoredDestinationAfterExecutedAttack()
        {
            var context = new ResumePathTestContext(true);
            var attackerData = CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000);
            attackerData.attackDelayMs = 0;
            var enemyData = CreateEntityData(TeamFlag.Red, string.Empty, string.Empty, 2000);
            var attacker = new Entity(1, context, attackerData);
            var enemy = new Entity(2, context, enemyData);
            context.AddEntity(attacker);
            context.AddEntity(enemy);
            attacker.SetPos(new GridPos(-1, 0));
            var authoredDestination = new GridPos(10, 0).ToFixedPos();
            attacker.SetDestination(authoredDestination);
            attacker.SetTacticalDestination(
                new GridPos(0, 0).ToFixedPos(),
                new List<GridPos> { new(0, 0) });
            enemy.SetPos(new GridPos(1, 0));
            enemy.SetDestination(enemy.GetPos());

            for (var i = 0; i < 20 && context.AttackRequestCount == 0; i++)
            {
                context.Update(attacker, 50);
            }

            if (context.AttackRequestCount == 0)
                return false;

            var tacticalPosition = attacker.GetPos();
            enemy.SetPos(new GridPos(8, 0));
            for (var i = 0; i < 20 && attacker.GetPos() == tacticalPosition; i++)
            {
                context.Update(attacker, 50);
            }

            return context.ResumePathRequestCount == 1 &&
                   attacker.GetDestinationForTest() == authoredDestination &&
                   attacker.GetPos() != tacticalPosition;
        }

        private static bool TestEntityRequestsSmoothingWhenResumingAuthoredDestination()
        {
            var context = new ResumePathTestContext(true);
            var attackerData = CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000);
            attackerData.attackDelayMs = 0;
            var attacker = new Entity(1, context, attackerData);
            var enemy = new Entity(
                2,
                context,
                CreateEntityData(TeamFlag.Red, string.Empty, string.Empty, 2000));
            context.AddEntity(attacker);
            context.AddEntity(enemy);
            attacker.SetPos(new GridPos(-1, 0));
            attacker.SetDestination(new GridPos(10, 10).ToFixedPos());
            attacker.SetTacticalDestination(
                new GridPos(0, 0).ToFixedPos(),
                new List<GridPos> { new(0, 0) });
            enemy.SetPos(new GridPos(1, 0));
            enemy.SetDestination(enemy.GetPos());

            for (var i = 0; i < 20 && context.AttackRequestCount == 0; i++)
                context.Update(attacker, 50);

            if (context.AttackRequestCount == 0)
                return false;

            enemy.SetPos(new GridPos(20, 20));
            for (var i = 0; i < 20 && context.ResumePathRequestCount == 0; i++)
                context.Update(attacker, 50);

            return context.ResumePathRequestCount == 1 &&
                   context.PathSmoothingCallCount == 1;
        }

        private static bool TestEntityResumesWhenTargetLeavesBeforeExecutingAttack()
        {
            var mapData = CreateMapData();
            var eventHandler = new AttackRecordingEventHandler();
            var simulator = new BattleMapSimulator(eventHandler, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueRanged = entities[1];
            var authoredDestination = blueRanged.GetDestinationForTest();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            for (var i = 0; i < 500 && blueRanged.ShouldPrioritizeMovement; i++)
            {
                simulator.Update(50);
            }

            for (var i = 0; i < 20 && !blueRanged.IsMainTargetInRange(); i++)
            {
                simulator.Update(50);
            }

            if (!blueRanged.IsMainTargetInRange() || eventHandler.HasAttackFrom(blueRanged.Id))
                return false;

            var redSurvivor = entities[5];
            for (var i = 3; i < 6; i++)
            {
                if (entities[i] != redSurvivor)
                    entities[i].Hit(entities[i].MaxHp);
            }

            var survivorPosition = new GridPos(28, 0).ToFixedPos();
            redSurvivor.SetPos(survivorPosition);
            redSurvivor.SetDestination(survivorPosition);
            for (var i = 0; i < 20; i++)
            {
                simulator.Update(50);
            }

            return !eventHandler.HasAttackFrom(blueRanged.Id) &&
                   blueRanged.GetDestinationForTest() == authoredDestination;
        }

        private static bool TestFailedAuthoredDestinationResumeIsAttemptedOnce()
        {
            var context = new ResumePathTestContext(false);
            var attackerData = CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000);
            attackerData.attackDelayMs = 0;
            var enemyData = CreateEntityData(TeamFlag.Red, string.Empty, string.Empty, 2000);
            var attacker = new Entity(1, context, attackerData);
            var enemy = new Entity(2, context, enemyData);
            context.AddEntity(attacker);
            context.AddEntity(enemy);
            attacker.SetPos(new GridPos(-1, 0));
            attacker.SetDestination(new GridPos(10, 0).ToFixedPos());
            attacker.SetTacticalDestination(
                new GridPos(0, 0).ToFixedPos(),
                new List<GridPos> { new(0, 0) });
            enemy.SetPos(new GridPos(10, 0));
            enemy.SetDestination(enemy.GetPos());

            for (var i = 0; i < 20 && attacker.ShouldPrioritizeMovement; i++)
            {
                context.Update(attacker, 50);
            }

            enemy.SetPos(new GridPos(1, 0));
            for (var i = 0; i < 20 && context.AttackRequestCount == 0; i++)
            {
                context.Update(attacker, 50);
            }

            if (context.AttackRequestCount == 0)
                return false;

            enemy.SetPos(new GridPos(8, 0));
            for (var i = 0; i < 20; i++)
            {
                context.Update(attacker, 50);
            }

            return context.ResumePathRequestCount == 1 &&
                   attacker.GetDestinationForTest() == new GridPos(0, 0).ToFixedPos();
        }

        private static bool HasEntityPrioritizingMovementInAttackRange(
            List<Entity> entities,
            TeamFlag teamFlag)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].GetTeamFlag() == teamFlag &&
                    entities[i].ShouldPrioritizeMovement &&
                    entities[i].IsMainTargetInRange())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
