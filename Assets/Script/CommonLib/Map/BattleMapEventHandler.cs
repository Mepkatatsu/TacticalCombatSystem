namespace Script.CommonLib.Map
{
    public interface IBattleMapEventHandler
    {
        public void OnEntityAdded(uint entityId, Entity entity);
        public void OnEntityPositionChanged(uint entityId, Vec3 pos);
        public void OnEntityDirectionChanged(uint entityId, Vec3 pos);
        public void OnEntityStartMoving(uint entityId);
        public void OnEntityStopMoving(uint entityId);
    }
}
