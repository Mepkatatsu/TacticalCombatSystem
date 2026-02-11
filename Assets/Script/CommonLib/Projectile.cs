using System;
using Script.CommonLib.Map;

namespace Script.CommonLib
{
    public class Projectile
    {
        private IBattleMapContext _battleMapContext;
        
        public ulong Id { get; private set; }
        public IEntityContext Attacker { get; private set; }
        public IEntityContext Target { get; private set; }
        public float Damage { get; private set; }
        private readonly float _lifeTime;
        private float _leftLifeTime;

        private Vec3 _pos;
        private Vec3 _dir;
        
        private const float RotateSpeed = 10f;
        
        public Projectile(IBattleMapContext battleMapContext, ulong id, IEntityContext attacker, IEntityContext target, float damage, float lifeTime, Vec3 pos)
        {
            _battleMapContext = battleMapContext;

            Id = id;
            Attacker = attacker;
            Target = target;
            Damage = damage;
            _lifeTime = lifeTime;
            _leftLifeTime = lifeTime;

            if (_lifeTime <= 0)
            {
                LogHelper.Error("Projectile LifeTime is less or equal to 0");
            }
            
            _pos = pos;
            _dir = Target.GetPos() - Attacker.GetPos();
        }

        public void Update(float deltaTime)
        {
            if (_leftLifeTime <= 0)
                return;
            
            _leftLifeTime -= deltaTime;
            
            MovePos(deltaTime);

            if (_leftLifeTime <= 0)
                Trigger();
        }

        private void MovePos(float deltaTime)
        {
            var targetPos = Target.GetPos();
            var nextPos = targetPos;
            var dir = (nextPos - _pos).normalized;
            
            var lastLifeTime = _leftLifeTime + deltaTime;

            if (_leftLifeTime > 0 && lastLifeTime > 0)
            {
                var totalDistance = Vec3.Distance(targetPos, _pos);
                var ratio = deltaTime / lastLifeTime;
                ratio = Math.Min(ratio, 1);
                var moveDistance = totalDistance * ratio;
                nextPos = _pos + dir * moveDistance;
            }
            
            var nextDir = Vec3.Lerp(_dir, dir, RotateSpeed * deltaTime);
            
            _pos = nextPos;
            _dir = nextDir;
            
            _battleMapContext.OnProjectilePositionChanged(Id, _pos);
            _battleMapContext.OnProjectileDirectionChanged(Id, _dir);
        }

        private void Trigger()
        {
            _battleMapContext.OnProjectileTriggered(Id);
        }

        public Vec3 GetPos()
        {
            return _pos;
        }
    }
}
