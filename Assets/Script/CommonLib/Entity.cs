namespace Script.CommonLib
{
    public class Entity : IMover
    {
        private MoveAgent _moveAgent;
        
        private Vector3 _pos;

        public Entity()
        {
            _moveAgent = new MoveAgent(this, 1);
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
