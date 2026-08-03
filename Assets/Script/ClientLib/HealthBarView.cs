using Script.CommonLib;
using UnityEngine;
using UnityEngine.UI;

namespace Script.ClientLib
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private RawImage _fillImage;
        [SerializeField] private RectTransform _fillMaskTransform;
        [SerializeField] private Color _blueTeamColor = new(0.61f, 0.94f, 0.22f, 1f);
        [SerializeField] private Color _redTeamColor = new(0.96f, 0.32f, 0.38f, 1f);

        public void Initialize(uint currentHp, uint maxHp, TeamFlag teamFlag)
        {
            _fillImage.color = teamFlag == TeamFlag.Red ? _redTeamColor : _blueTeamColor;
            SetHp(currentHp, maxHp);
            SetVisible(true);
        }

        public void SetHp(uint currentHp, uint maxHp)
        {
            float healthRatio = maxHp == 0 ? 0f : Mathf.Clamp01(currentHp / (float)maxHp);
            _fillMaskTransform.anchorMax = new Vector2(healthRatio, 1f);
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        private void LateUpdate()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
                transform.rotation = mainCamera.transform.rotation;
        }
    }
}
