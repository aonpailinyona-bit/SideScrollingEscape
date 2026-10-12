using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SideScrollingEscape.UI;

namespace SideScrollingEscape.Editor
{
    public class CreateInventoryUIEditor
    {
        [MenuItem("Tools/Create Inventory UI")]
        public static void CreateInventoryUI()
        {
            string prefabPath = "Assets/Prefabs/GameHUD.prefab";
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (hudPrefab == null)
            {
                Debug.LogError("Could not find GameHUD prefab at " + prefabPath);
                return;
            }

            // Open the prefab for editing
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);

            Transform hudPanel = instance.transform.Find("HUDPanel");
            if (hudPanel == null)
            {
                hudPanel = instance.transform;
            }

            // Remove existing inventory panel if it exists
            Transform oldPanel = hudPanel.Find("InventoryPanel");
            if (oldPanel != null)
            {
                Object.DestroyImmediate(oldPanel.gameObject);
            }

            // Load built-in UI sprite for rounded corners
            Sprite bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            // Create InventoryPanel (Main Container)
            GameObject invObj = new GameObject("InventoryPanel", typeof(RectTransform));
            invObj.transform.SetParent(hudPanel, false);

            RectTransform rect = invObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(340f, 400f); // Taller for title

            Image bg = invObj.AddComponent<Image>();
            bg.sprite = bgSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Dark blue-ish grey

            // Add Outline to panel
            Outline panelOutline = invObj.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.8f, 0.7f, 0.3f, 1f); // Gold outline
            panelOutline.effectDistance = new Vector2(2f, -2f);

            // Add Title Text
            GameObject titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(invObj.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 60f);
            titleRect.anchoredPosition = new Vector2(0f, 0f);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "INVENTORY";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.9f, 0.85f, 0.7f, 1f); // Pale gold text
            
            Shadow titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = Color.black;
            titleShadow.effectDistance = new Vector2(2f, -2f);

            // Add Grid Container
            GameObject gridObj = new GameObject("Grid", typeof(RectTransform));
            gridObj.transform.SetParent(invObj.transform, false);
            RectTransform gridRect = gridObj.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0f, 0f);
            gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.sizeDelta = new Vector2(0f, -60f); // Offset from top
            gridRect.anchoredPosition = new Vector2(0f, -30f);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(85f, 85f);
            grid.spacing = new Vector2(12f, 12f);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.childAlignment = TextAnchor.UpperCenter;

            // Add InventoryUI script
            InventoryUI inventoryUI = instance.AddComponent<InventoryUI>();

            // Create 9 slots
            InventorySlotUI[] slots = new InventorySlotUI[9];
            for (int i = 0; i < 9; i++)
            {
                GameObject slotObj = new GameObject("Slot_" + i, typeof(RectTransform));
                slotObj.transform.SetParent(gridObj.transform, false);

                // Slot background
                Image slotBg = slotObj.AddComponent<Image>();
                slotBg.sprite = bgSprite;
                slotBg.type = Image.Type.Sliced;
                slotBg.color = new Color(0.2f, 0.2f, 0.25f, 1f); // Lighter background

                Outline slotOutline = slotObj.AddComponent<Outline>();
                slotOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
                slotOutline.effectDistance = new Vector2(1f, -1f);

                // Icon image (child of slot)
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(slotObj.transform, false);
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = new Vector2(-16f, -16f); // 8px padding
                iconRect.anchoredPosition = Vector2.zero;

                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.preserveAspect = true; // Fix image stretching
                iconImg.enabled = false;

                // Add Slot script
                InventorySlotUI slotUI = slotObj.AddComponent<InventorySlotUI>();
                SerializedObject serializedSlot = new SerializedObject(slotUI);
                serializedSlot.FindProperty("icon").objectReferenceValue = iconImg;
                serializedSlot.ApplyModifiedProperties();

                slots[i] = slotUI;
            }

            // Cleanup old InventoryUI script if it exists
            InventoryUI[] oldUIs = instance.GetComponents<InventoryUI>();
            foreach(var ui in oldUIs)
            {
                if (ui != inventoryUI) Object.DestroyImmediate(ui);
            }

            // Setup InventoryUI
            SerializedObject serializedUI = new SerializedObject(inventoryUI);
            serializedUI.FindProperty("inventoryPanel").objectReferenceValue = invObj;
            
            SerializedProperty slotsProp = serializedUI.FindProperty("slots");
            slotsProp.arraySize = 9;
            for (int i = 0; i < 9; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }
            serializedUI.ApplyModifiedProperties();

            // Hide the panel initially
            invObj.SetActive(false);

            // Apply to prefab
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            Debug.Log("Beautiful Inventory UI successfully updated in GameHUD prefab!");
        }
    }
}
