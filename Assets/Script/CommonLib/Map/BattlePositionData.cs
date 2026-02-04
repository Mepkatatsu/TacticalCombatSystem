using System;
using UnityEngine.Serialization;

namespace Script.CommonLib
{
    [Serializable]
    public class BattlePositionData
    {
        public enum PositionType
        {
            Waypoint,
            StartPosition,
            EndPosition,
        }

        public string name;

        public GridPos gridPos;
        
        public PositionType positionType;
    
        public int index; // StartPosition, EndPosition일 때 사용
    }
}
