using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Verwaltet lokale Character-Animation (nur für den Owner sichtbar).
    /// </summary>
    public class PlayerCharacterAnimator : MonoBehaviour
    {
        private Animator m_Animator;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            if (m_Animator == null)
            {
                Debug.LogError("[PlayerCharacterAnimator] Animator component not found!");
            }
        }

        /// <summary>
        /// Setzt Move-Animation Parameter.
        /// </summary>
        public void SetMoveDirection(Vector2 direction)
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetFloat(MoveXHash, direction.x);
            m_Animator.SetFloat(MoveYHash, direction.y);
        }

        /// <summary>
        /// Triggert Jump-Animation.
        /// </summary>
        public void SetJump()
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetBool(IsJumpingHash, true);
        }

        /// <summary>
        /// Setzt Sprint-Status.
        /// </summary>
        public void SetSprinting(bool isSprinting)
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetBool(IsSprintingHash, isSprinting);
        }

        /// <summary>
        /// Triggert Attack-Animation.
        /// </summary>
        public void SetAttack()
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetBool(IsAttackingHash, true);
        }

        /// <summary>
        /// Setzt Jump-Animation zurück.
        /// </summary>
        public void ResetJump()
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetBool(IsJumpingHash, false);
        }

        /// <summary>
        /// Setzt Attack-Animation zurück.
        /// </summary>
        public void ResetAttack()
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetBool(IsAttackingHash, false);
        }
    }
}
