namespace Script.CommonLib.Map
{
    public interface IBattleMapEventHandler
    {
        public void OnEntityAdded(Entity entity);
        public void OnEntityPositionChanged(Entity entity, Vector3 pos);
        public void OnEntityDirectionChanged(Entity entity, Vector3 pos);
    }
}
