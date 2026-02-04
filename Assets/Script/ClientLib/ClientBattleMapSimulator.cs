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
        private Dictionary<Entity, GameObject> _models = new();
        
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

        public void OnEntityAdded(Entity entity)
        {
            var modelPath = ModelPathSettings.Instance.GetModelPath(entity.name);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var obj = Instantiate(prefab);
            obj.transform.localScale = new UnityEngine.Vector3(entity.modelScale.x, entity.modelScale.y, entity.modelScale.z);

            _models[entity] = obj;
        }

        public void OnEntityPositionChanged(Entity entity, Vector3 pos)
        {
            // TODO: 새로운 class를 만들어서 처리해야 함.
            _models.TryGetValue(entity, out var obj);

            if (!obj)
                return;
            
            LogHelper.Log($"OnEntityPositionChanged: {entity.name} {pos}");
            
            obj.transform.position = new UnityEngine.Vector3(pos.x, pos.y, pos.z);
        }

        public void OnEntityDirectionChanged(Entity entity, Vector3 dir)
        {
            _models.TryGetValue(entity, out var obj);

            if (!obj)
                return;
            
            obj.transform.rotation = Quaternion.LookRotation(new UnityEngine.Vector3(dir.x, dir.y, dir.z));
        }
    }
}
