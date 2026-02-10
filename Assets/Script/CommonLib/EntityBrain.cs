using Script.CommonLib.Battle;

namespace Script.CommonLib
{
    public class EntityBrain
    {
        private IEntityContext _entityContext;
        
        public EntityBrain(IEntityContext entityContext)
        {
            _entityContext = entityContext;
        }

        public EntityStateType ThinkNextStateType()
        {
            if (!_entityContext.IsAlive())
                return EntityStateType.Die;

            if (_entityContext.IsMainTargetInRange())
                return EntityStateType.Attack;

            return _entityContext.HasArrived() ? EntityStateType.Idle : EntityStateType.Move;
        }
    }
}
