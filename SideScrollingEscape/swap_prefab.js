const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const basePath = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape';
const prefabsPath = path.join(basePath, 'Assets/Prefabs');
const scenesPath = path.join(basePath, 'Assets/Scenes');
const animsPath = path.join(basePath, 'Assets/Animations/Killer');

// 1. Get Killer GUID and Controller GUID
const killerPrefabMeta = fs.readFileSync(path.join(prefabsPath, 'Killer.prefab.meta'), 'utf8');
const killerGuid = killerPrefabMeta.match(/guid: ([a-f0-9]+)/)[1];

const overrideMeta = fs.readFileSync(path.join(animsPath, 'Enemy2OverrideController.overrideController.meta'), 'utf8');
const overrideGuid = overrideMeta.match(/guid: ([a-f0-9]+)/)[1];

const controllerMeta = fs.readFileSync(path.join(animsPath, 'KillerAnimator.controller.meta'), 'utf8');
const controllerGuid = controllerMeta.match(/guid: ([a-f0-9]+)/)[1];

// 2. Create Killer2.prefab
let killerPrefab = fs.readFileSync(path.join(prefabsPath, 'Killer.prefab'), 'utf8');

// Replace Controller GUID
const oldControllerStr = `m_Controller: {fileID: 9100000, guid: ${controllerGuid}, type: 2}`;
const newControllerStr = `m_Controller: {fileID: 22100000, guid: ${overrideGuid}, type: 2}`;
killerPrefab = killerPrefab.replace(oldControllerStr, newControllerStr);

fs.writeFileSync(path.join(prefabsPath, 'Killer2.prefab'), killerPrefab);

// 3. Create Killer2.prefab.meta
const killer2Guid = crypto.randomBytes(16).toString('hex');
const killer2Meta = `fileFormatVersion: 2
guid: ${killer2Guid}
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
fs.writeFileSync(path.join(prefabsPath, 'Killer2.prefab.meta'), killer2Meta);

// 4. Update Level2.unity to use Killer2.prefab instead of Killer.prefab
let level2 = fs.readFileSync(path.join(scenesPath, 'Level2.unity'), 'utf8');
level2 = level2.split(killerGuid).join(killer2Guid);
fs.writeFileSync(path.join(scenesPath, 'Level2.unity'), level2);

console.log('Successfully swapped Killer to Killer2 in Level2!');
