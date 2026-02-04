using System;
using Script.CommonLib;
using UnityEngine;

public class BattleMap : MonoBehaviour
{
    [Range(1, BattleMapData.MaxMapSize)] public int width = 1;
    [Range(1, BattleMapData.MaxMapSize)] public int height = 1;

    public GridPos offset;

    private void OnValidate()
    {
        // 홀수여야 대칭성이 있어 관리가 편할 듯 하여 홀수로 강제. 변경하려면 minPos, maxPos 및 격자 그려주는 부분 수정해야 함.
        if (width % 2 == 0)
        {
            LogHelper.Warning($"관리 편의성을 위해 width를 홀수로 사용하시기를 권장합니다. width값을 {width + 1}(으)로 변경합니다.");
            ++width;
        }
        
        if (height % 2 == 0)
        {
            LogHelper.Warning($"관리 편의성을 위해 height를 홀수로 사용하시기를 권장합니다. width값을 {height + 1}(으)로 변경합니다.");
            ++height;
        }
    }

    public GridPos GetCenterGridPos()
    {
        var center = new GridPos((int)MathF.Truncate(transform.position.x) + offset.x, (int)MathF.Truncate(transform.position.z + offset.y));
        return center;
    }

    public GridPos GetMinGridPos()
    {
        var center = GetCenterGridPos();
        
        var xPos = center.x - width / 2;
        var zPos = center.y - height / 2;

        var minPos = new GridPos(xPos, zPos);
        return minPos;
    }
    
    public GridPos GetMaxGridPos()
    {
        var center = GetCenterGridPos();
        
        var xPos = center.x + width / 2;
        var zPos = center.y + height / 2;

        var maxPos = new GridPos(xPos, zPos);
        return maxPos;
    }
}
