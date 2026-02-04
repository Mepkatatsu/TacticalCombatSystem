using System;
using Script.ClientLib;
using Script.ClientLib.SaveData;
using Script.CommonLib;
using Script.EditorLib;
using Script.EditorLib.SaveData;
using UnityEditor;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

[CustomEditor(typeof(BattlePosition))]
public class BattlePositionEditor : Editor
{
    private void OnSceneGUI()
    {
        if (target is not BattlePosition battlePosition)
        {
            LogHelper.Error("Obstacle.OnSceneGUI: target is not Obstacle");
            return;
        }

        if (EditorSaveData.DrawBattleMapBaseGrid || EditorSaveData.DrawObstacle)
        {
            var battleMap = battlePosition.gameObject.GetParent<BattleMap>();

            if (battleMap)
            {
                if (EditorSaveData.DrawBattleMapBaseGrid)
                    BattleMapEditor.DrawBaseGrid(battleMap);
            
                if (EditorSaveData.DrawObstacle)
                    BattleMapEditor.DrawObstacles(battleMap);
            }
        }
            
        DrawBattlePosition(battlePosition);
    }
    
    public static void DrawBattlePosition(BattlePosition battlePosition)
    {
        switch (battlePosition.positionType)
        {
            case BattlePositionData.PositionType.Waypoint:
                Handles.color = Color.green;
                break;
            case BattlePositionData.PositionType.StartPosition:
            case BattlePositionData.PositionType.EndPosition:
                Handles.color = Color.blue;
                break;
        }
        
        var centerGridPos = battlePosition.GetGridPos();
        
        Handles.DrawWireCube(new Vector3(centerGridPos.x, 0, centerGridPos.y), new Vector3(1, 0, 1));
    }
    
    public override void OnInspectorGUI()
    {
        EditorSaveData.DrawBattleMapBaseGrid = EditorGUILayout.Toggle("Draw BattleMap Base Grid", EditorSaveData.DrawBattleMapBaseGrid);
        EditorSaveData.DrawObstacle = EditorGUILayout.Toggle("Draw Obstacles", EditorSaveData.DrawObstacle);
        
        base.OnInspectorGUI();
    }
}
