using System.Collections.Generic;
using Script.CommonLib;
using UnityEngine;

namespace Script.ClientLib
{
    public class HealthBarOverlay
    {
        private const string HealthBarPrefabPath = "Prefabs/HealthBar";
        private const string HealthBarContainerName = "HealthBarOverlay";

        private readonly RectTransform _canvasTransform;
        private readonly Camera _worldProjectionCamera;
        private readonly Dictionary<uint, TrackedHealthBar> _trackedHealthBars = new();
        private readonly List<uint> _removedEntityIds = new();

        private GameObject _healthBarPrefab;
        private RectTransform _healthBarContainer;
        private bool _isValid;

        private class TrackedHealthBar
        {
            public Transform targetTransform;
            public Vector3 localOffset;
            public HealthBarView healthBarView;
            public RectTransform rectTransform;
        }

        public HealthBarOverlay(Canvas canvas, Camera worldProjectionCamera)
        {
            if (canvas == null)
            {
                Debug.LogError("HealthBarOverlay: health bar Canvas is not assigned.");
                return;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogError("HealthBarOverlay: health bar Canvas must use Screen Space - Overlay mode.");
                return;
            }

            if (worldProjectionCamera == null)
            {
                Debug.LogError("HealthBarOverlay: world projection camera is not assigned.");
                return;
            }

            _canvasTransform = canvas.transform as RectTransform;
            _worldProjectionCamera = worldProjectionCamera;
            _isValid = _canvasTransform != null;

            if (!_isValid)
            {
                Debug.LogError("HealthBarOverlay: health bar Canvas does not have a RectTransform.");
                return;
            }

            _isValid = CreateHealthBarContainer();
        }

        public void Register(uint entityId, Transform targetTransform, Vector3 localOffset, uint currentHp, uint maxHp,
            TeamFlag teamFlag)
        {
            if (!_isValid || targetTransform == null)
                return;

            Unregister(entityId);

            if (!EnsureHealthBarContainer())
                return;

            var healthBarPrefab = GetHealthBarPrefab();
            if (healthBarPrefab == null)
                return;

            GameObject healthBarObject = null;

            try
            {
                healthBarObject = Object.Instantiate(healthBarPrefab, _healthBarContainer, false);

                var healthBarView = healthBarObject.GetComponent<HealthBarView>();
                if (healthBarView == null)
                {
                    Debug.LogError("HealthBarOverlay.Register: health bar prefab does not contain a HealthBarView component.");
                    DestroyHealthBarObjectSafely(healthBarObject);
                    return;
                }

                var rectTransform = healthBarObject.transform as RectTransform;
                if (rectTransform == null)
                {
                    Debug.LogError("HealthBarOverlay.Register: health bar prefab root does not have a RectTransform.");
                    DestroyHealthBarObjectSafely(healthBarObject);
                    return;
                }

                healthBarView.Initialize(currentHp, maxHp, teamFlag);
                _trackedHealthBars.Add(entityId, new TrackedHealthBar
                {
                    targetTransform = targetTransform,
                    localOffset = localOffset,
                    healthBarView = healthBarView,
                    rectTransform = rectTransform,
                });
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"HealthBarOverlay.Register: failed to initialize health bar for entity {entityId}. {exception.Message}");
                DestroyHealthBarObjectSafely(healthBarObject);
            }
        }

        private static void DestroyHealthBarObjectSafely(GameObject healthBarObject)
        {
            if (healthBarObject == null)
                return;

            try
            {
                Object.Destroy(healthBarObject);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"HealthBarOverlay.Register: failed to destroy an invalid health bar. {exception.Message}");
            }
        }

        public void SetHp(uint entityId, uint currentHp, uint maxHp)
        {
            if (!_trackedHealthBars.TryGetValue(entityId, out var trackedHealthBar))
                return;

            if (trackedHealthBar.healthBarView == null)
            {
                Unregister(entityId);
                return;
            }

            trackedHealthBar.healthBarView.SetHp(currentHp, maxHp);
        }

        public void Unregister(uint entityId)
        {
            if (!_trackedHealthBars.Remove(entityId, out var trackedHealthBar))
                return;

            if (trackedHealthBar.healthBarView != null)
                Object.Destroy(trackedHealthBar.healthBarView.gameObject);
        }

        public void Clear()
        {
            _trackedHealthBars.Clear();
            _removedEntityIds.Clear();

            if (_healthBarContainer == null)
                return;

            Object.Destroy(_healthBarContainer.gameObject);
            _healthBarContainer = null;
        }

        public void UpdatePositions()
        {
            if (!_isValid)
                return;

            if (_canvasTransform == null || _worldProjectionCamera == null || _healthBarContainer == null)
            {
                Clear();
                _isValid = false;
                return;
            }

            _removedEntityIds.Clear();

            foreach (var (entityId, trackedHealthBar) in _trackedHealthBars)
            {
                if (trackedHealthBar.targetTransform == null || trackedHealthBar.healthBarView == null)
                {
                    _removedEntityIds.Add(entityId);
                    continue;
                }

                var worldPosition = trackedHealthBar.targetTransform.TransformPoint(trackedHealthBar.localOffset);
                var screenPosition = _worldProjectionCamera.WorldToScreenPoint(worldPosition);
                bool isVisible = screenPosition.z > 0f;
                trackedHealthBar.healthBarView.SetVisible(isVisible);

                if (!isVisible)
                    continue;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(_healthBarContainer, screenPosition, null,
                    out var localPosition);
                trackedHealthBar.rectTransform.anchoredPosition = localPosition;
            }

            foreach (var entityId in _removedEntityIds)
            {
                Unregister(entityId);
            }
        }

        private GameObject GetHealthBarPrefab()
        {
            if (_healthBarPrefab == null)
                _healthBarPrefab = Resources.Load<GameObject>(HealthBarPrefabPath);

            if (_healthBarPrefab == null)
                Debug.LogError($"Health bar prefab not found at Resources/{HealthBarPrefabPath}.");

            return _healthBarPrefab;
        }

        private bool EnsureHealthBarContainer()
        {
            if (_healthBarContainer != null)
                return true;

            return CreateHealthBarContainer();
        }

        private bool CreateHealthBarContainer()
        {
            if (_canvasTransform == null)
                return false;

            try
            {
                var containerObject = new GameObject(HealthBarContainerName, typeof(RectTransform));
                _healthBarContainer = containerObject.GetComponent<RectTransform>();
                _healthBarContainer.SetParent(_canvasTransform, false);
                _healthBarContainer.anchorMin = Vector2.zero;
                _healthBarContainer.anchorMax = Vector2.one;
                _healthBarContainer.offsetMin = Vector2.zero;
                _healthBarContainer.offsetMax = Vector2.zero;
                _healthBarContainer.pivot = new Vector2(0.5f, 0.5f);

                // Keep dynamic health bars behind the Canvas' scene-authored result UI.
                _healthBarContainer.SetAsFirstSibling();
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"HealthBarOverlay: failed to create health bar container. {exception.Message}");
                _healthBarContainer = null;
                return false;
            }
        }
    }
}
