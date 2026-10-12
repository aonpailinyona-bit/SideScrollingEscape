const fs = require('fs');
const path = require('path');

const killer2Path = path.join('c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Prefabs/Killer2.prefab');

let prefabData = fs.readFileSync(killer2Path, 'utf8');

// Find KillerAI script component block (guid: 42e8cf5ad31e13748be722b2659f0e23)
const killerAIRegex = /m_Script: \{fileID: 11500000, guid: 42e8cf5ad31e13748be722b2659f0e23, type: 3\}[\s\S]*?(?=--- !u!|\Z)/g;
prefabData = prefabData.replace(killerAIRegex, (match) => {
    if (!match.includes('canScream:')) {
        // Insert canScream: 1 after m_EditorClassIdentifier
        return match.replace(/m_EditorClassIdentifier: .*(\r?\n)/, "$&  canScream: 1$1");
    } else {
        return match.replace(/canScream: 0/, 'canScream: 1');
    }
});

fs.writeFileSync(killer2Path, prefabData);
console.log('Patched Killer2.prefab');

// Also, verify Level2.unity doesn't have an override for canScream that overrides it to 0
const level2Path = path.join('c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Scenes/Level2.unity');
let level2Data = fs.readFileSync(level2Path, 'utf8');
let modified = false;

const killerAIGuid = '42e8cf5ad31e13748be722b2659f0e23';
// If level2 has an override for the KillerAI component, let's just make sure we set canScream to 1
if (level2Data.includes('canScream: 0')) {
    level2Data = level2Data.replace(/canScream: 0/g, 'canScream: 1');
    modified = true;
}

if (modified) {
    fs.writeFileSync(level2Path, level2Data);
    console.log('Patched Level2.unity');
}
