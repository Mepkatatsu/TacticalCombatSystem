using UnityEngine;

namespace Script.ClientLib
{
    public class GameParameterSettings : ScriptableObjectBase<GameParameterSettings>
    {
        [SerializeField] private ushort defaultMoveSpeed = 4000;
        [SerializeField] private ushort defaultAttackDelayMs = 1000;
        
        public ushort DefaultMoveSpeed => defaultMoveSpeed;
        public ushort DefaultAttackDelayMs => defaultAttackDelayMs;
    }
}
