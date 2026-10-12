using System.Collections;
using SideScrollingEscape.Audio;
using SideScrollingEscape.Core;
using SideScrollingEscape.Enemy;
using SideScrollingEscape.Player;
using UnityEngine;

namespace SideScrollingEscape.Items
{
    /// <summary>A Level 2 statue that either cleanses petrification or reveals itself as cursed.</summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class CurseStatue : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool isFake;
        [SerializeField] private Sprite cleanStatueSprite;
        [SerializeField] private Sprite cursedStatueSprite;
        [SerializeField] private float fakeCurseAmount = 35f;
        [SerializeField] private AudioClip cleanseClip;
        [SerializeField] private AudioClip cursedClip;

        private SpriteRenderer statueRenderer;
        private bool used;

        private void Awake()
        {
            statueRenderer = GetComponent<SpriteRenderer>();
            if (statueRenderer != null && cleanStatueSprite != null)
                statueRenderer.sprite = cleanStatueSprite;
        }

        public bool CanInteract(GameObject interactor)
        {
            PlayerController player = interactor.GetComponent<PlayerController>();
            return !used && player != null && !player.IsFrozen && player.CurrentCurse > 0.01f;
        }

        public void Interact(GameObject interactor)
        {
            PlayerController player = interactor.GetComponent<PlayerController>();
            if (used || player == null || player.IsFrozen || player.CurrentCurse <= 0.01f) return;

            used = true;
            if (isFake)
            {
                if (statueRenderer != null && cursedStatueSprite != null)
                    statueRenderer.sprite = cursedStatueSprite;
                player.ApplyStatueCurse(fakeCurseAmount);
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySfxAt(cursedClip, transform.position, 0.78f, 0.78f);

                foreach (KillerAI killer in FindObjectsByType<KillerAI>())
                    killer.HearNoise(transform.position, 2.8f);
            }
            else
            {
                player.CleanseCurse();
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySfxAt(cleanseClip, transform.position, 0.62f, 1.25f);
            }


            StartCoroutine(PlayResultVisual());
        }

        public string GetPrompt()
        {
            if (used) return isFake ? "The statue was cursed" : "The statue is dormant";
            return "Press E to cleanse the curse";
        }

        private IEnumerator PlayResultVisual()
        {
            if (statueRenderer == null) yield break;
            Vector3 basePosition = transform.position;
            Vector3 baseScale = transform.localScale;
            Color flashColor = isFake
                ? new Color(1f, 0.08f, 0.08f, 1f)
                : new Color(0.25f, 0.9f, 1f, 1f);

            const float duration = 0.55f;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                transform.localScale = baseScale * (1f + pulse * 0.12f);
                if (isFake)
                    transform.position = basePosition + Vector3.right * Mathf.Sin(t * 55f) * pulse * 0.08f;
                statueRenderer.color = Color.Lerp(Color.white, flashColor, pulse);
                yield return null;
            }

            transform.position = basePosition;
            transform.localScale = baseScale;
            statueRenderer.color = isFake
                ? new Color(0.82f, 0.28f, 0.28f, 1f)
                : new Color(0.55f, 0.72f, 0.76f, 1f);
        }
    }
}
