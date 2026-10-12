using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level3TilemapBuilder
{
    private const string ScenePath = "Assets/Scenes/Level3.unity";
    private const string TileFolder = "Assets/Sprites/Tileset";
    private const string GeneratedRootName = "Level3_Forest";
    [MenuItem("Tools/Side Scrolling Escape/Rebuild Level 3 Tilemap")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void BuildFromCommandLine()
    {
        try
        {
            Build();
            Debug.Log("LEVEL3_TILEMAP_BUILD_OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        DestroyRoot("Level3_Tilemap");
        DestroyRoot(GeneratedRootName);

        Sprite background = LoadSprite("Assets/Sprites/Tileset/Background.png", "Background_0");
        Sprite tallTreeA = LoadSprite("Assets/Sprites/Tileset/Dark-Tree.png", "Dark-Tree_0");
        Sprite tallTreeB = LoadSprite("Assets/Sprites/Tileset/Dark-Tree.png", "Dark-Tree_2");
        Sprite tallTreeC = LoadSprite("Assets/Sprites/Tileset/Dark-Tree.png", "Dark-Tree_3");
        Sprite bushA = LoadSprite("Assets/Sprites/Tileset/Tree-Assets.png", "Tree-Assets_1");
        Sprite bushB = LoadSprite("Assets/Sprites/Tileset/Tree-Assets.png", "Tree-Assets_2");
        Sprite grassA = LoadSprite("Assets/Sprites/Tileset/Tiles.png", "Tiles_0");
        Sprite grassB = LoadSprite("Assets/Sprites/Tileset/Tiles.png", "Tiles_1");
        Sprite grassC = LoadSprite("Assets/Sprites/Tileset/Tiles.png", "Tiles_2");

        var root = new GameObject(GeneratedRootName);
        Transform sky = CreateGroup(root.transform, "Mist_And_Distant_Pines");
        Transform farTrees = CreateGroup(root.transform, "Far_Tree_Wall");
        Transform terrain = CreateGroup(root.transform, "Forest_Ground_And_Path");
        Transform midTrees = CreateGroup(root.transform, "Midground_Trees_And_Bushes");

        // Blue-grey mist keeps the corridor readable, while the tree layers make it feel enclosed.
        CreateSprite(sky, "MoonlitMist_A", background, new Vector3(-2f, 4f, 0f), Vector3.one, new Color(0.28f, 0.39f, 0.43f, 0.58f), -20);
        CreateSprite(sky, "MoonlitMist_B", background, new Vector3(46f, 4f, 0f), Vector3.one, new Color(0.19f, 0.29f, 0.34f, 0.62f), -20, true);
        CreateTreeWall(farTrees, tallTreeA, tallTreeB, tallTreeC, -8f, 102f, 7f, 1.8f, new Color(0.14f, 0.20f, 0.19f, 0.63f), -14);
        PaintForestTerrain(terrain, grassA, grassB, grassC);
        CreateTreeWall(midTrees, tallTreeB, tallTreeA, tallTreeC, -2f, 92f, 12f, 2.2f, new Color(0.18f, 0.28f, 0.18f, 0.9f), -4);
        PaintBushes(midTrees, bushA, bushB);
        DisablePrototypeRenderers();

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void DestroyRoot(string rootName)
    {
        GameObject oldRoot = GameObject.Find(rootName);
        if (oldRoot != null)
            UnityEngine.Object.DestroyImmediate(oldRoot);
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static Sprite LoadSprite(string assetPath, string preferredName)
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is Sprite sprite && sprite.name == preferredName)
                return sprite;
        }

        throw new FileNotFoundException($"Sprite '{preferredName}' was not found at '{assetPath}'.");
    }

    private static void CreateTreeWall(Transform parent, Sprite first, Sprite second, Sprite third, float startX, float endX, float spacing, float scale, Color color, int sortingOrder)
    {
        Sprite[] trees = { first, second, third };
        int index = 0;
        for (float x = startX; x <= endX; x += spacing)
        {
            Sprite tree = trees[index++ % trees.Length];
            bool flip = index % 2 == 0;
            CreateSprite(parent, $"Tree_{index:00}", tree, new Vector3(x, 0f, 0f), new Vector3(scale, scale, 1f), color, sortingOrder, flip);
        }
    }

    private static void PaintForestTerrain(Transform parent, Sprite grassA, Sprite grassB, Sprite grassC)
    {
        Sprite[] grass = { grassA, grassB, grassC };
        for (int x = -1; x <= 91; x++)
        {
            CreateSprite(parent, $"Ground_{x}_A", grass[Math.Abs(x) % grass.Length], new Vector3(x, -1.5f, 0f), Vector3.one, new Color(0.38f, 0.48f, 0.31f, 1f), -1);
            CreateSprite(parent, $"Ground_{x}_B", grass[Math.Abs(x + 1) % grass.Length], new Vector3(x, -0.5f, 0f), Vector3.one, new Color(0.30f, 0.41f, 0.25f, 1f), -1);
        }

        PaintGrassPlatform(parent, 7, 3, 4, grass);
        PaintGrassPlatform(parent, 13, 6, 3, grass);
        PaintGrassPlatform(parent, 18, 8, 3, grass);
        PaintGrassPlatform(parent, 24, 6, 4, grass);
        PaintGrassPlatform(parent, 30, 4, 3, grass);
        PaintGrassPlatform(parent, 36, 6, 4, grass);
        PaintGrassPlatform(parent, 43, 8, 4, grass);
        PaintGrassPlatform(parent, 51, 6, 3, grass);
        PaintGrassPlatform(parent, 56, 3, 4, grass);
        PaintGrassPlatform(parent, 63, 6, 4, grass);
        PaintGrassPlatform(parent, 70, 9, 4, grass);
        PaintGrassPlatform(parent, 78, 4, 4, grass);
    }

    private static void PaintGrassPlatform(Transform parent, int x, int y, int width, Sprite[] grass)
    {
        for (int offset = 0; offset < width; offset++)
        {
            Sprite sprite = grass[(x + offset) % grass.Length];
            CreateSprite(parent, $"Path_{x}_{y}_{offset}", sprite, new Vector3(x + offset, y, 0f), Vector3.one, new Color(0.40f, 0.50f, 0.30f, 1f), 0);
        }
    }

    private static void PaintBushes(Transform parent, Sprite bushA, Sprite bushB)
    {
        int[] positions = { 4, 11, 17, 28, 34, 48, 54, 60, 68, 75, 83, 89 };
        for (int index = 0; index < positions.Length; index++)
        {
            Sprite bush = index % 2 == 0 ? bushA : bushB;
            float y = index % 3 == 0 ? 0f : 1.2f;
            CreateSprite(parent, $"ShadowBush_{index:00}", bush, new Vector3(positions[index], y, 0f), new Vector3(1.35f, 1.35f, 1f), new Color(0.16f, 0.25f, 0.16f, 0.9f), -2, index % 2 == 1);
        }
    }

    private static void CreateSprite(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, int sortingOrder, bool flipX = false)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localScale = scale;
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.flipX = flipX;
    }

    private static Tilemap CreateLayer(Transform parent, string name, int sortingOrder)
    {
        var layer = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        layer.transform.SetParent(parent, false);
        Tilemap tilemap = layer.GetComponent<Tilemap>();
        TilemapRenderer renderer = layer.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        return tilemap;
    }

    private static TileBase LoadTile(params int[] candidates)
    {
        foreach (int index in candidates)
        {
            string path = $"{TileFolder}/Interior-01_{index}.asset";
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile != null)
                return tile;
        }

        throw new FileNotFoundException("No usable Interior-01 tile asset was found.");
    }

    private static void PaintBackdrop(Tilemap map, TileBase baseTile, TileBase accentTile)
    {
        for (int x = 0; x < 90; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                bool accent = (x % 11 == 0 && y > 1) || (y == 1 && x % 7 == 0);
                map.SetTile(new Vector3Int(x, y, 0), accent ? accentTile : baseTile);
            }
        }

        map.color = new Color(0.48f, 0.44f, 0.38f, 1f);
    }

    private static void PaintArchitecture(Tilemap map, TileBase ground, TileBase accent, TileBase trim)
    {
        // Outer shell of the hive corridor.
        PaintRect(map, -1, -2, 92, 2, ground, accent);
        PaintRect(map, -1, 12, 92, 1, trim, accent);
        PaintRect(map, -1, 0, 1, 12, ground, accent);
        PaintRect(map, 90, 0, 1, 12, ground, accent);

        // Existing gameplay route, rebuilt with tiles so ladders, keys and patrol logic stay valid.
        PaintPlatform(map, 7, 3, 4, ground, accent);
        PaintPlatform(map, 13, 6, 3, ground, accent);
        PaintPlatform(map, 18, 8, 3, ground, accent);
        PaintPlatform(map, 24, 6, 4, ground, accent);
        PaintPlatform(map, 30, 4, 3, ground, accent);
        PaintPlatform(map, 36, 6, 4, ground, accent);
        PaintPlatform(map, 43, 8, 4, ground, accent);
        PaintPlatform(map, 51, 6, 3, ground, accent);
        PaintPlatform(map, 56, 3, 4, ground, accent);
        PaintPlatform(map, 63, 6, 4, ground, accent);
        PaintPlatform(map, 70, 9, 4, ground, accent);
        PaintPlatform(map, 78, 4, 4, ground, accent);

        PaintPillar(map, 22, 0, 5, trim, accent);
        PaintPillar(map, 42, 0, 5, trim, accent);
        PaintPillar(map, 62, 0, 5, trim, accent);
        PaintPillar(map, 76, 0, 5, trim, accent);
    }

    private static void PaintDetails(Tilemap map, TileBase trim, TileBase accent)
    {
        // Repeating beams and landmark clusters make the long corridor easier to read while fleeing.
        for (int x = 4; x < 88; x += 8)
        {
            map.SetTile(new Vector3Int(x, 1, 0), accent);
            map.SetTile(new Vector3Int(x, 10, 0), trim);
        }

        int[] landmarkX = { 10, 31, 45, 65, 72, 84 };
        foreach (int x in landmarkX)
        {
            map.SetTile(new Vector3Int(x, 1, 0), trim);
            map.SetTile(new Vector3Int(x + 1, 1, 0), accent);
        }
    }

    private static void PaintPlatform(Tilemap map, int x, int y, int width, TileBase main, TileBase edge)
    {
        for (int i = 0; i < width; i++)
            map.SetTile(new Vector3Int(x + i, y, 0), i == 0 || i == width - 1 ? edge : main);
    }

    private static void PaintPillar(Tilemap map, int x, int y, int height, TileBase main, TileBase accent)
    {
        for (int i = 0; i < height; i++)
            map.SetTile(new Vector3Int(x, y + i, 0), i == height - 1 ? accent : main);
    }

    private static void PaintRect(Tilemap map, int x, int y, int width, int height, TileBase main, TileBase accent)
    {
        for (int ix = 0; ix < width; ix++)
        {
            for (int iy = 0; iy < height; iy++)
            {
                bool edge = ix == 0 || ix == width - 1 || iy == height - 1;
                map.SetTile(new Vector3Int(x + ix, y + iy, 0), edge ? accent : main);
            }
        }
    }

    private static void DisablePrototypeRenderers()
    {
        GameObject environment = GameObject.Find("Environment");
        if (environment == null)
            return;

        foreach (SpriteRenderer renderer in environment.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }
    }
}
