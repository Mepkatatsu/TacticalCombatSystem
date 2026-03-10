namespace Script.CommonLib.Battle
{
    public class IdleState : IState
    {
        private IEntityContext _entityContext;

        public IdleState(IEntityContext entityContext)
        {
            _entityContext = entityContext;
        }
        
        public void Enter()
        {
            
        }

        public void Update(ushort deltaMs)
        {
            _entityContext.TryGetNearestEnemy();
        }

        public void Exit()
        {
            
        }
    }
}
