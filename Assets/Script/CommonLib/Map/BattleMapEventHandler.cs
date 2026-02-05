namespace Script.CommonLib.Map
{
    public interface IBattleMapEventHandler
    {
        public void OnEntityAdded(Entity entity);
        public void OnEntityPositionChanged(Entity entity, Vector3 pos);
        public void OnEntityDirectionChanged(Entity entity, Vector3 pos);
        public void OnEntityStartMoving(Entity entity); // TODO: 상태 전이 함수로 바꿔야 함
        public void OnEntityStopMoving(Entity entity); // TODO: 상태 전이 함수로 바꿔야 함
    }
}
