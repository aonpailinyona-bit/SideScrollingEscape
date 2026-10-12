const fs = require('fs');
const path = require('path');

const animsDir = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Animations/Killer';
const spritesDir = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Sprites/Enemy/Enemy3';

const enemy3Data = {};

function parseMetaFile(filename, animName) {
    const metaPath = path.join(spritesDir, filename + '.meta');
    if (!fs.existsSync(metaPath)) return;
    
    const content = fs.readFileSync(metaPath, 'utf8');
    
    const guidMatch = content.match(/^guid: ([a-f0-9]{32})/m);
    if (!guidMatch) return;
    const guid = guidMatch[1];
    
    const frames = [];
    const nameFileIdTableStr = content.split('nameFileIdTable:')[1];
    if (nameFileIdTableStr) {
        const lines = nameFileIdTableStr.split('\n');
        for (const line of lines) {
            const match = line.match(/^\s+([\w_]+):\s+(-?\d+)/);
            if (match) {
                frames.push({ name: match[1], fileID: match[2] });
            }
        }
    }
    
    // Sort frames by name (e.g., Idle_0, Idle_1, ...)
    frames.sort((a, b) => {
        const numA = parseInt(a.name.split('_').pop());
        const numB = parseInt(b.name.split('_').pop());
        return numA - numB;
    });
    
    enemy3Data[animName] = { guid, frames };
}

parseMetaFile('Idle.png', 'Idle');
parseMetaFile('Walk.png', 'Walk');
parseMetaFile('Attack.png', 'Attack_3');

const fps = 12; // Typical for pixel art
const loopModes = {
    "Idle": 2, // Loop
    "Walk": 2, // Loop
    "Run": 2, // Loop
    "Attack_3": 0, // Default/Clamp
};

// Also copy walk to run if run isn't provided
if (enemy3Data['Walk']) {
    enemy3Data['Run'] = JSON.parse(JSON.stringify(enemy3Data['Walk']));
}

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
    console.log(`Created Enemy3${animName}.anim with guid ${guid} and ${frames.length} frames`);
}
