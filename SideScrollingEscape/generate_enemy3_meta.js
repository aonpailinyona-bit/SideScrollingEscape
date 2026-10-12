const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const animsDir = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Animations/Killer';
const animNames = ['Enemy3Idle', 'Enemy3Walk', 'Enemy3Run', 'Enemy3Attack_3'];

// Original Killer GUIDs
const originalGuids = {
    'Idle': '338b43ed7aadd224982700eefa4c0888', // KillerIdle
    'Walk': '5dc97c8cb8ccc304cabac280e957f242', // KillerWalk
    'Run': '932100bee9651f44bb841b792f19f7d6',  // KillerRun
    'Attack1': '412936b61c66394428c3c227de9b8f82'// KillerAttack1
};

const newGuids = {};

animNames.forEach(name => {
    const metaPath = path.join(animsDir, `${name}.anim.meta`);
    const guid = crypto.randomBytes(16).toString('hex');
    newGuids[name] = guid;
    
    const metaContent = `fileFormatVersion: 2
guid: ${guid}
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 7400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
    fs.writeFileSync(metaPath, metaContent);
    console.log(`Created ${metaPath}`);
});

// Create Override Controller
const overrideContent = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!221 &22100000
AnimatorOverrideController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Enemy3OverrideController
  m_Controller: {fileID: 9100000, guid: 03f56ceb9be63435fa8f2197475f456c, type: 2}
  m_Clips:
  - m_OriginalClip: {fileID: 7400000, guid: ${originalGuids['Idle']}, type: 2}
    m_OverrideClip: {fileID: 7400000, guid: ${newGuids['Enemy3Idle']}, type: 2}
  - m_OriginalClip: {fileID: 7400000, guid: ${originalGuids['Walk']}, type: 2}
    m_OverrideClip: {fileID: 7400000, guid: ${newGuids['Enemy3Walk']}, type: 2}
  - m_OriginalClip: {fileID: 7400000, guid: ${originalGuids['Run']}, type: 2}
    m_OverrideClip: {fileID: 7400000, guid: ${newGuids['Enemy3Run']}, type: 2}
  - m_OriginalClip: {fileID: 7400000, guid: ${originalGuids['Attack1']}, type: 2}
    m_OverrideClip: {fileID: 7400000, guid: ${newGuids['Enemy3Attack_3']}, type: 2}
`;

const overridePath = path.join(animsDir, 'Enemy3OverrideController.overrideController');
fs.writeFileSync(overridePath, overrideContent);
console.log(`Created Enemy3OverrideController.overrideController`);
