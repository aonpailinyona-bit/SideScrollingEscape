const fs = require('fs');
const path = require('path');

const animsDir = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Animations/Killer';

// The data from get_sprite_info.js for Enemy3
const enemy3Data = {
  "Idle": {
    "guid": "23207fb90a07ed9448d4a5e74aee83de",
    "frames": [
      { "name": "Idle_0", "fileID": "4723107735738678597" },
      { "name": "Idle_1", "fileID": "-7385789195546760866" },
      { "name": "Idle_2", "fileID": "3234629978573100161" },
      { "name": "Idle_3", "fileID": "6373048116405451881" },
      { "name": "Idle_4", "fileID": "7264182050183361747" },
      { "name": "Idle_5", "fileID": "-2834552850196481573" },
      { "name": "Idle_6", "fileID": "3278449094680734212" }
    ]
  },
  "Walk": {
    "guid": "f0ae112a352f28948897b4167b87d155",
    "frames": [
      { "name": "Walk_0", "fileID": "4035959289877228764" },
      { "name": "Walk_1", "fileID": "-982904232830982447" },
      { "name": "Walk_2", "fileID": "-350908408369970039" },
      { "name": "Walk_3", "fileID": "-5409570386352685021" },
      { "name": "Walk_4", "fileID": "1553940418269799609" },
      { "name": "Walk_5", "fileID": "2416434504916517810" },
      { "name": "Walk_6", "fileID": "-6428935122508569504" },
      { "name": "Walk_7", "fileID": "7144598999076838305" },
      { "name": "Walk_8", "fileID": "7500509103958437945" },
      { "name": "Walk_9", "fileID": "1008816216748069464" },
      { "name": "Walk_10", "fileID": "11406907883527339" },
      { "name": "Walk_11", "fileID": "8030128826893041618" },
      { "name": "Walk_12", "fileID": "2315132830120601726" }
    ]
  },
  "Run": {
    "guid": "0f2c6688a899f4b43b08f4ce43adf39e",
    "frames": [
      { "name": "Run_0", "fileID": "4944230376442633554" },
      { "name": "Run_1", "fileID": "-2274363484027164459" },
      { "name": "Run_2", "fileID": "8284087734974297348" },
      { "name": "Run_3", "fileID": "5304202850863732263" },
      { "name": "Run_4", "fileID": "2833043080597125133" },
      { "name": "Run_5", "fileID": "-2217414122262695004" },
      { "name": "Run_6", "fileID": "-4440497016933152808" }
    ]
  },
  "Attack_3": {
    "guid": "fb17cb0ed8b48ea4f9325908cfb0b320",
    "frames": [
      { "name": "Attack_3_0", "fileID": "-8910899737472627771" },
      { "name": "Attack_3_1", "fileID": "-5464336490339650859" },
      { "name": "Attack_3_2", "fileID": "-1132038362817505424" },
      { "name": "Attack_3_3", "fileID": "-6574224857763752514" },
      { "name": "Attack_3_4", "fileID": "-6827800710642968084" },
      { "name": "Attack_3_5", "fileID": "5388459929022106953" },
      { "name": "Attack_3_6", "fileID": "-241614906349035404" },
      { "name": "Attack_3_7", "fileID": "-7294828852963927212" },
      { "name": "Attack_3_8", "fileID": "-6599260265685524675" },
      { "name": "Attack_3_9", "fileID": "-3510743571422996371" }
    ]
  }
};

const fps = 12; // Typical for pixel art
const loopModes = {
    "Idle": 2, // Loop
    "Walk": 2, // Loop
    "Run": 2, // Loop
    "Attack_3": 0, // Default/Clamp
    "Special": 0
};

for (const [animName, data] of Object.entries(enemy3Data)) {
    const guid = data.guid;
    const frames = data.frames;
    
    let curveStr = '';
    let ppCurveStr = '';
    
    frames.forEach((f, index) => {
        const t = (index * (1.0 / fps)).toFixed(2);
        curveStr += `      - time: ${t}\n`;
        curveStr += `        value: {fileID: ${f.fileID}, guid: ${guid}, type: 3}\n`;
        
        ppCurveStr += `    - time: ${t}\n`;
        ppCurveStr += `      value: {fileID: ${f.fileID}, guid: ${guid}, type: 3}\n`;
    });
    
    // add an extra frame at the end to hold the last sprite
    const endT = (frames.length * (1.0 / fps)).toFixed(2);

    const isLooping = loopModes[animName] === 2;
    const loopStr = isLooping ? `
    m_LoopTime: 1
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0` : `
    m_LoopTime: 0
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0`;

    const template = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Enemy3${animName}
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
${curveStr}    attribute: m_Sprite
    path: Sprite
    classID: 212
    script: {fileID: 0}
    flags: 2
  m_SampleRate: ${fps}
  m_WrapMode: ${loopModes[animName]}
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
${ppCurveStr}  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {fileID: 0}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: ${endT}
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0${loopStr}
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
`;

    fs.writeFileSync(path.join(animsDir, `Enemy3${animName}.anim`), template);
    console.log(`Created Enemy3${animName}.anim`);
}
