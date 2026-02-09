namespace Script.CommonLib.Battle
{
    public interface IState
    {
        public void Enter();
        public void Update(float deltaTime);
        public void Exit();
    }
}