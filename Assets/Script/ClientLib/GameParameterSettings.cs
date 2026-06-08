using UnityEngine;

namespace Script.ClientLib
{
    public class GameParameterSettings : ScriptableObjectBase<GameParameterSettings>
    {
        [SerializeField] private ushort defaultMoveSpeed = 4000;
        [SerializeField] private ushort comboMaintainMs = 1000;
        
        public ushort DefaultMoveSpeed => defaultMoveSpeed;
        public ushort ComboMaintainMs => comboMaintainMs;
    }
}
