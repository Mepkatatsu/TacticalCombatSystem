using System;
using Script.CommonLib;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class BattlePosition : MonoBehaviour
{
    public BattlePositionData.PositionType positionType;

    [HideInInspector] public int index; // StartPosition, EndPosition일 때 사용

    // Position을 정수로 맞춰줌
    public GridPos GetGridPos()
    {
        var origin = transform.position;

        var xPos = (int)MathF.Truncate(origin.x);
        var yPos = (int)MathF.Truncate(origin.y);
        var zPos = (int)MathF.Truncate(origin.z);
        
        transform.position = new Vector3(xPos, yPos, zPos);

        if (origin != transform.position)
        {
            LogHelper.Error($"{name}의 Position이 정수값으로 변경되었습니다. {origin} -> {transform.position}");
        }

        return new GridPos(xPos, zPos);
    }

    public BattlePositionData ToBattlePositionData()
    {
        var gridPos = GetGridPos();
        
        var battlePositionData = new BattlePositionData
        {
            name = name,
            gridPos = gridPos,
            positionType = positionType,
            index = index
        };

        return battlePositionData;
    }
}
