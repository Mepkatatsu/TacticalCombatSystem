using UnityEngine;

namespace Script.ClientLib
{
    public class EntityView : MonoBehaviour
    {
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int IsAttack = Animator.StringToHash("IsAttack");
        private static readonly int IsRetired = Animator.StringToHash("IsRetired");
        
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        private static readonly int AttackIndex = Animator.StringToHash("AttackIndex");

        public uint Hp { get; private set; }
        
        private Animator _animator;
        private Animator Animator => _animator ??= GetComponent<Animator>();

        private ushort _attackDelayMs;
        private uint _comboMs;
        private byte _nextAttack = 0;

        public void OnUpdate(ushort deltaMs)
        {
            if (_comboMs <= deltaMs)
            {
                _comboMs = 0;
            }
            else
            {
                _comboMs -= deltaMs;
            }
        }

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
            if (_nextAttack == 0 || _comboMs <= 0)
            {
                Animator.SetInteger(AttackIndex, 0);
                _nextAttack = 1;
            }
            else if (_nextAttack == 1)
            {
                Animator.SetInteger(AttackIndex, 1);
                _nextAttack = 2;
            }
            else if (_nextAttack == 2)
            {
                Animator.SetInteger(AttackIndex, 2);
                _nextAttack = 0;
            }

            _comboMs = (uint)_attackDelayMs * GameParameterSettings.Instance.ComboMaintainMs;
        }

        public void OnRetired()
        {
            Animator.SetTrigger(IsRetired);
        }
        
        public void OnMoveSpeedChanged(ushort moveSpeed)
        {
            float animationMoveSpeed = moveSpeed / (float)GameParameterSettings.Instance.DefaultMoveSpeed;
            
            Animator.SetFloat(MoveSpeed, animationMoveSpeed);
        }

        public void OnAttackDelayMsChanged(ushort basisAttackDelayMs, ushort attackDelayMs)
        {
            _attackDelayMs = attackDelayMs;
            
            float animationAttackSpeed = (float)basisAttackDelayMs / attackDelayMs;
            
            Animator.SetFloat(AttackSpeed, animationAttackSpeed);
        }
    }
}
