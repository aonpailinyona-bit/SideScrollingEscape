const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const basePath = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape';
const enemy2Path = path.join(basePath, 'Assets/Sprites/Enemy/Enemy2');
const killerAnimPath = path.join(basePath, 'Assets/Animations/Killer');

function getSpriteInfo(metaPath) {
    const content = fs.readFileSync(metaPath, 'utf8');
    const guidMatch = content.match(/guid: ([a-f0-9]+)/);
    const guid = guidMatch ? guidMatch[1] : null;
    
    const tableMatch = content.match(/nameFileIdTable:\s*([\s\S]*?)mipmapLimitGroupName/);
    const frames = [];
    if (tableMatch) {
        const lines = tableMatch[1].split('\n');
        for (const line of lines) {
            const m = line.match(/([a-zA-Z0-9_]+):\s*(-?\d+)/);
            if (m) {
                frames.push({ name: m[1], fileID: m[2] });
            }
        }
    }
    
    frames.sort((a, b) => {
        const numA = parseInt(a.name.match(/\d+$/)[0]);
        const numB = parseInt(b.name.match(/\d+$/)[0]);
        return numA - numB;
    });
    return { guid, frames };
}

const info = {};
['Idle', 'Walk', 'Run', 'Attack_3'].forEach(anim => {
    info[anim] = getSpriteInfo(path.join(enemy2Path, `${anim}.png.meta`));
});

function generateAnim(newAnimName, templateName, spriteData) {
    const templatePath = path.join(killerAnimPath, `${templateName}.anim`);
    let content = fs.readFileSync(templatePath, 'utf8');
    content = content.replace(/m_Name: .*/, `m_Name: ${newAnimName}`);

    let curveStr = '    curve:\n';
    let mappingStr = '    pptrCurveMapping:\n';
    const guid = spriteData.guid;
    for (let i = 0; i < spriteData.frames.length; i++) {
        const frame = spriteData.frames[i];
        curveStr += `    - time: ${i === 0 ? '0' : (i / 10).toString()}\n`;
        curveStr += `      value: {fileID: ${frame.fileID}, guid: ${guid}, type: 3}\n`;
        mappingStr += `    - {fileID: ${frame.fileID}, guid: ${guid}, type: 3}\n`;
    }

    content = content.replace(/    curve:\n(?:    - time: [\s\S]*?(?=    attribute: m_Sprite))/m, curveStr);
    content = content.replace(/    pptrCurveMapping:\n(?:    - \{fileID: [\s\S]*?(?=  m_AnimationClipSettings:))/m, mappingStr);

    const stopTime = spriteData.frames.length / 10;
    content = content.replace(/m_StopTime: .*/, `m_StopTime: ${stopTime}`);

    fs.writeFileSync(path.join(killerAnimPath, `${newAnimName}.anim`), content);
    
    // create meta
    const animGuid = crypto.randomBytes(16).toString('hex');
    const metaYaml = `fileFormatVersion: 2\nguid: ${animGuid}\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;
    fs.writeFileSync(path.join(killerAnimPath, `${newAnimName}.anim.meta`), metaYaml);
    return animGuid;
}

const mapping = {
    'Idle': 'KillerIdle',
    'Walk': 'KillerWalk',
    'Run': 'KillerRun',
    'Attack_3': 'KillerAttack1'
};

const newGuids = {};
for (const [newAnim, oldAnim] of Object.entries(mapping)) {
    const guid = generateAnim(`Enemy2${newAnim}`, oldAnim, info[newAnim]);
    newGuids[`Enemy2${newAnim}`] = guid;
}

function getGuid(filePath) {
    const content = fs.readFileSync(filePath, 'utf8');
    return content.match(/guid: ([a-f0-9]+)/)[1];
}

const controllerGuid = getGuid(path.join(killerAnimPath, 'KillerAnimator.controller.meta'));

const yaml = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!221 &22100000
AnimatorOverrideController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Enemy2OverrideController
  m_Controller: {fileID: 9100000, guid: ${controllerGuid}, type: 2}
  m_Clips:
  - m_OriginalClip: {fileID: 7400000, guid: ${getGuid(path.join(killerAnimPath, 'KillerIdle.anim.meta'))}, type: 2}
    m_OverrideClip: {fileID: 7400000, guid: ${newGuids.Enemy2Idle}, type: 2}
  - m_OriginalClip: {fileID: 7400000, guid: ${getGuid(path.join(killerAnimPath, 'KillerWalk.anim.meta'))}, type: 2}
    m_OverrideClip: {fileID: 7400000, guid: ${newGuids.Enemy2Walk}, type: 2}
  - m_OriginalClip: {fileID: 7400000, guid: ${getGuid(path.join(killerAnimPath, 'KillerRun.anim.meta'))}, type: 2}
    m_OverrideClip: {fileID: 7400000, guid: ${newGuids.Enemy2Run}, type: 2}
  - m_OriginalClip: {fileID: 7400000, guid: ${getGuid(path.join(killerAnimPath, 'KillerAttack1.anim.meta'))}, type: 2}
    m_OverrideClip: {fileID: 7400000, guid: ${newGuids.Enemy2Attack_3}, type: 2}`;

fs.writeFileSync(path.join(killerAnimPath, 'Enemy2OverrideController.overrideController'), yaml);
const overrideGuid = crypto.randomBytes(16).toString('hex');
const metaYaml = `fileFormatVersion: 2\nguid: ${overrideGuid}\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`;
fs.writeFileSync(path.join(killerAnimPath, 'Enemy2OverrideController.overrideController.meta'), metaYaml);

// Modify Level2.unity
const level2Path = path.join(basePath, 'Assets/Scenes/Level2.unity');
let level2Content = fs.readFileSync(level2Path, 'utf8');

// The original controller has guid: controllerGuid. We replace it with overrideGuid where it is used for Killer in Level2.
// But Level2 might just have the standard Controller guid on the Animator component.
// An Animator component has:
// m_Controller: {fileID: 9100000, guid: 003a7d42824ab654ba6ff6dfe3c79147, type: 2}
const oldControllerStr = `m_Controller: {fileID: 9100000, guid: ${controllerGuid}, type: 2}`;
const newControllerStr = `m_Controller: {fileID: 22100000, guid: ${overrideGuid}, type: 2}`;
level2Content = level2Content.split(oldControllerStr).join(newControllerStr);

fs.writeFileSync(level2Path, level2Content);
console.log('Success! Level 2 updated with Enemy2OverrideController');
