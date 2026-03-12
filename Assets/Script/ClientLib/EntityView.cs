using UnityEngine;

namespace Script.ClientLib
{
    public class EntityView : MonoBehaviour
    {
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int IsAttack = Animator.StringToHash("IsAttack");
        private static readonly int IsRetired = Animator.StringToHash("IsRetired");

        public uint Hp { get; private set; }
        
        private Animator _animator;
        private Animator Animator => _animator ??= GetComponent<Animator>();

        public void SetHp(uint hp)
        {
            Hp = hp;
        }
        
        public void GetDamage(uint damage)
        {
            if (damage >= Hp)
            {
                Hp = 0;
            }
            else
            {
                Hp -= damage;
            }
        }

        public void OnPositionChanged(Vector3 position)
        {
            transform.position = position;
        }
    
        public void OnDirectionChanged(Vector3 dir)
        {
            if (dir == Vector3.zero)
                return;
            
            transform.rotation = Quaternion.LookRotation(dir);
        }

        public void OnStartMoving()
        {
            Animator.SetBool(IsMoving, true);
        }
        
        public void OnStopMoving()
        {
            Animator.SetBool(IsMoving, false);
        }

        public void OnStartAttack()
        {
            Animator.SetTrigger(IsAttack);
        }

        public void OnRetired()
        {
            Animator.SetTrigger(IsRetired);
        }
    }
}
