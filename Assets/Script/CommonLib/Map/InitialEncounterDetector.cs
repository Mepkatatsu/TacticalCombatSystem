using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    internal static class InitialEncounterDetector
    {
        private const int FrontlineDetectionMargin = 15000;

        public static bool HasEncounter(List<Entity> blueEntities, List<Entity> redEntities)
        {
            var blueFrontline = InitialTacticalFormationPlanner.GetFrontlineEntity(blueEntities);
            var redFrontline = InitialTacticalFormationPlanner.GetFrontlineEntity(redEntities);
            if (blueFrontline == null || redFrontline == null)
                return false;

            var distance = blueFrontline.GetPos().GetDistance(redFrontline.GetPos());
            return distance <= blueFrontline.AttackRange + FrontlineDetectionMargin ||
                   distance <= redFrontline.AttackRange + FrontlineDetectionMargin;
        }
    }
}
