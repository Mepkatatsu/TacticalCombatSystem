using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.ClientLib;
using Script.CommonLib;
using Script.CommonLib.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Script.EditorLib
{
    public static class BattleMapSaveProcessor
    {
        [InitializeOnLoadMethod]
        private static void RegisterOnSceneSaved()
        {
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }
        
        private static void OnSceneSaved(Scene scene)
        {
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                var battleMap = rootObj.GetComponentInChildren<BattleMap>();
                    
                if (!battleMap)
                    continue;
                
                SaveBattleMap(battleMap);
            }
        }

        public static void SaveBattleMap(BattleMap battleMap)
        {
            LogHelper.Log($"SaveBattleMap: {battleMap.name}");

            var battleMapData = new BattleMapData
            {
                minGridPos = battleMap.GetMinGridPos(),
                maxGridPos = battleMap.GetMaxGridPos(),
                battlePositions = SaveBattlePositions(battleMap),
                obstacles = SaveObstacles(battleMap),
                entities = SaveEntities(battleMap),
            };

            if (!CheckBattleMapData(battleMapData))
                return;
            
            // TODO: Binary로 저장/읽어오도록 수정 (속도 향상)
            var path = $"Assets/Data/MapData/{battleMap.name}_Data.json";
            var json = JsonConvert.SerializeObject(battleMapData);
            
            File.WriteAllText(path, json);
            
            LogHelper.Log($"BattleMapData Saved: {path}");
            
            AssetDatabase.Refresh();
        }

        private static bool CheckBattleMapData(BattleMapData battleMapData)
        {
            var battlePositions = battleMapData.battlePositions;

            var startPositions = battlePositions.FindAll(e => e.positionType == BattlePositionData.PositionType.StartPosition);
            var endPositions = battlePositions.FindAll(e => e.positionType == BattlePositionData.PositionType.EndPosition);

            if (startPositions.Count != endPositions.Count)
            {
                LogHelper.Error("BattleMapData: StartPosition과 EndPosition의 개수가 맞지 않습니다.");
                return false;
            }

            var battleMapSimulator = new BattleMapPathFinder(battleMapData);
            var waypointList = new List<GridPos>();
            
            for (var i = 0; i < startPositions.Count; i++)
            {
                var startPosition = startPositions[i];
                var endPosition = endPositions.Find(e => e.index == startPosition.index);

                if (endPosition == null)
                {
                    LogHelper.Error($"BattleMapData: StartPosition의 index와 동일한 EndPosition을 찾을 수 없습니다. (StartPosition의 index: {startPosition.index})");
                    return false;
                }

                battleMapSimulator.FindWaypoints(startPosition.gridPos, endPosition.gridPos, waypointList);

                if (waypointList.Count == 0)
                {
                    LogHelper.Error($"BattleMapData: StartPosition에서 EndPosition으로 이동하는 경로를 찾을 수 없습니다. (index: {startPosition.index})");
                    return false;
                }
            }

            return true;
        }

        private static List<BattlePositionData> SaveBattlePositions(BattleMap battleMap)
        {
            var battlePositions = battleMap.transform.GetComponentsInChildren<BattlePosition>();

            if (battlePositions.IsEmpty())
                return null;
            
            var startPosIndex = 0;
            var endPosIndex = 0;
                
            foreach (var battlePosition in battlePositions)
            {
                if (battlePosition.positionType == BattlePositionData.PositionType.StartPosition)
                {
                    battlePosition.index = startPosIndex++;
                }
                else if (battlePosition.positionType == BattlePositionData.PositionType.EndPosition)
                {
                    battlePosition.index = endPosIndex++;
                }
                    
                battlePosition.name = $"{battlePosition.positionType}{battlePosition.index + 1}";
            }

            if (startPosIndex != endPosIndex)
                throw new Exception("StartPosition과 EndPosition의 수가 다릅니다. BattleMap이 저장되지 않습니다.");
            
            var battlePositionDataList = new List<BattlePositionData>();
            
            foreach (var battlePosition in battlePositions)
            {
                battlePositionDataList.Add(battlePosition.ToBattlePositionData());
            }

            return battlePositionDataList;
        }
        
        private static List<ObstacleData> SaveObstacles(BattleMap battleMap)
        {
            var obstacles = battleMap.transform.GetComponentsInChildren<Obstacle>();

            if (obstacles.IsEmpty())
                return null;
            
            var obstacleDataList = new List<ObstacleData>();
            
            foreach (var obstacle in obstacles)
            {
                obstacleDataList.Add(obstacle.ToObstacleData());
            }

            return obstacleDataList;
        }
        
        private static List<EntityData> SaveEntities(BattleMap battleMap)
        {
            var entityComponents = battleMap.transform.GetComponentsInChildren<EntityComponent>();

            if (entityComponents.IsEmpty())
                return null;
            
            var entitiesDataList = new List<EntityData>();
            
            foreach (var entityComponent in entityComponents)
            {
                entitiesDataList.Add(entityComponent.entityData);
            }

            return entitiesDataList;
        }
    }
}