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
        
        private FixedPos _currentPos;
        private FixedDir _dir;

        private bool _hasTriggered;
        
        public Projectile(IBattleMapContext battleMapContext, ulong id, IEntityContext attacker, IEntityContext target, uint damage, uint lifeMs, FixedPos startPos)
        {
            _battleMapContext = battleMapContext;

            Id = id;
            Attacker = attacker;
            Target = target;
            Damage = damage;
            _leftLifeMs = lifeMs;
            
            _currentPos = startPos;
            _dir = new FixedDir(Attacker.GetPos(), Target.GetPos());
        }

        public void Update(ushort deltaMs)
        {
            if (_hasTriggered)
                return;

            if (_leftLifeMs <= deltaMs)
            {
                deltaMs = (ushort)_leftLifeMs;
                _leftLifeMs = 0;
            }
            else
            {
                _leftLifeMs -= deltaMs;
            }
            
            MovePos(deltaMs);

            if (_leftLifeMs == 0)
            {
                Trigger();
            }
        }

        private void MovePos(ushort deltaMs)
        {
            if (deltaMs == 0)
                return;
            
            var targetPos = Target.GetPos();
            var nextPos = targetPos;
            
            var prevLifeMs = _leftLifeMs + deltaMs;

            if (_leftLifeMs > 0)
            {
                var delta = targetPos - _currentPos;
                var move = delta * deltaMs / (int)prevLifeMs; // TODO: 반올림/최솟값 처리 고려
                nextPos = _currentPos + move;
            }
            
            _dir = new FixedDir(_currentPos, nextPos);
            _currentPos = nextPos;
            
            _battleMapContext.OnProjectilePositionChanged(Id, _currentPos);
            _battleMapContext.OnProjectileDirectionChanged(Id, _dir);
        }

        private void Trigger()
        {
            _hasTriggered = true;
            _battleMapContext.OnProjectileTriggered(Id);
        }

        public FixedPos GetPos()
        {
            return _currentPos;
        }
    }
}
