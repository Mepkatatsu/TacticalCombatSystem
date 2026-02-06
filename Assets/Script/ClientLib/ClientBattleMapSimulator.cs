using System.Collections.Generic;
using System.IO;
using Script.CommonLib;
using Script.CommonLib.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector3 = Script.CommonLib.Vector3;

namespace Script.ClientLib
{
    public class ClientBattleMapSimulator : MonoBehaviour, IBattleMapEventHandler
    {
        private BattleMapSimulator _battleMapSimulator;
        private Dictionary<uint, EntityView> _entityViews = new();
        
        private void Start()
        {
            var scene = SceneManager.GetActiveScene();
            var path = $"Assets/Data/MapData/{scene.name}_Data.json";
            var json = File.ReadAllText(path);

            if (string.IsNullOrEmpty(json))
            {
                LogHelper.Error($"file {path} not found");
                return;
            }
            
            var battleMapData = JsonSerialize.DeserializeObject<BattleMapData>(json);

            if (battleMapData == null)
            {
                LogHelper.Error($"file {path} is not a BattleMapData");
                return;
            }
            
            _battleMapSimulator = new BattleMapSimulator(this, battleMapData);
            _battleMapSimulator.Init();
        }

        private void Update()
        {
            _battleMapSimulator.Update(Time.deltaTime);
        }

        public void OnEntityAdded(uint entityId, Entity entity)
        {
            var modelPath = ModelPathSettings.Instance.GetModelPath(entity.name);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var obj = Instantiate(prefab);
            obj.transform.localScale = new UnityEngine.Vector3(entity.modelScale.x, entity.modelScale.y, entity.modelScale.z);
            var entityView = obj.AddComponent<EntityView>();
            _entityViews.Add(entityId, entityView);
        }

        public void OnEntityPositionChanged(uint entityId, Vector3 pos)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnPositionChanged(new UnityEngine.Vector3(pos.x, pos.y, pos.z));
        }

        public void OnEntityDirectionChanged(uint entityId, Vector3 dir)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnDirectionChanged(new UnityEngine.Vector3(dir.x, dir.y, dir.z));
        }

        public void OnEntityStartMoving(uint entityId)
        {
            _entityViews.TryGetValue(entityId, out var entityView);
            
            if (!entityView)
                return;
            
            entityView.OnStartMoving();
        }

        public void OnEntityStopMoving(uint entityId)
        {
            _entityViews.TryGetValue(entityId, out var entityView);
            
            if (!entityView)
                return;
            
            entityView.OnStopMoving();
        }
    }
}
