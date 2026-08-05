using System;
using System.Collections.Generic;
using Script.CommonLib;
using UnityEngine;
using UnityEngine.UI;

namespace Script.ClientLib
{
    /// <summary>
    /// EntityView와 독립적으로 짧은 수명의 피해 숫자 UI를 관리한다.
    /// 다른 일시적 전투 UI에도 같은 정책이 필요해질 때까지 작은 로컬 풀은 private으로 유지한다.
    /// </summary>
    public class DamageNumberOverlay
    {
        private const string DamageNumberContainerName = "DamageNumberOverlay";
        private const string DamageNumberPrefabPath = "Prefabs/DamageNumber";
        private const int InitialPoolSize = 20;
        private const int MaximumPoolSize = 100;
        private const float LifetimeSeconds = 0.45f;
        private const float PopPeakSeconds = 0.08f;
        private const float SettleSeconds = 0.15f;
        private const float MovementDurationSeconds = 0.32f;
        private const float FadeStartSeconds = MovementDurationSeconds;
        private const float FadeDurationSeconds = LifetimeSeconds - FadeStartSeconds;
        private const float RiseDistance = 52f;
        private const float HorizontalDriftDistance = 30f;
        private const float InitialScale = 0.78f;
        private const float PeakScale = 1.10f;

        private static readonly float[] BlueTeamHorizontalInitialOffsets =
        {
            -34f,
            -29f,
            -24f,
            -19f,
            -14f,
            -9f,
            -4f,
        };

        private static readonly float[] VerticalInitialOffsets =
        {
            -14f,
            -8f,
            -2f,
            4f,
            10f,
            16f,
            22f,
        };

        private readonly RectTransform _canvasTransform;
        private readonly Camera _worldProjectionCamera;
        private readonly Stack<DamageNumberInstance> _availableInstances = new();
        private readonly List<DamageNumberInstance> _activeInstances = new();

        private RectTransform _damageNumberContainer;
        private GameObject _damageNumberPrefab;
        private bool _damageNumberPrefabLoadAttempted;
        private int _createdInstanceCount;
        private readonly System.Random _presentationRandom = new();
        private bool _isValid;
        private bool _acceptNewNumbers = true;

        private class DamageNumberInstance
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public Text text;
            public CanvasGroup canvasGroup;
            public Vector2 startPosition;
            public Vector2 travelOffset;
            public float elapsedSeconds;
        }

        public DamageNumberOverlay(Canvas canvas, Camera worldProjectionCamera)
        {
            if (!canvas)
            {
                Debug.LogError("DamageNumberOverlay: Canvas is not assigned.");
                return;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogError("DamageNumberOverlay: Canvas must use Screen Space - Overlay mode.");
                return;
            }

            if (!worldProjectionCamera)
            {
                Debug.LogError("DamageNumberOverlay: world projection camera is not assigned.");
                return;
            }

            _canvasTransform = canvas.transform as RectTransform;
            _worldProjectionCamera = worldProjectionCamera;

            if (!_canvasTransform)
            {
                Debug.LogError("DamageNumberOverlay: Canvas does not have a RectTransform.");
                return;
            }

            _isValid = CreateDamageNumberContainer();
            if (!_isValid)
                return;

            Prewarm();
        }

        public void Show(Transform targetTransform, Vector3 localOffset, TeamFlag teamFlag, uint damage)
        {
            if (!_isValid || !_acceptNewNumbers || !targetTransform)
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

            instance.startPosition = localPosition + GetInitialOffset(teamFlag);
            instance.travelOffset = GetTravelOffset(teamFlag);
            instance.elapsedSeconds = 0f;
            instance.rectTransform.anchoredPosition = instance.startPosition;
            instance.rectTransform.localScale = Vector3.one * InitialScale;
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

            if (!_canvasTransform || !_worldProjectionCamera || !_damageNumberContainer)
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
            _acceptNewNumbers = false;
            _isValid = false;

            if (!_damageNumberContainer)
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
                if (instance != null && instance.gameObject)
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
            if (!_damageNumberContainer)
                return null;

            try
            {
                var damageNumberPrefab = GetDamageNumberPrefab();
                if (!damageNumberPrefab)
                    return null;

                var numberObject = UnityEngine.Object.Instantiate(damageNumberPrefab, _damageNumberContainer, false);
                var rectTransform = numberObject.GetComponent<RectTransform>();
                var text = numberObject.GetComponent<Text>();
                var canvasGroup = numberObject.GetComponent<CanvasGroup>();
                if (!rectTransform || !text || !canvasGroup)
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

            if (!_damageNumberPrefab)
                Debug.LogError($"DamageNumberOverlay: damage number prefab not found at Resources/{DamageNumberPrefabPath}.");

            return _damageNumberPrefab;
        }

        private void Return(DamageNumberInstance instance)
        {
            if (instance == null || !instance.gameObject)
                return;

            instance.gameObject.SetActive(false);
            _availableInstances.Push(instance);
        }

        private static void UpdatePresentation(DamageNumberInstance instance)
        {
            float movementProgress = Mathf.Clamp01(instance.elapsedSeconds / MovementDurationSeconds);
            float travelProgress = 1f - Mathf.Pow(1f - movementProgress, 2f);
            instance.rectTransform.anchoredPosition = instance.startPosition + instance.travelOffset * travelProgress;

            instance.rectTransform.localScale = Vector3.one * GetPopScale(instance.elapsedSeconds);

            instance.canvasGroup.alpha = instance.elapsedSeconds < FadeStartSeconds
                ? 1f
                : Mathf.Lerp(1f, 0f, (instance.elapsedSeconds - FadeStartSeconds) / FadeDurationSeconds);
        }

        private static float GetPopScale(float elapsedSeconds)
        {
            if (elapsedSeconds < PopPeakSeconds)
                return Mathf.Lerp(InitialScale, PeakScale, elapsedSeconds / PopPeakSeconds);

            if (elapsedSeconds < SettleSeconds)
                return Mathf.Lerp(PeakScale, 1f, (elapsedSeconds - PopPeakSeconds) /
                    (SettleSeconds - PopPeakSeconds));

            return 1f;
        }

        private Vector2 GetInitialOffset(TeamFlag teamFlag)
        {
            float horizontalOffset = BlueTeamHorizontalInitialOffsets[
                _presentationRandom.Next(BlueTeamHorizontalInitialOffsets.Length)];
            float verticalOffset = VerticalInitialOffsets[_presentationRandom.Next(VerticalInitialOffsets.Length)];

            if (teamFlag == TeamFlag.Blue)
                return new Vector2(horizontalOffset, verticalOffset);

            if (teamFlag == TeamFlag.Red)
                return new Vector2(-horizontalOffset, verticalOffset);

            // Draw 또는 미설정 팀에는 전투 진영 방향이 없으므로 중립적이고 읽기 쉬운 위치를 사용한다.
            return new Vector2(0f, verticalOffset);
        }

        private static Vector2 GetTravelOffset(TeamFlag teamFlag)
        {
            if (teamFlag == TeamFlag.Blue)
                return new Vector2(-HorizontalDriftDistance, RiseDistance);

            if (teamFlag == TeamFlag.Red)
                return new Vector2(HorizontalDriftDistance, RiseDistance);

            return Vector2.up * RiseDistance;
        }

        private bool EnsureDamageNumberContainer()
        {
            if (_damageNumberContainer)
                return true;

            return CreateDamageNumberContainer();
        }

        private bool CreateDamageNumberContainer()
        {
            if (!_canvasTransform)
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

                // Layer 순서: 0 = HealthBarOverlay, 1 = DamageNumberOverlay, 그 뒤에는 scene-authored result UI가 위치한다.
                // TODO: 체력바·피해 숫자·결과 UI 레이어를 명시적으로 관리하는 구조를 도입하면 하드코딩한 sibling index를 제거한다.
                _damageNumberContainer.SetSiblingIndex(1);
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
