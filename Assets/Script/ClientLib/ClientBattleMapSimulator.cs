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
        private Dictionary<ulong, ProjectileView> _projectileViews = new();

        public GameObject redTeamWinText;
        public GameObject blueTeamWinText;
        public GameObject drawText;
        
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
            obj.transform.localScale = new Vector3(modelData.modelScale.x, modelData.modelScale.y, modelData.modelScale.z);
            var entityView = obj.AddComponent<EntityView>();
            _entityViews.Add(entityId, entityView);
        }

        public void OnEntityPositionChanged(uint entityId, Vec3 pos)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnPositionChanged(new Vector3(pos.x, pos.y, pos.z));
        }

        public void OnEntityDirectionChanged(uint entityId, Vec3 dir)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnDirectionChanged(new Vector3(dir.x, dir.y, dir.z));
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

        public void OnEntityStartAttack(uint attackerId, uint targetId)
        {
            _entityViews.TryGetValue(attackerId, out var attacker);
            
            if (!attacker)
                return;
            
            attacker.OnStartAttack();
        }

        public void OnEntityRetired(uint entityId)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnRetired();
        }

        public void OnProjectileAdded(ulong projectileId, Projectile projectile)
        {
            const string projectileName = "Projectile"; // TODO: 임시값 변경
            
            var projectileData = ProjectileSettings.Instance.GetProjectileData(projectileName);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectileData.projectilePath);
            var obj = Instantiate(prefab);
            obj.transform.localScale = new Vector3(projectileData.scale.x, projectileData.scale.y, projectileData.scale.z);
            var projectileView = obj.AddComponent<ProjectileView>();
            _projectileViews.Add(projectileId, projectileView);
        }

        public void OnProjectilePositionChanged(ulong projectileId, Vec3 pos)
        {
            _projectileViews.TryGetValue(projectileId, out var projectileView);

            if (!projectileView)
                return;

            const float projectileHeight = 1f;  // TODO: 임시값 수정해야 함
            
            projectileView.OnPositionChanged(new Vector3(pos.x, projectileHeight, pos.z));
        }

        public void OnProjectileDirectionChanged(ulong projectileId, Vec3 dir)
        {
            _projectileViews.TryGetValue(projectileId, out var projectileView);

            if (!projectileView)
                return;
            
            projectileView.OnDirectionChanged(new Vector3(dir.x, dir.y, dir.z));
        }

        public void OnProjectileTriggered(ulong projectileId)
        {
            _projectileViews.TryGetValue(projectileId, out var projectileView);

            if (!projectileView)
                return;
            
            _projectileViews.Remove(projectileId);
            Destroy(projectileView.gameObject);
        }

        public void OnBattleEnd(TeamFlag winner)
        {
            if (winner == TeamFlag.Blue)
            {
                blueTeamWinText.SetActive(true);
            }
            else if (winner == TeamFlag.Red)
            {
                redTeamWinText.SetActive(true);
            }
            else if (winner == TeamFlag.None)
            {
                drawText.SetActive(true);
            }
        }
    }
}
