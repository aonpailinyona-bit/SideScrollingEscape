const fs = require('fs');
const path = require('path');

const killer3Path = path.join('c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Prefabs/Killer3.prefab');
let prefabData = fs.readFileSync(killer3Path, 'utf8');

// Fix Animator Controller fileID to 22100000 for AnimatorOverrideController
prefabData = prefabData.replace(/m_Controller: \{fileID: 9100000, guid: 9b54c9a1d609f7106b864af262d23d40, type: 2\}/g, 
                                'm_Controller: {fileID: 22100000, guid: 9b54c9a1d609f7106b864af262d23d40, type: 2}');

// Fix initial Sprite to be from Enemy3 Idle.png
// The original is: m_Sprite: {fileID: 4723107735738678597, guid: 7d3d350f66a0cae4e973ab7d0a4697ad, type: 3}
// We want to replace the guid with 8e63ceddbe119424e89292ec7c858f1e (Enemy3 Idle.png)
// And the fileID is 4723107735738678597 (which is conveniently the same internal ID for Idle_0 in the new sprite sheet!)
prefabData = prefabData.replace(/m_Sprite: \{fileID: -?[0-9]+, guid: [a-f0-9]{32}, type: 3\}/g,
                                'm_Sprite: {fileID: 4723107735738678597, guid: 8e63ceddbe119424e89292ec7c858f1e, type: 3}');

fs.writeFileSync(killer3Path, prefabData);
console.log('Fixed Killer3.prefab');
