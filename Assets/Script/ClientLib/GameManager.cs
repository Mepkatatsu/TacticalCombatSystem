using UnityEngine;

namespace Script.ClientLib
{
    public class GameManager : MonoBehaviour
    {
        public int targetFrameRate = 60;
    
        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
