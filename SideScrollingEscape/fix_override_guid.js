const fs = require('fs');
const path = require('path');

const controllerPath = path.join('c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Animations/Killer/Enemy3OverrideController.overrideController');
let data = fs.readFileSync(controllerPath, 'utf8');

// Replace the bad controller GUID with the correct KillerAnimator.controller GUID
data = data.replace(/m_Controller: \{fileID: 9100000, guid: [a-f0-9]{32}, type: 2\}/, 'm_Controller: {fileID: 9100000, guid: 003a7d42824ab654ba6ff6dfe3c79147, type: 2}');

fs.writeFileSync(controllerPath, data);
console.log('Fixed Enemy3OverrideController');
