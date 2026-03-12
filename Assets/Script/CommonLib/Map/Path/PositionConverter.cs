namespace Script.CommonLib
{
    public static class PositionConverter
    {
        public const int FixedPosMultiplier = 1000;
        public const int FixedPosScale = FixedPosMultiplier / 2;
        
        public static FixedPos ToFixedPos(this GridPos gridPos)
        {
            return new FixedPos(gridPos.x * FixedPosMultiplier, 0, gridPos.y * FixedPosMultiplier);
        }

        public static GridPos ToGridPos(this FixedPos fixedPos)
        {
            int xScale = fixedPos.X >= 0 ? FixedPosScale : -FixedPosScale;
            int zScale = fixedPos.Z >= 0 ? FixedPosScale : -FixedPosScale;
            
            int x = (int)((fixedPos.X + xScale) / FixedPosMultiplier);
            int z = (int)((fixedPos.Z + zScale) / FixedPosMultiplier);
            
            return new GridPos(x, z);
        }
    }
}
