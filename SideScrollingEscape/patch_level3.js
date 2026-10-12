const fs = require('fs');
const path = require('path');

const levelPath = path.join('c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Scenes/Level3.unity');
const configPath = path.join('c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Data/Levels/Level3Config.asset');

// 1. Modify Level3.unity
let levelData = fs.readFileSync(levelPath, 'utf8');

// Find KillerAI script component block (guid: 42e8cf5ad31e13748be722b2659f0e23)
const killerAIRegex = /m_Script: \{fileID: 11500000, guid: 42e8cf5ad31e13748be722b2659f0e23, type: 3\}[\s\S]*?(?=--- !u!|\Z)/g;
levelData = levelData.replace(killerAIRegex, (match) => {
    if (!match.includes('isBlind:')) {
        // Insert isBlind: 1 after m_EditorClassIdentifier
        return match.replace(/m_EditorClassIdentifier: .*(\r?\n)/, "$&  isBlind: 1$1");
    } else {
        return match.replace(/isBlind: 0/, 'isBlind: 1');
    }
});

// Update the Animator m_Controller to use Enemy3OverrideController
// The original is 003a7d42824ab654ba6ff6dfe3c79147 (KillerAnimator.controller)
// We want it to be 9b54c9a1d609f7106b864af262d23d40 (Enemy3OverrideController.overrideController)
const animatorRegex = /m_Controller: \{fileID: 9100000, guid: [a-f0-9]{32}, type: 2\}/g;
levelData = levelData.replace(animatorRegex, 'm_Controller: {fileID: 9100000, guid: 9b54c9a1d609f7106b864af262d23d40, type: 2}');

fs.writeFileSync(levelPath, levelData);
console.log('Patched Level3.unity');

// 2. Modify Level3Config.asset
let configData = fs.readFileSync(configPath, 'utf8');

// The Blind Hunter: Fast walk speed, huge hearing radius, short sight (though sight is bypassed if isBlind is true)
configData = configData.replace(/killerPatrolSpeed: .*/, 'killerPatrolSpeed: 3.5');
configData = configData.replace(/killerChaseSpeed: .*/, 'killerChaseSpeed: 5.5');
configData = configData.replace(/killerSightRange: .*/, 'killerSightRange: 0'); // Blind
configData = configData.replace(/killerHearingRange: .*/, 'killerHearingRange: 15'); // Hear you anywhere in the room

fs.writeFileSync(configPath, configData);
console.log('Patched Level3Config.asset');

