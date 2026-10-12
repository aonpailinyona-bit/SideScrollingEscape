using UnityEngine;

namespace SideScrollingEscape.Environment
{
    /// <summary>
    /// Marks a climbable ladder. Requires a trigger <see cref="Collider2D"/> that
    /// defines the climbable volume. The player's controller detects this
    /// component on trigger overlap and enters/exits its climbing state.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Ladder : MonoBehaviour
    {
        private Collider2D climbCollider;

        /// <summary>The collider that defines the climbable area of this ladder.</summary>
        public Collider2D ClimbCollider
        {
            get
            {
                if (climbCollider == null)
                {
                    climbCollider = GetComponent<Collider2D>();
                }
                return climbCollider;
            }
        }

        /// <summary>World X position the player snaps to while climbing.</summary>
        public float CenterX => ClimbCollider != null ? ClimbCollider.bounds.center.x : transform.position.x;

        /// <summary>World Y of the bottom of the climbable area (where climb-down ends).</summary>
        public float BottomY => ClimbCollider != null ? ClimbCollider.bounds.min.y : transform.position.y;

        /// <summary>World Y of the top of the climbable area.</summary>
        public float TopY => ClimbCollider != null ? ClimbCollider.bounds.max.y : transform.position.y;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Awake()
        {
            climbCollider = GetComponent<Collider2D>();
            if (climbCollider != null)
            {
                climbCollider.isTrigger = true;
            }
        }
    }
}
