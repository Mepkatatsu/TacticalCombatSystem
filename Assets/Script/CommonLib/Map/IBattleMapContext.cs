namespace Script.CommonLib.Map
{
    public interface IBattleMapContext
    {
        public IEntityContext GetNearestEnemy(uint entityId, float maxDistance);
    }
}
