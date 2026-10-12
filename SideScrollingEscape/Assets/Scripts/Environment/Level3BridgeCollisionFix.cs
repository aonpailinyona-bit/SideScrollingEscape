using UnityEngine;
using UnityEngine.SceneManagement;

namespace SideScrollingEscape.Environment
{
    internal static class Level3BridgeCollisionFix
    {
        private const string ColliderName = "Level3_SmoothBridgeCollider";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateSmoothBridgeCollider()
        {
            // Recovery scenes use a temporary scene name, so also identify
            // Level 3 by its ground tilemap.
            if (SceneManager.GetActiveScene().name != "Level3" && GameObject.Find("Tilemap_2") == null) return;
            if (GameObject.Find(ColliderName) != null) return;

            var bridgeColliderObject = new GameObject(ColliderName);
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) bridgeColliderObject.layer = groundLayer;

            var edge = bridgeColliderObject.AddComponent<EdgeCollider2D>();
            edge.edgeRadius = 0.04f;

            const float startX = 12f;
            const float endX = 29f;
            const float spacing = 0.1f;
            int pointCount = Mathf.RoundToInt((endX - startX) / spacing) + 1;
            var points = new Vector2[pointCount];

            for (int index = 0; index < pointCount; index++)
            {
                float x = index == pointCount - 1 ? endX : startX + index * spacing;
                points[index] = new Vector2(x, GetBridgeHeight(x));
            }

            edge.points = points;
        }

        private static float GetBridgeHeight(float x)
        {
            // Match only the visible stair footprint. Extending this farther
            // makes the player appear to stand in mid-air beside the bridge.
            const float rampStart = 16f;
            const float plateauStart = 19f;
            const float plateauEnd = 22f;
            const float rampEnd = 25f;
            const float bridgeHeight = 2.15f;

            if (x <= rampStart || x >= rampEnd) return 0f;
            if (x < plateauStart)
            {
                float t = Mathf.InverseLerp(rampStart, plateauStart, x);
                return bridgeHeight * SmoothLinearRamp(t);
            }

            if (x <= plateauEnd) return bridgeHeight;

            float downT = Mathf.InverseLerp(plateauEnd, rampEnd, x);
            return Mathf.Lerp(bridgeHeight, 0f, downT);
        }

        private static float SmoothLinearRamp(float t)
        {
            // Round only the ends of the ramp. The long linear middle keeps
            // the slope walkable, unlike SmoothStep which becomes too steep.
            const float blend = 0.15f;
            float scale = 1f / (1f - blend);

            if (t < blend)
                return scale * t * t / (2f * blend);

            if (t > 1f - blend)
            {
                float remaining = 1f - t;
                return 1f - scale * remaining * remaining / (2f * blend);
            }

            return scale * (t - blend * 0.5f);
        }
    }
}
