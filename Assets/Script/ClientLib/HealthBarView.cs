using Script.CommonLib;
using UnityEngine;
using UnityEngine.UI;

namespace Script.ClientLib
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _blueTeamColor = new(0.25f, 0.64f, 1f, 1f);
        [SerializeField] private Color _redTeamColor = new(1f, 0.31f, 0.31f, 1f);

        public void Initialize(uint currentHp, uint maxHp, TeamFlag teamFlag)
        {
            _fillImage.color = teamFlag == TeamFlag.Red ? _redTeamColor : _blueTeamColor;
            SetHp(currentHp, maxHp);
            SetVisible(true);
        }

        public void SetHp(uint currentHp, uint maxHp)
        {
            _fillImage.fillAmount = maxHp == 0 ? 0f : Mathf.Clamp01(currentHp / (float)maxHp);
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
