namespace Script.CommonLib.Battle
{
    public class AttackState : IState
    {
        private IEntityContext _entityContext;

        private const ushort DefaultAttackDelayMs = 2000;
        private uint _lastAttackMs;

        public AttackState(IEntityContext entityContext)
        {
            _entityContext = entityContext;
        }
        
        public void Enter()
        {
            
        }

        public void Update(ushort deltaMs)
        {
            var attackDelayMs = GetAttackDelayMs();
            var battleMapElapsedMs = _entityContext.GetBattleMapElapsedMs();

            if (!CanAttack(attackDelayMs, battleMapElapsedMs))
                return;

            _lastAttackMs = battleMapElapsedMs;
            _entityContext.RequestAttack();
        }

        public void Exit()
        {
            
        }

        private ushort GetAttackDelayMs()
        {
            return (ushort)(DefaultAttackDelayMs / _entityContext.AttackSpeed);
        }

        private bool CanAttack(ushort attackDelayMs, uint battleMapElapsedMs)
        {
            return _lastAttackMs + attackDelayMs < battleMapElapsedMs;
        }
    }
}
