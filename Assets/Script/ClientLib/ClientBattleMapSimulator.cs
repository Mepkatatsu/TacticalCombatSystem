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
        private Dictionary<uint, Vector3> _damageNumberOffsets = new();
        private HealthBarOverlay _healthBarOverlay;
        private DamageNumberOverlay _damageNumberOverlay;

        public uint simulationSpeed = 1;
        public bool repeatTest;
        public string baseUrl = "http://localhost:5099";
        public string accountId;

        public GameObject redTeamWinText;
        public GameObject blueTeamWinText;
        public GameObject drawText;
        public Canvas combatOverlayCanvas;
        public Camera combatOverlayProjectionCamera;

        private readonly TestClientApp _clientApp = new();
        private readonly List<ushort> _updateIntervals = new();

        private const ushort MinDeltaMs = 10;
        private const ushort MaxDeltaMs = 1000;

        private async void Start()
        {
#if !UNITY_EDITOR
            repeatTest = false;
#endif
            InitializeCombatOverlays();
            var stageName = GetStageName();
            await InitBattleMap(stageName);
            await ConnectToServer(stageName);
        }

        private void InitializeCombatOverlays()
        {
            if (!combatOverlayCanvas)
            {
                LogHelper.Error("ClientBattleMapSimulator.InitializeCombatOverlays: combat overlay Canvas is not assigned. Combat overlay UI is disabled.");
                return;
            }

            if (!combatOverlayProjectionCamera)
            {
                LogHelper.Error("ClientBattleMapSimulator.InitializeCombatOverlays: combat overlay projection camera is not assigned. Combat overlay UI is disabled.");
                return;
            }

            try
            {
                _healthBarOverlay = new HealthBarOverlay(combatOverlayCanvas, combatOverlayProjectionCamera);
            }
            catch (Exception exception)
            {
                _healthBarOverlay = null;
                LogHelper.Error($"ClientBattleMapSimulator.InitializeCombatOverlays: failed to create health bar layer. Health bar UI is disabled. {exception}");
            }

            try
            {
                _damageNumberOverlay = new DamageNumberOverlay(combatOverlayCanvas, combatOverlayProjectionCamera);
            }
            catch (Exception exception)
            {
                _damageNumberOverlay = null;
                LogHelper.Error($"ClientBattleMapSimulator.InitializeCombatOverlays: failed to create damage number layer. Damage number UI is disabled. {exception}");
            }
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
            _damageNumberOffsets.Clear();
            _healthBarOverlay?.Clear();
            _damageNumberOverlay?.Clear();
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

        private void LateUpdate()
        {
            _healthBarOverlay?.UpdatePositions();
            _damageNumberOverlay?.Update(Time.deltaTime);
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
            obj.name = $"{entity.name}_Model";
            var teamFlag = entity.GetTeamFlag();
            entityView.Initialize(entity.Hp, entity.MaxHp, teamFlag);
            entityView.OnMoveSpeedChanged(entity.MoveSpeed);
            entityView.OnAttackDelayMsChanged(entity.BasisAttackDelayMs, entity.AttackDelayMs);
            
            _entityViews.Add(entityId, entityView);
            _damageNumberOffsets.Add(entityId, modelData.healthBarOffset - Vector3.up * 0.3f);
            _healthBarOverlay?.Register(entityId, entityView.transform, modelData.healthBarOffset, entity.Hp,
                entity.MaxHp, teamFlag);
        }

        public void OnEntityPositionChanged(uint entityId, FixedPos pos)
        {
            if (!_entityViews.TryGetValue(entityId, out var entityView))
                return;
            
            entityView.OnPositionChanged(pos.ToVector3());
        }

        public void OnEntityDirectionChanged(uint entityId, FixedDir dir)
        {
            if (!_entityViews.TryGetValue(entityId, out var entityView))
                return;
            
            entityView.OnDirectionChanged(dir.ToDirection());
        }

        public void OnEntityStartMove(uint entityId)
        {
            if (!_entityViews.TryGetValue(entityId, out var entityView))
                return;
            
            entityView.OnStartMoving();
        }

        public void OnEntityStopMove(uint entityId)
        {
            if (!_entityViews.TryGetValue(entityId, out var entityView))
                return;
            
            entityView.OnStopMoving();
        }

        public void OnEntityStartAttack(uint attackerId, uint targetId)
        {
            if (!_entityViews.TryGetValue(attackerId, out var attacker))
                return;
            
            attacker.OnStartAttack();
        }

        public void OnEntityGetDamage(uint entityId, uint damage)
        {
            if (!_entityViews.TryGetValue(entityId, out var entityView))
                return;
            
            entityView.GetDamage(damage);
            _healthBarOverlay?.SetHp(entityId, entityView.Hp, entityView.MaxHp);

            if (!_damageNumberOffsets.TryGetValue(entityId, out var damageNumberOffset))
                return;

            _damageNumberOverlay?.Show(entityView.transform, damageNumberOffset, entityView.TeamFlag, damage);
        }

        public void OnEntityRetired(uint entityId)
        {
            if (!_entityViews.TryGetValue(entityId, out var entityView))
                return;
            
            entityView.OnRetired();
            _healthBarOverlay?.Unregister(entityId);
            _damageNumberOffsets.Remove(entityId);
        }

        public void OnProjectileAdded(ulong projectileId, Projectile projectile)
        {
            var projectileData = ProjectileSettings.Instance.GetProjectileData(projectile.ProjectileName);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectileData.projectilePath);
            var obj = Instantiate(prefab);
            obj.transform.localScale = new Vector3(projectileData.scale.x, projectileData.scale.y, projectileData.scale.z);
            var projectileView = obj.AddComponent<ProjectileView>();
            _projectileViews.Add(projectileId, projectileView);
        }

        public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos)
        {
            if (!_projectileViews.TryGetValue(projectileId, out var projectileView))
                return;

            const float projectileHeight = 1f;  // TODO: 임시값 수정해야 함

            var vector3Pos = pos.ToVector3();
            vector3Pos.y = projectileHeight;
            
            projectileView.OnPositionChanged(vector3Pos);
        }

        public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir)
        {
            if (!_projectileViews.TryGetValue(projectileId, out var projectileView))
                return;
            
            projectileView.OnDirectionChanged(dir.ToDirection());
        }

        public void OnProjectileTriggered(ulong projectileId)
        {
            if (!_projectileViews.Remove(projectileId, out var projectileView))
                return;

            Destroy(projectileView.gameObject);
        }

        public async void OnBattleEnd(TeamFlag winner)
        {
            // 화면에 이미 표시된 숫자는 마지막 피격 연출까지 마칠 수 있게 유지하고,
            // 결과 표시 뒤 콜백이 새 숫자를 추가하지 못하게 막는다.
            _damageNumberOverlay?.StopAcceptingNewNumbers();

            foreach (var projectileView in _projectileViews.Values)
            {
                Destroy(projectileView.gameObject);
            }
            _projectileViews.Clear();
            
            foreach (var entityView in _entityViews.Values)
            {
                entityView.OnStopMoving();
                entityView.OnBattleEnd();
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

            foreach (var entityView in _entityViews.Values)
            {
                entityView.OnUpdate(deltaMs);
            }
        }
    }
}
