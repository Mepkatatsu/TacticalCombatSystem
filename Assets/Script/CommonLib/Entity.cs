namespace Script.CommonLib
{
    public class Entity : IMover
    {
        private MoveAgent _moveAgent;
        
        private Vector3 _pos;

        public string name;
        public string startPositionName;
        public string endPositionName;
        public Vector3 modelScale = new Vector3(3.5f, 3.5f, 3.5f);

        public Entity()
        {
            _moveAgent = new MoveAgent(this, 1);
        }

        public Entity(EntityData entityData)
        {
            _moveAgent = new MoveAgent(this, 1);
            name = entityData.name;
            startPositionName = entityData.startPositionName;
            endPositionName = entityData.endPositionName;
            modelScale = entityData.modelScale;
        }

        public void Update(float deltaTime)
        {
            _moveAgent.Update(deltaTime);
        }

        public Vector3 GetPos()
        {
            return _pos;
        }

        public void SetPos(Vector3 pos)
        {
            _pos = pos;
        }

        public void MoveTo(Vector3 pos)
        {
            _moveAgent.SetDestination(pos);
            _moveAgent.SetIsMoving(true);
        }
        
        public void Stop()
        {
            _moveAgent.SetIsMoving(false);
        }
    }
}
