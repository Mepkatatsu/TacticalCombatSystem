using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Script.ClientLib.Network.App;
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

        public uint simulationSpeed = 1;
        public bool repeatTest;
        public string baseUrl = "http://localhost:5099";
        public string accountId;

        public GameObject redTeamWinText;
        public GameObject blueTeamWinText;
        public GameObject drawText;

        private readonly TestClientApp _clientApp = new();
        private readonly List<ushort> _updateIntervals = new();

        private const ushort MinDeltaMs = 10;
        private const ushort MaxDeltaMs = 1000;

        private async void Start()
        {
#if !UNITY_EDITOR
            repeatTest = false;
#endif
            var stageName = GetStageName();
            await InitBattleMap(stageName);
            await ConnectToServer(stageName);
        }

        private void ReloadBattleMap()
        {
            Clear();
            ReloadScene();
        }

        private void Clear()
        {
            _entityViews.Clear();
            _projectileViews.Clear();
        }

        private void ReloadScene()
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }

        private async Task InitBattleMap(string stageName)
        {
            var (path, battleMapData) = await GetBattleMapData(stageName);

            if (battleMapData == null)
            {
                LogHelper.Error($"file {path} is not a BattleMapData");
                return;
            }

            _battleMapSimulator = new BattleMapSimulator(this, battleMapData);
            _battleMapSimulator.Init();
        }

        private async Task ConnectToServer(string stageName)
        {
            var connectSucceed = await _clientApp.ConnectToServer(baseUrl, accountId);

            if (!connectSucceed)
                return;

            var enterStageSucceed = await _clientApp.RequestEnterStage(stageName);

            LogHelper.Log($"enterStageSucceed: {enterStageSucceed}");
        }

        private static async Task<(string path, BattleMapData battleMapData)> GetBattleMapData(string stageName)
        {
            var path = $"Assets/Data/MapData/{stageName}_Data.json";
            var json = await File.ReadAllTextAsync(path);

            if (string.IsNullOrEmpty(json))
            {
                LogHelper.Error($"file {path} not found");
                return (path, null);
            }

            var battleMapData = JsonSerialize.DeserializeObject<BattleMapData>(json);
            return (path, battleMapData);
        }

        private static string GetStageName()
        {
            var scene = SceneManager.GetActiveScene();
            var stageName = scene.name;
            return stageName;
        }

        private void Update()
        {
#if !UNITY_EDITOR
            simulationSpeed = 1;
#endif
            ushort deltaMs = GetDeltaMs();
            for (int i = 0; i < simulationSpeed; ++i)
            {
                _battleMapSimulator?.Update(deltaMs);
            }
        }

        private ushort GetDeltaMs()
        {
            float deltaTime = Time.deltaTime;
            uint rawDeltaMs = (uint)(deltaTime * 1000);
            ushort deltaMs = (ushort)Math.Clamp(rawDeltaMs, MinDeltaMs, MaxDeltaMs);
            
            return deltaMs;
        }

        public void OnEntityAdded(uint entityId, Entity entity)
        {
            var modelData = ModelSettings.Instance.GetModelData(entity.name);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelData.modelPath);
            var obj = Instantiate(prefab);
            obj.transform.localScale = new Vector3(modelData.modelScale.x, modelData.modelScale.y, modelData.modelScale.z);
            var entityView = obj.AddComponent<EntityView>();
            entityView.SetHp(entity.Hp);
            
            _entityViews.Add(entityId, entityView);
        }

        public void OnEntityPositionChanged(uint entityId, FixedPos pos)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnPositionChanged(pos.ToVector3());
        }

        public void OnEntityDirectionChanged(uint entityId, FixedDir dir)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.OnDirectionChanged(dir.ToDirection());
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

        public void OnEntityGetDamage(uint entityId, uint damage)
        {
            _entityViews.TryGetValue(entityId, out var entityView);

            if (!entityView)
                return;
            
            entityView.GetDamage(damage);
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

        public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos)
        {
            _projectileViews.TryGetValue(projectileId, out var projectileView);

            if (!projectileView)
                return;

            const float projectileHeight = 1f;  // TODO: 임시값 수정해야 함

            var vector3Pos = pos.ToVector3();
            vector3Pos.y = projectileHeight;
            
            projectileView.OnPositionChanged(vector3Pos);
        }

        public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir)
        {
            _projectileViews.TryGetValue(projectileId, out var projectileView);

            if (!projectileView)
                return;
            
            projectileView.OnDirectionChanged(dir.ToDirection());
        }

        public void OnProjectileTriggered(ulong projectileId)
        {
            _projectileViews.TryGetValue(projectileId, out var projectileView);

            if (!projectileView)
                return;
            
            _projectileViews.Remove(projectileId);
            Destroy(projectileView.gameObject);
        }

        public async void OnBattleEnd(TeamFlag winner)
        {
            foreach (var projectileView in _projectileViews.Values)
            {
                Destroy(projectileView.gameObject);
            }
            _projectileViews.Clear();
            
            foreach (var entityView in _entityViews.Values)
            {
                entityView.OnStopMoving();
            }

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
            
            LogHelper.Log("===ClientResult===");
            
            foreach (var keyValuePair in _entityViews)
            {
                var id = keyValuePair.Key;
                var entityView = keyValuePair.Value;
                
                if (entityView.Hp > 0)
                {
                    LogHelper.Log($"[Alive] entityId: {id} hp: {entityView.Hp}");
                }
            }
            
            var result =  await _clientApp.RequestVerifyStageBattle(_updateIntervals, GetAliveEntities(), winner);

            if (!result)
            {
                LogHelper.Error($"OnBattleEnd: result is not Verified");
            }
            else
            {
                LogHelper.Log($"OnBattleEnd: result is Verified");
            }

            if (repeatTest)
                ReloadBattleMap();
        }
        
        public List<IEntityContext> GetAliveEntities()
        {
            return _battleMapSimulator?.GetAliveEntities();
        }

        public void OnBattleMapUpdated(ushort deltaMs)
        {
            _updateIntervals.Add(deltaMs);
        }
    }
}
