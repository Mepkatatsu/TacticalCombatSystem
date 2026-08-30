using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.CommonLib.Map;

namespace Script.CommonLib.Tests
{
    public partial class InitialTacticalFormationPlannerTest : ITest
    {
        public bool Test()
        {
            var success = true;

            // 기능의 활성화 시점과 실패 시 기존 상태 보존 계약을 검증한다.
            success &= Verify(TestInitDoesNotApplyFormation(), nameof(TestInitDoesNotApplyFormation));
            success &= Verify(TestEncounterDetectionMarginBoundary(), nameof(TestEncounterDetectionMarginBoundary));
            success &= Verify(TestLongRangeBacklineDoesNotStartEncounterEarly(), nameof(TestLongRangeBacklineDoesNotStartEncounterEarly));
            success &= Verify(TestFirstEncounterAppliesFormation(), nameof(TestFirstEncounterAppliesFormation));
            success &= Verify(TestFormationIsNotReappliedAfterFirstEncounter(), nameof(TestFormationIsNotReappliedAfterFirstEncounter));
            success &= Verify(TestPredictionFailureKeepsAuthoredDestinations(), nameof(TestPredictionFailureKeepsAuthoredDestinations));
            success &= Verify(TestPartialCandidateFailureKeepsWholeTeamDestinations(), nameof(TestPartialCandidateFailureKeepsWholeTeamDestinations));
            success &= Verify(TestFourEntityCandidateFailureKeepsWholeTeamDestinations(), nameof(TestFourEntityCandidateFailureKeepsWholeTeamDestinations));
            success &= Verify(TestPlacementOrderIsDeterministicWhenInputOrderChanges(), nameof(TestPlacementOrderIsDeterministicWhenInputOrderChanges));

            // 이동 우선순위와 기존 경로 복귀의 상태 전이 회귀를 검증한다.
            success &= Verify(TestPlannedEntityMovesEvenWhenEnemyIsInRange(), nameof(TestPlannedEntityMovesEvenWhenEnemyIsInRange));
            success &= Verify(TestTacticalMovementReachesDestinationAfterCurrentTargetDeathAndDamageTaken(), nameof(TestTacticalMovementReachesDestinationAfterCurrentTargetDeathAndDamageTaken));
            success &= Verify(TestImmobilePlannedEntityReleasesMovementPriority(), nameof(TestImmobilePlannedEntityReleasesMovementPriority));
            success &= Verify(TestSubStepTacticalMovementReleasesMovementPriority(), nameof(TestSubStepTacticalMovementReleasesMovementPriority));
            success &= Verify(TestExhaustedTacticalPathReleasesMovementPriority(), nameof(TestExhaustedTacticalPathReleasesMovementPriority));
            success &= Verify(TestFailedMovementPathStopsAndIsNotRetried(), nameof(TestFailedMovementPathStopsAndIsNotRetried));
            success &= Verify(TestUnplannedEntityKeepsAttackPriority(), nameof(TestUnplannedEntityKeepsAttackPriority));
            success &= Verify(TestEntityResumesAuthoredDestinationAfterExecutedAttack(), nameof(TestEntityResumesAuthoredDestinationAfterExecutedAttack));
            success &= Verify(TestEntityRequestsSmoothingWhenResumingAuthoredDestination(), nameof(TestEntityRequestsSmoothingWhenResumingAuthoredDestination));
            success &= Verify(TestEntityResumesWhenTargetLeavesBeforeExecutingAttack(), nameof(TestEntityResumesWhenTargetLeavesBeforeExecutingAttack));
            success &= Verify(TestFailedAuthoredDestinationResumeIsAttemptedOnce(), nameof(TestFailedAuthoredDestinationResumeIsAttemptedOnce));

            // 경로 탐색과 전환은 도달 가능성과 실패 시 상태 보존을 계약으로 삼는다.
            success &= Verify(TestArbitraryGoalUsesAuthoredWaypointDetour(), nameof(TestArbitraryGoalUsesAuthoredWaypointDetour));
            success &= Verify(TestSmoothPathTransitionSplitsCornerDeterministically(), nameof(TestSmoothPathTransitionSplitsCornerDeterministically));
            success &= Verify(TestSmoothPathTransitionKeepsOriginalPathWhenBlendIsBlocked(), nameof(TestSmoothPathTransitionKeepsOriginalPathWhenBlendIsBlocked));
            success &= Verify(TestSmoothPathTransitionKeepsUTurns(), nameof(TestSmoothPathTransitionKeepsUTurns));
            success &= Verify(TestSmoothPathTransitionReducesInternalCorner(), nameof(TestSmoothPathTransitionReducesInternalCorner));
            success &= Verify(TestIntegerAngleOrderingBoundaries(), nameof(TestIntegerAngleOrderingBoundaries));
            success &= Verify(TestEntityRequestsSmoothingForTacticalDestinationAfterMovement(), nameof(TestEntityRequestsSmoothingForTacticalDestinationAfterMovement));
            success &= Verify(TestExistingFindWaypointsResultIsPreserved(), nameof(TestExistingFindWaypointsResultIsPreserved));
            success &= Verify(TestFailedPathReturnsEmptyResult(), nameof(TestFailedPathReturnsEmptyResult));

            // 장애물 없는 대표 입력의 품질 회귀이며 모든 맵의 절대 조건을 의미하지 않는다.
            success &= Verify(TestUnequalRangePredictionMatchesFixedTickReference(), nameof(TestUnequalRangePredictionMatchesFixedTickReference));
            success &= Verify(TestOpenMapPlacementMatchesCurrentSafeAttackPolicy(), nameof(TestOpenMapPlacementMatchesCurrentSafeAttackPolicy));
            success &= Verify(TestOpenMapFourEntityPlacementMatchesCurrentSpacingPolicy(), nameof(TestOpenMapFourEntityPlacementMatchesCurrentSpacingPolicy));
            success &= Verify(TestPlacementPreservesCurrentLateralSide(), nameof(TestPlacementPreservesCurrentLateralSide));
            success &= Verify(TestPlacementPreservesRelativeLateralOrder(), nameof(TestPlacementPreservesRelativeLateralOrder));
            success &= Verify(TestRedPlacementPreservesCurrentLateralSide(), nameof(TestRedPlacementPreservesCurrentLateralSide));
            success &= Verify(TestDiagonalPlacementPreservesCurrentLateralSide(), nameof(TestDiagonalPlacementPreservesCurrentLateralSide));
            success &= Verify(TestBlueAndRedPlacementIsSymmetric(), nameof(TestBlueAndRedPlacementIsSymmetric));

            // 실제 맵에서는 적용과 이동 완료만 기본 회귀로 확인한다. 자연스러움은 이 테스트의 성공 조건으로 삼지 않는다.
            success &= Verify(TestTest001RuntimeSimulationCompletesPlannedMovement(), nameof(TestTest001RuntimeSimulationCompletesPlannedMovement));
            return success;
        }

        private static bool Verify(bool result, string testName)
        {
            if (!result)
                LogHelper.Error($"[InitialTacticalFormationPlannerTest] {testName} failed.");

            return result;
        }
    }
}
