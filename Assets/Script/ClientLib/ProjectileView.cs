using UnityEngine;

namespace Script.ClientLib
{
    public class ProjectileView : MonoBehaviour
    {
        public void OnPositionChanged(Vector3 position)
        {
            transform.position = position;
        }
    
        public void OnDirectionChanged(Vector3 position)
        {
            if (position == Vector3.zero)
                return;
            
            transform.rotation = Quaternion.LookRotation(position);
        }
    }
}
