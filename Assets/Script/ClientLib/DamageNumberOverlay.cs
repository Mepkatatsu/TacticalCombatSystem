using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Script.ClientLib
{
    /// <summary>
    /// Owns short-lived damage number UI independently from entity views.
    /// The small local pool deliberately stays private until other transient combat UI needs the same policy.
    /// </summary>
    public class DamageNumberOverlay
    {
        private const string DamageNumberContainerName = "DamageNumberOverlay";
        private const string DamageNumberPrefabPath = "Prefabs/DamageNumber";
        private const int InitialPoolSize = 20;
        private const int MaximumPoolSize = 100;
        private const float LifetimeSeconds = 0.6f;
        private const float AppearanceSeconds = 0.1f;
        private const float FadeStartSeconds = 0.3f;
        private const float RiseDistance = 22f;
        private const float HorizontalOffsetDistance = 14f;

        private readonly RectTransform _canvasTransform;
        private readonly Camera _worldProjectionCamera;
        private readonly Stack<DamageNumberInstance> _availableInstances = new();
        private readonly List<DamageNumberInstance> _activeInstances = new();

        private RectTransform _damageNumberContainer;
        private GameObject _damageNumberPrefab;
        private bool _damageNumberPrefabLoadAttempted;
        private int _createdInstanceCount;
        private int _nextHorizontalOffsetIndex;
        private bool _isValid;
        private bool _acceptNewNumbers = true;

        private class DamageNumberInstance
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public Text text;
            public CanvasGroup canvasGroup;
            public Vector2 startPosition;
            public float elapsedSeconds;
        }

        public DamageNumberOverlay(Canvas canvas, Camera worldProjectionCamera)
        {
            if (canvas == null)
            {
                Debug.LogError("DamageNumberOverlay: Canvas is not assigned.");
                return;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogError("DamageNumberOverlay: Canvas must use Screen Space - Overlay mode.");
                return;
            }

            if (worldProjectionCamera == null)
            {
                Debug.LogError("DamageNumberOverlay: world projection camera is not assigned.");
                return;
            }

            _canvasTransform = canvas.transform as RectTransform;
            _worldProjectionCamera = worldProjectionCamera;

            if (_canvasTransform == null)
            {
                Debug.LogError("DamageNumberOverlay: Canvas does not have a RectTransform.");
                return;
            }

            _isValid = CreateDamageNumberContainer();
            if (!_isValid)
                return;

            Prewarm();
        }

        public void Show(Transform targetTransform, Vector3 localOffset, uint damage)
        {
            if (!_isValid || !_acceptNewNumbers || targetTransform == null)
                return;

            if (!EnsureDamageNumberContainer())
                return;

            var screenPosition = _worldProjectionCamera.WorldToScreenPoint(targetTransform.TransformPoint(localOffset));
            if (screenPosition.z <= 0f)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_damageNumberContainer, screenPosition, null,
                    out var localPosition))
                return;

            var instance = GetInstance();
            if (instance == null)
                return;

            // Spread rapid hits across left, center, and right to keep each value readable.
            int horizontalOffsetIndex = _nextHorizontalOffsetIndex++ % 3;
            float horizontalOffset = (horizontalOffsetIndex - 1) * HorizontalOffsetDistance;

            instance.startPosition = localPosition + new Vector2(horizontalOffset, 0f);
            instance.elapsedSeconds = 0f;
            instance.rectTransform.anchoredPosition = instance.startPosition;
            instance.rectTransform.localScale = Vector3.one * 0.85f;
            instance.text.text = damage.ToString();
            instance.canvasGroup.alpha = 1f;
            instance.gameObject.SetActive(true);
            _activeInstances.Add(instance);
        }

        public void StopAcceptingNewNumbers()
        {
            _acceptNewNumbers = false;
        }

        public void Update(float deltaTime)
        {
            if (!_isValid)
                return;

            if (_canvasTransform == null || _worldProjectionCamera == null || _damageNumberContainer == null)
            {
                Clear();
                _isValid = false;
                return;
            }

            for (int index = _activeInstances.Count - 1; index >= 0; --index)
            {
                var instance = _activeInstances[index];
                instance.elapsedSeconds += deltaTime;

                if (instance.elapsedSeconds >= LifetimeSeconds)
                {
                    _activeInstances.RemoveAt(index);
                    Return(instance);
                    continue;
                }

                UpdatePresentation(instance);
            }
        }

        public void Clear()
        {
            _activeInstances.Clear();
            _availableInstances.Clear();
            _createdInstanceCount = 0;
            _nextHorizontalOffsetIndex = 0;
            _acceptNewNumbers = false;
            _isValid = false;

            if (_damageNumberContainer == null)
                return;

            UnityEngine.Object.Destroy(_damageNumberContainer.gameObject);
            _damageNumberContainer = null;
        }

        private void Prewarm()
        {
            for (int index = 0; index < InitialPoolSize; ++index)
            {
                var instance = CreateInstance();
                if (instance == null)
                    return;

                Return(instance);
            }
        }

        private DamageNumberInstance GetInstance()
        {
            while (_availableInstances.Count > 0)
            {
                var instance = _availableInstances.Pop();
                if (instance?.gameObject != null)
                    return instance;
            }

            if (_createdInstanceCount < MaximumPoolSize)
                return CreateInstance();

            if (_activeInstances.Count == 0)
                return null;

            var oldestInstance = _activeInstances[0];
            _activeInstances.RemoveAt(0);
            return oldestInstance;
        }

        private DamageNumberInstance CreateInstance()
        {
            if (_damageNumberContainer == null)
                return null;

            try
            {
                var damageNumberPrefab = GetDamageNumberPrefab();
                if (damageNumberPrefab == null)
                    return null;

                var numberObject = UnityEngine.Object.Instantiate(damageNumberPrefab, _damageNumberContainer, false);
                var rectTransform = numberObject.GetComponent<RectTransform>();
                var text = numberObject.GetComponent<Text>();
                var canvasGroup = numberObject.GetComponent<CanvasGroup>();
                if (rectTransform == null || text == null || canvasGroup == null)
                {
                    Debug.LogError("DamageNumberOverlay: DamageNumber prefab requires RectTransform, Text, and CanvasGroup components.");
                    UnityEngine.Object.Destroy(numberObject);
                    return null;
                }

                var instance = new DamageNumberInstance
                {
                    gameObject = numberObject,
                    rectTransform = rectTransform,
                    text = text,
                    canvasGroup = canvasGroup,
                };
                _createdInstanceCount++;
                return instance;
            }
            catch (Exception exception)
            {
                Debug.LogError($"DamageNumberOverlay: failed to create damage number UI. {exception.Message}");
                return null;
            }
        }

        private GameObject GetDamageNumberPrefab()
        {
            if (!_damageNumberPrefabLoadAttempted)
            {
                _damageNumberPrefab = Resources.Load<GameObject>(DamageNumberPrefabPath);
                _damageNumberPrefabLoadAttempted = true;
            }

            if (_damageNumberPrefab == null)
                Debug.LogError($"DamageNumberOverlay: damage number prefab not found at Resources/{DamageNumberPrefabPath}.");

            return _damageNumberPrefab;
        }

        private void Return(DamageNumberInstance instance)
        {
            if (instance?.gameObject == null)
                return;

            instance.gameObject.SetActive(false);
            _availableInstances.Push(instance);
        }

        private static void UpdatePresentation(DamageNumberInstance instance)
        {
            float normalizedTime = Mathf.Clamp01(instance.elapsedSeconds / LifetimeSeconds);
            float rise = Mathf.Lerp(0f, RiseDistance, normalizedTime);
            instance.rectTransform.anchoredPosition = instance.startPosition + Vector2.up * rise;

            float scale = instance.elapsedSeconds < AppearanceSeconds
                ? Mathf.Lerp(0.85f, 1.05f, instance.elapsedSeconds / AppearanceSeconds)
                : Mathf.Lerp(1.05f, 1f, (instance.elapsedSeconds - AppearanceSeconds) /
                    (LifetimeSeconds - AppearanceSeconds));
            instance.rectTransform.localScale = Vector3.one * scale;

            instance.canvasGroup.alpha = instance.elapsedSeconds < FadeStartSeconds
                ? 1f
                : Mathf.Lerp(1f, 0f, (instance.elapsedSeconds - FadeStartSeconds) /
                    (LifetimeSeconds - FadeStartSeconds));
        }

        private bool EnsureDamageNumberContainer()
        {
            if (_damageNumberContainer != null)
                return true;

            return CreateDamageNumberContainer();
        }

        private bool CreateDamageNumberContainer()
        {
            if (_canvasTransform == null)
                return false;

            try
            {
                var containerObject = new GameObject(DamageNumberContainerName, typeof(RectTransform));
                _damageNumberContainer = containerObject.GetComponent<RectTransform>();
                _damageNumberContainer.SetParent(_canvasTransform, false);
                _damageNumberContainer.anchorMin = Vector2.zero;
                _damageNumberContainer.anchorMax = Vector2.one;
                _damageNumberContainer.offsetMin = Vector2.zero;
                _damageNumberContainer.offsetMax = Vector2.zero;
                _damageNumberContainer.pivot = new Vector2(0.5f, 0.5f);

                // Scene-authored result UI must remain above transient damage numbers.
                _damageNumberContainer.SetAsFirstSibling();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"DamageNumberOverlay: failed to create damage number container. {exception.Message}");
                _damageNumberContainer = null;
                return false;
            }
        }
    }
}
