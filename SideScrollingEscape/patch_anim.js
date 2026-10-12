const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const basePath = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape';
const animsPath = path.join(basePath, 'Assets/Animations/Killer');

// 1. Create Enemy2Special.anim
const spriteGuid = '3f5ee12ae4c489649922f810572a11a2';
const fileIds = [
  '-9068871714124230211',
  '-5513262646514174557',
  '-6198531488440642195',
  '6701770154028129973',
  '4992887672371064048'
];

let curve = '';
for (let i = 0; i < fileIds.length; i++) {
  const time = (i * 0.16).toFixed(2);
  curve += `    - time: ${time}
      value: {fileID: ${fileIds[i]}, guid: ${spriteGuid}, type: 3}
`;
}
const totalTime = ((fileIds.length) * 0.16).toFixed(2);

const animYaml = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Enemy2Special
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
  m_PPtrCurves:
  - serializedVersion: 2
    curve:
${curve}    attribute: m_Sprite
    path: Sprite
    classID: 212
    script: {fileID: 0}
    flags: 2
  m_SampleRate: 60
  m_WrapMode: 0
  m_Bounds:
    m_Center: {x: 0, y: 0, z: 0}
    m_Extent: {x: 0, y: 0, z: 0}
  m_ClipBindingConstant:
    genericBindings:
    - serializedVersion: 2
      path: 850496168
      attribute: 0
      script: {fileID: 0}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    pptrCurveMapping:
${curve.replace(/    - time/g, '    - {fileID').replace(/      value: /g, '').replace(/\}/g, '}')}
  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {fileID: 0}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: ${totalTime}
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

fs.writeFileSync(path.join(animsPath, 'Enemy2Special.anim'), animYaml);
fs.writeFileSync(path.join(animsPath, 'Enemy2Special.anim.meta'), `fileFormatVersion: 2
guid: ${crypto.randomBytes(16).toString('hex')}
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 7400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`);


// 2. Modify KillerAnimator.controller
const controllerPath = path.join(animsPath, 'KillerAnimator.controller');
let controllerStr = fs.readFileSync(controllerPath, 'utf8');

// Generate unique IDs
const stateId = '1122334455667788990';
const transitionId = '9988776655443322110';

// Add parameter
const paramsRegex = /m_AnimatorParameters:\r?\n/;
controllerStr = controllerStr.replace(paramsRegex, `m_AnimatorParameters:\n  - m_Name: scream\n    m_Type: 9\n    m_DefaultFloat: 0\n    m_DefaultInt: 0\n    m_DefaultBool: 0\n    m_Controller: {fileID: 9100000}\n`);

// Add state to child states
const childStatesRegex = /m_ChildStates:\r?\n/;
controllerStr = controllerStr.replace(childStatesRegex, `m_ChildStates:\n  - serializedVersion: 1\n    m_State: {fileID: ${stateId}}\n    m_Position: {x: 350, y: 100, z: 0}\n`);

// Add AnyState transition
const anyStateRegex = /m_AnyStateTransitions: \[\]/;
controllerStr = controllerStr.replace(anyStateRegex, `m_AnyStateTransitions:\n  - {fileID: ${transitionId}}`);

// Append new objects
const newObjects = `
--- !u!1102 &${stateId}
AnimatorState:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Scream
  m_Speed: 1
  m_CycleOffset: 0
  m_Transitions: []
  m_StateMachineBehaviours: []
  m_Position: {x: 50, y: 50, z: 0}
  m_IKOnFeet: 0
  m_WriteDefaultValues: 1
  m_Mirror: 0
  m_SpeedParameterActive: 0
  m_MirrorParameterActive: 0
  m_CycleOffsetParameterActive: 0
  m_TimeParameterActive: 0
  m_Motion: {fileID: 0}
  m_Tag: 
  m_SpeedParameter: 
  m_MirrorParameter: 
  m_CycleOffsetParameter: 
  m_TimeParameter: 
--- !u!1101 &${transitionId}
AnimatorStateTransition:
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  m_Conditions:
  - m_ConditionMode: 1
    m_ConditionEvent: scream
    m_EventTreshold: 0
  m_DstStateMachine: {fileID: 0}
  m_DstState: {fileID: ${stateId}}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0
  m_TransitionOffset: 0
  m_ExitTime: 0.75
  m_HasExitTime: 0
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1
`;
controllerStr += newObjects;

fs.writeFileSync(controllerPath, controllerStr);

// 3. Modify Enemy2OverrideController.overrideController
// Wait, we need to know the GUID of the new anim we just created.
// But we just created Enemy2Special.anim, its guid is in the meta file. Let's read it back.
const specialAnimMeta = fs.readFileSync(path.join(animsPath, 'Enemy2Special.anim.meta'), 'utf8');
const specialGuid = specialAnimMeta.match(/guid: ([a-f0-9]+)/)[1];

const overridePath = path.join(animsPath, 'Enemy2OverrideController.overrideController');
let overrideStr = fs.readFileSync(overridePath, 'utf8');

// We need to append the override clip. It has an m_Clips array.
const clipsRegex = /m_Clips:\r?\n/;
// The original clip in the base animator is {fileID: 0} because we left m_Motion as {fileID: 0} for Scream state.
// Wait, an AnimatorOverrideController overrides based on the ORIGINAL clip reference.
// If the original clip is null, we can't easily override it!
// We MUST assign a dummy clip to the base Scream state so the override controller has a key to override.
// Let's use KillerIdle.anim as the dummy base clip.
const killerIdleGuid = '338b43ed7aadd224982700eefa4c0888'; // from KillerIdle.anim.meta (we saw it earlier)

// Let's fix the base controller's Scream state motion:
controllerStr = controllerStr.replace('m_Motion: {fileID: 0}', `m_Motion: {fileID: 7400000, guid: ${killerIdleGuid}, type: 2}`);
fs.writeFileSync(controllerPath, controllerStr);

// Now for the override controller, we might not easily be able to add it via text because the override controller has a specific format.
// Let's check how Enemy2OverrideController looks.
console.log('Done generating files, outputting override controller...');
