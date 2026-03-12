namespace Script.CommonLib.Map
{
    public interface IBattleMapEventHandler
    {
        public void OnEntityAdded(uint entityId, Entity entity);
        public void OnEntityPositionChanged(uint entityId, FixedPos pos);
        public void OnEntityDirectionChanged(uint entityId, FixedDir dir);
        public void OnEntityStartMove(uint entityId);
        public void OnEntityStopMove(uint entityId);
        public void OnEntityStartAttack(uint attackerId, uint targetId);
        public void OnEntityGetDamage(uint entityId, uint damage);
        public void OnEntityRetired(uint entityId);
        
        public void OnProjectileAdded(ulong projectileId, Projectile projectile);
        public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos);
        public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir);
        public void OnProjectileTriggered(ulong projectileId);
        
        public void OnBattleEnd(TeamFlag winner);
        public void OnBattleMapUpdated(ushort deltaMs);
    }
}
