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
        public uint Damage { get; private set; }
        private uint _leftLifeMs;

        private Vec3 _pos;
        private Vec3 _dir;
        
        private const float RotateSpeed = 10f;
        
        public Projectile(IBattleMapContext battleMapContext, ulong id, IEntityContext attacker, IEntityContext target, uint damage, uint lifeMs, Vec3 pos)
        {
            _battleMapContext = battleMapContext;

            Id = id;
            Attacker = attacker;
            Target = target;
            Damage = damage;
            _leftLifeMs = lifeMs;

            if (lifeMs == 0)
            {
                LogHelper.Error("Projectile LifeTime must be greater than 0");
            }
            
            _pos = pos;
            _dir = Target.GetPos() - Attacker.GetPos();
        }

        public void Update(ushort deltaMs)
        {
            if (_leftLifeMs <= 0)
                return;
            
            _leftLifeMs -= deltaMs;
            
            MovePos(deltaMs);

            if (_leftLifeMs <= 0)
                Trigger();
        }

        private void MovePos(ushort deltaMs)
        {
            var targetPos = Target.GetPos();
            var nextPos = targetPos;
            var dir = (nextPos - _pos).normalized;
            
            var lastLifeMs = _leftLifeMs + deltaMs;

            if (_leftLifeMs > 0 && lastLifeMs > 0)
            {
                var totalDistance = Vec3.Distance(targetPos, _pos);
                var ratio = (float)deltaMs / lastLifeMs; // TODO: 부동 소수점 오차를 고려해 이동 방식 변경
                ratio = Math.Min(ratio, 1);
                var moveDistance = totalDistance * ratio;
                nextPos = _pos + dir * moveDistance;
            }
            
            var nextDir = Vec3.Lerp(_dir, dir, RotateSpeed * deltaMs / 1000f); // TODO: 부동 소수점 오차를 고려해 이동 방식 변경
            
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
