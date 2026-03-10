namespace Script.CommonLib.Battle
{
    public interface IState
    {
        public void Enter();
        public void Update(ushort deltaMs);
        public void Exit();
    }
}