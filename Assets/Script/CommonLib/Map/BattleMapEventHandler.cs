namespace Script.CommonLib.Map
{
    public interface IBattleMapEventHandler
    {
        public void OnEntityAdded(uint entityId, Entity entity);
        public void OnEntityPositionChanged(uint entityId, Vec3 pos);
        public void OnEntityDirectionChanged(uint entityId, Vec3 pos);
        public void OnEntityStartMove(uint entityId);
        public void OnEntityStopMove(uint entityId);
        public void OnEntityStartAttack(uint attackerId, uint targetId);
        public void OnEntityRetired(uint entityId);
        
        public void OnProjectileAdded(ulong projectileId, Projectile projectile);
        public void OnProjectilePositionChanged(ulong projectileId, Vec3 pos);
        public void OnProjectileDirectionChanged(ulong projectileId, Vec3 dir);
        public void OnProjectileTriggered(ulong projectileId);
        
        public void OnBattleEnd(TeamFlag winner);
    }
}
