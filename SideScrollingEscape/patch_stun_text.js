const fs = require('fs');

const prefabPath = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Prefabs/GameHUD.prefab';
let prefabContent = fs.readFileSync(prefabPath, 'utf8');

if (prefabContent.includes('m_Name: StunText')) {
    console.log('StunText already exists in prefab!');
    process.exit(0);
}

function generateID() {
    return Math.floor(Math.random() * 800000000000000000) + 100000000000000000;
}

const goID = generateID();
const rtID = generateID();
const crID = generateID();
const txtID = generateID();
const hudPanelID = '8951677089205098104';

const newYAML = `
--- !u!1 &${goID}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rtID}}
  - component: {fileID: ${crID}}
  - component: {fileID: ${txtID}}
  m_Layer: 0
  m_Name: StunText
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${rtID}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goID}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${hudPanelID}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 1}
  m_AnchorMax: {x: 0.5, y: 1}
  m_AnchoredPosition: {x: 0, y: -120}
  m_SizeDelta: {x: 500, y: 50}
  m_Pivot: {x: 0.5, y: 1}
--- !u!222 &${crID}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goID}}
  m_CullTransparentMesh: 1
--- !u!114 &${txtID}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goID}}
  m_Enabled: 0
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 0.9, b: 0, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}
    m_FontSize: 40
    m_FontStyle: 1
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: 1
    m_AlignByGeometry: 0
    m_RichText: 1
    m_HorizontalOverflow: 1
    m_VerticalOverflow: 1
    m_LineSpacing: 1
  m_Text: โดนสตั๊น!
`;

prefabContent += newYAML;

// Inject to HUDPanel's children
const hudPanelRegex = /(--- !u!224 &8951677089205098104[\s\S]*?m_Children:\n(?:  - {fileID: \d+}\n)*)/;
prefabContent = prefabContent.replace(hudPanelRegex, '$1  - {fileID: ' + rtID + '}\n');

// Inject to UIManager
const uiManagerRegex = /(  dangerText: {fileID: \d+}\n)/;
if (prefabContent.match(uiManagerRegex)) {
    prefabContent = prefabContent.replace(uiManagerRegex, '$1  stunText: {fileID: ' + txtID + '}\n');
}

fs.writeFileSync(prefabPath, prefabContent);
console.log('StunText added successfully via Node!');
