const fs = require('fs');
const path = require('path');

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
    
    // Sort frames by name to ensure correct order
    frames.sort((a, b) => a.name.localeCompare(b.name));
    return { guid, frames };
}

const basePath = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape';
const enemy2Path = path.join(basePath, 'Assets/Sprites/Enemy/Enemy2');
const anims = ['Idle', 'Walk', 'Run', 'Attack_3'];

const info = {};
for (const anim of anims) {
    const metaFile = path.join(enemy2Path, `${anim}.png.meta`);
    info[anim] = getSpriteInfo(metaFile);
}

console.log(JSON.stringify(info, null, 2));
