using UnityEngine;

namespace Script.ClientLib
{
    public class EntityView : MonoBehaviour
    {
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        
        private Animator _animator;
        private Animator Animator => _animator ??= GetComponent<Animator>();

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

        public void OnStartMoving()
        {
            Animator.SetBool(IsMoving, true);
        }
        
        public void OnStopMoving()
        {
            Animator.SetBool(IsMoving, false);
        }
    }
}
