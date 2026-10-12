const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const basePath = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape';
const prefabsPath = path.join(basePath, 'Assets/Prefabs');
const scenesPath = path.join(basePath, 'Assets/Scenes');

// 1. Create Killer3.prefab
const killerPrefabMeta = fs.readFileSync(path.join(prefabsPath, 'Killer.prefab.meta'), 'utf8');
const killerGuid = killerPrefabMeta.match(/guid: ([a-f0-9]+)/)[1];

let killerPrefab = fs.readFileSync(path.join(prefabsPath, 'Killer.prefab'), 'utf8');
// Replace fallbackChaseSpeed
killerPrefab = killerPrefab.replace(/fallbackChaseSpeed: 3\.5/, 'fallbackChaseSpeed: 7');
fs.writeFileSync(path.join(prefabsPath, 'Killer3.prefab'), killerPrefab);

// Create Meta for Killer3
const killer3Guid = crypto.randomBytes(16).toString('hex');
const killer3Meta = `fileFormatVersion: 2
guid: ${killer3Guid}
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
fs.writeFileSync(path.join(prefabsPath, 'Killer3.prefab.meta'), killer3Meta);

// 2. Modify Level3.unity
const level3Path = path.join(scenesPath, 'Level3.unity');
let level3Content = fs.readFileSync(level3Path, 'utf8');

// Swap Killer Prefab GUID
level3Content = level3Content.split(killerGuid).join(killer3Guid);

// Change GlobalLight2D
const globalLightRegex = /(m_LightType: 4\r?\n\s+m_BlendStyleIndex: 0\r?\n\s+m_FalloffIntensity: [\d.]+\r?\n\s+m_Color: )\{.*?\}(\r?\n\s+m_Intensity: )[\d.]+/;
level3Content = level3Content.replace(globalLightRegex, '$1{r: 0.8, g: 0.1, b: 0.1, a: 1}$20.05');

// Change Audio
const oldAmbient = 'ambientClip: {fileID: 8300000, guid: d570b52f645c0b54fb2e523e6d6d704a, type: 3}';
const newAmbient = 'ambientClip: {fileID: 8300000, guid: 283db2a89e8ca6a40b7fed62238a3d77, type: 3}';
level3Content = level3Content.replace(oldAmbient, newAmbient);

fs.writeFileSync(level3Path, level3Content);
console.log('Level 3 made scary successfully!');
