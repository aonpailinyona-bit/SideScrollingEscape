const fs = require('fs');
const path = require('path');

const killer3Path = path.join('c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Prefabs/Killer3.prefab');
let prefabData = fs.readFileSync(killer3Path, 'utf8');

const animatorRegex = /m_Controller: \{fileID: 9100000, guid: [a-f0-9]{32}, type: 2\}/g;
prefabData = prefabData.replace(animatorRegex, 'm_Controller: {fileID: 9100000, guid: 9b54c9a1d609f7106b864af262d23d40, type: 2}');

// Also ensure isBlind: 1 is set in Killer3.prefab just in case the scene override didn't stick
const killerAIRegex = /m_Script: \{fileID: 11500000, guid: 42e8cf5ad31e13748be722b2659f0e23, type: 3\}[\s\S]*?(?=--- !u!|\Z)/g;
prefabData = prefabData.replace(killerAIRegex, (match) => {
    if (!match.includes('isBlind:')) {
        return match.replace(/m_EditorClassIdentifier: .*(\r?\n)/, "$&  isBlind: 1$1");
    } else {
        return match.replace(/isBlind: 0/, 'isBlind: 1');
    }
});

fs.writeFileSync(killer3Path, prefabData);
console.log('Patched Killer3.prefab');
