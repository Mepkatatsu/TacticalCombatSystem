using Script.ClientLib;
using Script.ClientLib.SaveData;
using Script.CommonLib;
using Script.EditorLib.SaveData;
using UnityEditor;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Script.EditorLib
{
    [CustomEditor(typeof(Obstacle))]
    public class ObstacleEditor : Editor
    {
        private void OnSceneGUI()
        {
            if (target is not Obstacle obstacle)
            {
                LogHelper.Error("Obstacle.OnSceneGUI: target is not Obstacle");
                return;
            }

            if (EditorSaveData.DrawBattleMapBaseGrid)
                BattleMapEditor.DrawBaseGrid(obstacle.gameObject.GetParent<BattleMap>());
            
            DrawObstacle(obstacle);
        }

        public static void DrawObstacle(Obstacle obstacle)
        {
            if (obstacle.obstacleType != Obstacle.ObstacleType.Square)
                return;
            
            var blockedPoints = obstacle.GetBlockedPoints();

            Handles.color = Color.red;
            
            for (var i = 0; i < blockedPoints.Count; i++)
            {
                var waypoint = blockedPoints[i];
                Handles.DrawWireCube(new Vector3(waypoint.X, 0, waypoint.Y), new Vector3(1, 0, 1));
            }
            
            DrawObstacleWaypoints(obstacle);
        }

        private static void DrawObstacleWaypoints(Obstacle obstacle)
        {
            var waypoints = obstacle.GetWaypoints();

            Handles.color = Color.green;
            
            for (var i = 0; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];
                Handles.DrawWireCube(new Vector3(waypoint.X, 0, waypoint.Y), new Vector3(1, 0, 1));
            }
        }

        public override void OnInspectorGUI()
        {
            EditorSaveData.DrawBattleMapBaseGrid = EditorGUILayout.Toggle("Draw BattleMap Base Grid", EditorSaveData.DrawBattleMapBaseGrid);
            
            base.OnInspectorGUI();
        }
    }
}
