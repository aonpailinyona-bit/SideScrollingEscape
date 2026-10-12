using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace SideScrollingEscape.Environment
{
    /// <summary>
    /// Simulates a weak, dying candle light that flickers irregularly and occasionally almost goes out.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class LightFlicker : MonoBehaviour
    {
        private Light2D light2D;
        private float logTimer = 0f;

        private void Awake()
        {
            light2D = GetComponent<Light2D>();
            if (light2D == null)
            {
                Debug.LogError("[LightFlicker] Light2D component is null in Awake!", this);
            }
            else
            {
                Debug.Log("[LightFlicker] Successfully initialized Light2D in Awake.", this);
            }
        }

        private void Start()
        {
            if (light2D != null)
            {
                StartCoroutine(FlickerRoutine());
            }
        }

        private IEnumerator FlickerRoutine()
        {
            while (true)
            {
                float targetIntensity;
                float duration;

                // Occasionally drop to near zero (0.02) briefly for 0.03-0.08 seconds to simulate almost going out
                // Let's use a 15% chance for this dramatic drop
                if (Random.value < 0.15f)
                {
                    targetIntensity = 0.02f;
                    duration = Random.Range(0.03f, 0.08f);
                }
                else
                {
                    // Randomly change light intensity between 0.15 and 0.5
                    targetIntensity = Random.Range(0.15f, 0.5f);
                    
                    // Irregular timing: 0.05 to 0.2 seconds
                    duration = Random.Range(0.05f, 0.2f);
                }

                light2D.intensity = targetIntensity;

                // Debug print approx once per second
                logTimer += duration;
                if (logTimer >= 1.0f)
                {
                    Debug.Log(string.Format("[LightFlicker] Active intensity: {0:F3}", light2D.intensity), this);
                    logTimer = 0f;
                }

                yield return new WaitForSeconds(duration);
            }
        }
    }
}
