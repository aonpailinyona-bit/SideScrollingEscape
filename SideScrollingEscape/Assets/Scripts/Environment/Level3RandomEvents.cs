using System.Collections.Generic;
using SideScrollingEscape.Items;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SideScrollingEscape.Environment
{
    /// <summary>Randomizes and evenly spreads Level 3 keys and traps along walkable ground.</summary>
    public sealed class Level3RandomEvents : MonoBehaviour
    {
        private readonly List<Vector2> lowerGround = new List<Vector2>();
        private readonly List<Vector2> upperGround = new List<Vector2>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Level3" || FindAnyObjectByType<Level3RandomEvents>() != null) return;
            new GameObject("Level3_RandomLayout").AddComponent<Level3RandomEvents>();
        }

        private void Start()
        {
            BuildGroundPools();
            RandomizeKeysAndTraps();
        }

        private void BuildGroundPools()
        {
            float[] sampleX = { 6f, 13f, 20f, 27f, 34f, 42f, 50f, 58f, 66f, 74f, 83f, 93f };
            foreach (float x in sampleX)
            {
                if (TryFindFloor(x, 3.2f, -1.5f, out Vector2 lower)) lowerGround.Add(lower);
                if (TryFindFloor(x, 48.2f, 43.2f, out Vector2 upper)) upperGround.Add(upper);
            }

            lowerGround.Sort((a, b) => a.x.CompareTo(b.x));
            upperGround.Sort((a, b) => a.x.CompareTo(b.x));
        }

        private static bool TryFindFloor(float x, float rayStartY, float minimumY, out Vector2 floor)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, rayStartY), Vector2.down, rayStartY - minimumY);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger || hit.normal.y < 0.65f) continue;
                if (hit.point.y > rayStartY - 2f) continue;
                Rigidbody2D rigidbody = hit.collider.attachedRigidbody;
                if (rigidbody != null && rigidbody.bodyType != RigidbodyType2D.Static) continue;
                floor = hit.point;
                return true;
            }

            floor = default;
            return false;
        }

        private void RandomizeKeysAndTraps()
        {
            ItemPickup[] allPickups = FindObjectsByType<ItemPickup>();
            var keys = new List<ItemPickup>();
            foreach (ItemPickup pickup in allPickups)
                if (pickup != null && pickup.ItemData != null && pickup.ItemData.IsKeyItem) keys.Add(pickup);

            Level3NoiseTrap[] traps = FindObjectsByType<Level3NoiseTrap>();
            int lowerKeyCount = Mathf.CeilToInt(keys.Count * 0.6f);
            int upperKeyCount = keys.Count - lowerKeyCount;
            int lowerTrapCount = Mathf.CeilToInt(traps.Length * 0.6f);
            int upperTrapCount = traps.Length - lowerTrapCount;

            List<Vector2> lowerSlots = PickEvenlySpreadSlots(lowerGround, lowerKeyCount + lowerTrapCount);
            List<Vector2> upperSlots = PickEvenlySpreadSlots(upperGround, upperKeyCount + upperTrapCount);
            var lowerKeySlots = new List<Vector2>();
            var lowerTrapSlots = new List<Vector2>();
            SplitAlternating(lowerSlots, lowerKeyCount, lowerKeySlots, lowerTrapSlots);
            var upperKeySlots = new List<Vector2>();
            var upperTrapSlots = new List<Vector2>();
            SplitAlternating(upperSlots, upperKeyCount, upperKeySlots, upperTrapSlots);

            Shuffle(keys);
            Shuffle(traps);
            PlaceKeys(keys, lowerKeySlots, upperKeySlots, lowerKeyCount);
            PlaceTraps(traps, lowerTrapSlots, upperTrapSlots, lowerTrapCount);
        }

        private static List<Vector2> PickEvenlySpreadSlots(List<Vector2> pool, int count)
        {
            var result = new List<Vector2>();
            if (pool.Count == 0 || count <= 0) return result;

            // Split the entire map width into equal bands and choose one valid
            // floor location from each band, preventing clusters.
            for (int band = 0; band < count; band++)
            {
                int start = Mathf.FloorToInt(band * pool.Count / (float)count);
                int end = Mathf.Max(start + 1, Mathf.FloorToInt((band + 1) * pool.Count / (float)count));
                int chosen = Random.Range(start, Mathf.Min(end, pool.Count));
                result.Add(pool[chosen]);
            }
            return result;
        }

        private static void SplitAlternating(List<Vector2> slots, int keyCount,
            List<Vector2> keySlots, List<Vector2> trapSlots)
        {
            bool keyFirst = Random.value >= 0.5f;
            int trapCount = slots.Count - keyCount;
            foreach (Vector2 slot in slots)
            {
                bool alternatingKey = ((keySlots.Count + trapSlots.Count) % 2 == 0) == keyFirst;
                bool takeKey = keySlots.Count < keyCount && (trapSlots.Count >= trapCount || alternatingKey);
                if (takeKey) keySlots.Add(slot);
                else trapSlots.Add(slot);
            }
        }

        private static void PlaceKeys(List<ItemPickup> keys, List<Vector2> lower, List<Vector2> upper, int lowerCount)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                bool useLower = i < lowerCount;
                List<Vector2> slots = useLower ? lower : upper;
                int index = useLower ? i : i - lowerCount;
                if (index >= slots.Count) continue;
                Vector2 point = slots[index];
                keys[i].SetSpawnPosition(new Vector3(point.x, point.y + 0.68f, 0f));
            }
        }

        private static void PlaceTraps(Level3NoiseTrap[] traps, List<Vector2> lower, List<Vector2> upper, int lowerCount)
        {
            for (int i = 0; i < traps.Length; i++)
            {
                bool useLower = i < lowerCount;
                List<Vector2> slots = useLower ? lower : upper;
                int index = useLower ? i : i - lowerCount;
                if (index >= slots.Count) continue;
                Vector2 point = slots[index];
                traps[i].transform.position = new Vector3(point.x, point.y + 0.2f, 0f);
            }
        }

        private static void Shuffle<T>(IList<T> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = values[i];
                values[i] = values[j];
                values[j] = temp;
            }
        }
    }
}
