using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SideScrollingEscape.Environment
{
    /// <summary>
    /// Jitters the intensity of a Light2D component to simulate a flickering torch.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class FlickeringLight2D : MonoBehaviour
    {
        [Tooltip("The base intensity of the light.")]
        [SerializeField] private float baseIntensity = 0.8f;

        [Tooltip("Maximum amount the intensity can deviate from base intensity.")]
        [SerializeField] private float flickerRange = 0.15f;

        [Tooltip("The speed of the flicker animation.")]
        [SerializeField] private float flickerSpeed = 10f;

        private Light2D light2D;
        private float noiseOffset;

        private void Awake()
        {
            light2D = GetComponent<Light2D>();
            noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (light2D == null) return;

            // Generate noise-based flicker for a smooth, organic flame look
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);
            light2D.intensity = baseIntensity + (noise * 2f - 1f) * flickerRange;
        }
    }
}
