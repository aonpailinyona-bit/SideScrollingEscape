const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const animsPath = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Animations/Killer';

// 1. Create KillerScream.anim (Empty dummy clip)
const dummyGuid = crypto.randomBytes(16).toString('hex');
const dummyYaml = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: KillerScream
  serializedVersion: 7
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves: []
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves: []
  m_PPtrCurves: []
  m_SampleRate: 60
  m_WrapMode: 0
  m_Bounds:
    m_Center: {x: 0, y: 0, z: 0}
    m_Extent: {x: 0, y: 0, z: 0}
  m_ClipBindingConstant:
    genericBindings: []
    pptrCurveMapping: []
  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {fileID: 0}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: 0.1
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: 0
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
`;

fs.writeFileSync(path.join(animsPath, 'KillerScream.anim'), dummyYaml);
fs.writeFileSync(path.join(animsPath, 'KillerScream.anim.meta'), `fileFormatVersion: 2
guid: ${dummyGuid}
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 7400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`);

// 2. Fix base controller
const controllerPath = path.join(animsPath, 'KillerAnimator.controller');
let controllerStr = fs.readFileSync(controllerPath, 'utf8');
const killerIdleGuid = '338b43ed7aadd224982700eefa4c0888';
controllerStr = controllerStr.replace(`m_Motion: {fileID: 7400000, guid: ${killerIdleGuid}, type: 2}`, `m_Motion: {fileID: 7400000, guid: ${dummyGuid}, type: 2}`);
fs.writeFileSync(controllerPath, controllerStr);

// 3. Update Override Controller
const specialAnimMeta = fs.readFileSync(path.join(animsPath, 'Enemy2Special.anim.meta'), 'utf8');
const specialGuid = specialAnimMeta.match(/guid: ([a-f0-9]+)/)[1];

const overridePath = path.join(animsPath, 'Enemy2OverrideController.overrideController');
let overrideStr = fs.readFileSync(overridePath, 'utf8');

overrideStr += `  - m_OriginalClip: {fileID: 7400000, guid: ${dummyGuid}, type: 2}\n    m_OverrideClip: {fileID: 7400000, guid: ${specialGuid}, type: 2}\n`;
fs.writeFileSync(overridePath, overrideStr);

console.log('Fixed overrides!');
