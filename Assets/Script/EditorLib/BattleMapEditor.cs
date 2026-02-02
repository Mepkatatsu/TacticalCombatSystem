using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.ClientLib;
using Script.CommonLib;
using Script.CommonLib.Map;
using Script.EditorLib.SaveData;
using UnityEditor;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Script.EditorLib
{
    [CustomEditor(typeof(BattleMap))]
    public class BattleMapEditor : Editor
    {
        private readonly List<GridPos> _waypoints = new();
        private readonly List<string> _namedWaypoints = new();
        private string[] _waypointsArray;

        private readonly BresenhamSuperCoverNodeVisitor _visitor = new();
        private readonly List<GridPos> _visitedWaypointList = new();
        private readonly List<GridPos> _visitedAllGridPosList = new();
        
        private int _startPointIndex;
        private int _endPointIndex;
        
        private BattleMapPathFinder _battleMapPathFinder;
        
        private void OnSceneGUI()
        {
            if (target is not BattleMap battleMap)
            {
                LogHelper.Error("BattleMapEditor.OnSceneGUI: target is not BattleMap");
                return;
            }

            if (EditorSaveData.DrawBattleMapBaseGrid)
                DrawBaseGrid(battleMap);
            
            if (EditorSaveData.DrawObstacle)
                DrawObstacles(battleMap);

            if (EditorSaveData.DrawBattlePositions)
                DrawBattlePositions(battleMap);

            DrawFoundPath();
        }

        public static void DrawBaseGrid(BattleMap battleMap)
        {
            if (!battleMap)
                return;
            
            var width = battleMap.width;
            var height = battleMap.height;
            
            var minGridPos = battleMap.GetMinGridPos();

            Handles.color = Color.white;
            
            // 세로선
            for (var x = 0; x <= width; x++)
            {
                // 각 Position이 칸의 중심에 오도록 하기 위해 0.5f씩 조절하여 그림
                var xPos = minGridPos.X + x - 0.5f;
                var zPos = minGridPos.Y - 0.5f;
                
                var start = new Vector3(xPos, 0, zPos);
                var end = new Vector3(xPos, 0, zPos + height);
                Handles.DrawLine(start, end);
            }

            // 가로선
            for (var z = 0; z <= height; z++)
            {
                // 각 Position이 칸의 중심에 오도록 하기 위해 0.5f씩 조절하여 그림
                var xPos = minGridPos.X - 0.5f;
                var zPos = minGridPos.Y + z - 0.5f;
                
                var start = new Vector3(xPos, 0, zPos);
                var end = new Vector3(xPos + width, 0, zPos);
                
                Handles.DrawLine(start, end);
            }
        }

        public static void DrawObstacles(BattleMap battleMap)
        {
            var obstacles = battleMap.GetComponentsInChildren<Obstacle>();

            if (obstacles.IsEmpty())
                return;
            
            foreach (var obstacle in obstacles)
            {
                ObstacleEditor.DrawObstacle(obstacle);
            }
        }
        
        private static void DrawBattlePositions(BattleMap battleMap)
        {
            var battlePositions = battleMap.transform.GetComponentsInChildren<BattlePosition>();

            if (battlePositions.IsEmpty())
                return;
            
            foreach (var battlePosition in battlePositions)
            {
                BattlePositionEditor.DrawBattlePosition(battlePosition);
            }
        }
        
        public override void OnInspectorGUI()
        {
            if (target is not BattleMap battleMap)
            {
                LogHelper.Error("BattleMapEditor.OnSceneGUI: target is not BattleMap");
                return;
            }
            
            EditorSaveData.DrawBattleMapBaseGrid = EditorGUILayout.Toggle("Draw BattleMap Base Grid", EditorSaveData.DrawBattleMapBaseGrid);
            EditorSaveData.DrawObstacle = EditorGUILayout.Toggle("Draw Obstacles", EditorSaveData.DrawObstacle);
            EditorSaveData.DrawBattlePositions = EditorGUILayout.Toggle("Draw BattlePositions", EditorSaveData.DrawBattlePositions);
            EditorSaveData.ShowPathFinder = EditorGUILayout.Toggle("Show Path Finder", EditorSaveData.ShowPathFinder);
            
            if (EditorSaveData.ShowPathFinder)
                ShowPathFinder(battleMap);
            
            base.OnInspectorGUI();
        }

        private void ShowPathFinder(BattleMap battleMap)
        {
            var obstacles = battleMap.GetComponentsInChildren<Obstacle>();
            var battlePositions = battleMap.transform.GetComponentsInChildren<BattlePosition>();
            
            _namedWaypoints.Clear();

            GetWaypoints(obstacles, battlePositions);

            _startPointIndex = EditorGUILayout.Popup("Start Point", _startPointIndex, _waypointsArray);
            _endPointIndex = EditorGUILayout.Popup("End Point", _endPointIndex, _waypointsArray);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Find Path"))
                FindPath(battleMap);
            
            if (GUILayout.Button("Clear Path"))
                ClearPath();
            
            EditorGUILayout.EndHorizontal();
        }

        private void GetWaypoints(Obstacle[] obstacles, BattlePosition[] battlePositions)
        {
            if (obstacles.IsNotEmpty())
            {
                for (var i = 0; i < obstacles.Length; i++)
                {
                    var waypoints = obstacles[i].GetWaypoints();
                    
                    for (var j = 0; j < waypoints.Count; j++)
                    {
                        _waypoints.Add(waypoints[j]);
                        _namedWaypoints.Add($"Obstacle {i + 1} {waypoints[j]}");
                    }
                }
            }

            if (battlePositions.IsNotEmpty())
            {
                for (var i = 0; i < battlePositions.Length; i++)
                {
                    var gridPos = battlePositions[i].GetGridPos();
                    
                    _waypoints.Add(gridPos);
                    _namedWaypoints.Add($"BattlePosition ({battlePositions[i].positionType}) {i + 1} ({gridPos})");
                }
            }
            
            if (_waypointsArray.IsEmpty() || _waypointsArray.Length != _namedWaypoints.Count)
            {
                _waypointsArray = new string[_namedWaypoints.Count];

                for (var i = 0; i < _namedWaypoints.Count; i++)
                {
                    _waypointsArray[i] = _namedWaypoints[i];
                }
            }
        }

        private void FindPath(BattleMap battleMap)
        {
            var path = $"Assets/Data/MapData/{battleMap.name}_Data.json";
            var json = File.ReadAllText(path);
            var battleMapData = JsonConvert.DeserializeObject<BattleMapData>(json);

            _battleMapPathFinder = new BattleMapPathFinder(battleMapData);
            
            ClearPath();
            var startGridPos = _waypoints[_startPointIndex];
            var endGridPos = _waypoints[_endPointIndex];
            
            _battleMapPathFinder.FindWaypoints(startGridPos, endGridPos, _visitedWaypointList);
            
            for (var i = 0; i < _visitedWaypointList.Count; i++)
            {
                var waypoint = _visitedWaypointList[i];

                endGridPos = waypoint;
                
                _battleMapPathFinder.FindStraightPathWithoutBlock(startGridPos, endGridPos, _visitedAllGridPosList);

                startGridPos = waypoint;
            }
            
            SceneView.RepaintAll();
        }

        private void ClearPath()
        {
            _visitedAllGridPosList.Clear();
            SceneView.RepaintAll();
        }

        private void DrawFoundPath()
        {
            if (_visitedAllGridPosList.IsEmpty())
                return;
            
            Handles.color = Color.green;
            Handles.matrix = Matrix4x4.identity;
            
            for (var i = 0; i < _visitedAllGridPosList.Count; i++)
            {
                var gridPos = _visitedAllGridPosList[i];
                Handles.DrawWireCube(new Vector3(gridPos.X, 0, gridPos.Y), new Vector3(1, 0, 1));
            }
        }
    }
}
