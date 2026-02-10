using System.Collections.Generic;
using System.IO;
using Script.CommonLib;
using Script.CommonLib.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            var modelData = ModelSettings.Instance.GetModelData(entity.name);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelData.modelPath);
            var obj = Instantiate(prefab);
            obj.transform.localScale = new UnityEngine.Vector3(modelData.modelScale.x, modelData.modelScale.y, modelData.modelScale.z);
            var entityView = obj.AddComponent<EntityView>();
            _entityViews.Add(entityId, entityView);
        }

        public void OnEntityPositionChanged(uint entityId, Vec3 pos)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnPositionChanged(new UnityEngine.Vector3(pos.x, pos.y, pos.z));
        }

        public void OnEntityDirectionChanged(uint entityId, Vec3 dir)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnDirectionChanged(new UnityEngine.Vector3(dir.x, dir.y, dir.z));
        }

        public void OnEntityStartMove(uint entityId)
        {
            _entityViews.TryGetValue(entityId, out var entityView);
            
            if (!entityView)
                return;
            
            entityView.OnStartMoving();
        }

        public void OnEntityStopMove(uint entityId)
        {
            _entityViews.TryGetValue(entityId, out var entityView);
            
            if (!entityView)
                return;
            
            entityView.OnStopMoving();
        }

        public void OnEntityAttack(uint attackerId, uint targetId)
        {
            _entityViews.TryGetValue(attackerId, out var attacker);
            
            if (!attacker)
                return;
            
            attacker.OnStartAttack();
            
            LogHelper.Log($"entity {attackerId} attacked {targetId}");
        }

        public void OnEntityRetired(uint entityId)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnRetired();
        }
    }
}
