using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class FitCameraBounds : EditorWindow
{
    [MenuItem("Tools/Fit Camera Bounds")]
    public static void FitBounds()
    {
        // ค้นหา PolygonCollider2D ที่น่าจะใช้เป็นขอบเขตกล้อง
        PolygonCollider2D poly = null;
        var polys = FindObjectsByType<PolygonCollider2D>();
        foreach (var p in polys)
        {
            if (p.isTrigger || p.name.ToLower().Contains("bound") || p.name.ToLower().Contains("confine"))
            {
                poly = p;
                break;
            }
        }

        if (poly == null && polys.Length > 0) poly = polys[0];

        if (poly == null)
        {
            Debug.LogError("ไม่พบ PolygonCollider2D สำหรับขอบเขตกล้องในฉาก");
            return;
        }

        bool foundBounds = false;
        Bounds totalBounds = new Bounds(Vector3.zero, Vector3.zero);

        // หาขนาดของ Tilemap ทั้งหมดในฉาก
        TilemapRenderer[] tilemaps = FindObjectsByType<TilemapRenderer>();
        if (tilemaps.Length > 0)
        {
            totalBounds = tilemaps[0].bounds;
            foundBounds = true;
            foreach (var tm in tilemaps)
            {
                totalBounds.Encapsulate(tm.bounds);
            }
        }

        // หาขนาดของ Background (เช่น orig_big) เผื่อไว้ด้วย
        SpriteRenderer[] sprites = FindObjectsByType<SpriteRenderer>();
        foreach (var sr in sprites)
        {
            if (sr.name.ToLower().Contains("orig_big") || sr.name.ToLower().Contains("bg") || sr.name.ToLower().Contains("background"))
            {
                if (!foundBounds)
                {
                    totalBounds = sr.bounds;
                    foundBounds = true;
                }
                else
                {
                    totalBounds.Encapsulate(sr.bounds);
                }
            }
        }

        if (!foundBounds)
        {
            Debug.LogError("ไม่พบด่าน (Tilemap) หรือ Background ในฉากให้คำนวณขนาด");
            return;
        }

        Vector2 min = totalBounds.min;
        Vector2 max = totalBounds.max;

        poly.points = new Vector2[]
        {
            new Vector2(min.x, min.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y),
            new Vector2(min.x, max.y)
        };

        EditorUtility.SetDirty(poly);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(poly.gameObject.scene);
        Debug.Log($"ปรับขอบเขตกล้องพอดีกับด่านเรียบร้อย: {min} ถึง {max}");
    }
}
