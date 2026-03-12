using Script.CommonLib;
using UnityEngine;

namespace Script.ClientLib
{
    public static class ClientPositionConverter
    {
        public static Vector3 ToVector3(this FixedPos fixedPos)
        {
            return new Vector3(
                (float)fixedPos.X / PositionConverter.FixedPosMultiplier,
                (float)fixedPos.Y / PositionConverter.FixedPosMultiplier,
                (float)fixedPos.Z / PositionConverter.FixedPosMultiplier);
        }
        
        public static Vector3 ToDirection(this FixedDir fixedDir)
        {
            var delta = new Vector3(
                fixedDir.targetFixedPos.X - fixedDir.currentFixedPos.X,
                fixedDir.targetFixedPos.Y - fixedDir.currentFixedPos.Y,
                fixedDir.targetFixedPos.Z - fixedDir.currentFixedPos.Z);

            return delta.normalized;
        }
    }
}
