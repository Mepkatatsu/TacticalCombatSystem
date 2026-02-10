namespace Script.CommonLib.Battle
{
    public class AttackState : IState
    {
        private IEntityContext _entityContext;

        private const float DefaultAttackDelaySec = 2f;
        private float _lastAttackSec;

        public AttackState(IEntityContext entityContext)
        {
            _entityContext = entityContext;
        }
        
        public void Enter()
        {
            
        }

        public void Update(float deltaTime)
        {
            var attackDelaySec = GetAttackDelaySec();
            var battleMapElapsedSec = _entityContext.GetBattleMapElapsedSec();

            if (!CanAttack(attackDelaySec, battleMapElapsedSec))
                return;

            _lastAttackSec = battleMapElapsedSec;
            _entityContext.RequestAttack();
        }

        public void Exit()
        {
            
        }

        private float GetAttackDelaySec()
        {
            return DefaultAttackDelaySec / _entityContext.AttackSpeed;
        }

        private bool CanAttack(float attackDelaySec, float battleMapElapsedSec)
        {
            return _lastAttackSec + attackDelaySec < battleMapElapsedSec;
        }
    }
}
