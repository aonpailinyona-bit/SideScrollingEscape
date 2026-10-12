const fs = require('fs');
const path = require('path');

const level3Path = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Scenes/Level3.unity';
let content = fs.readFileSync(level3Path, 'utf8');

// Fix lighting bug
content = content.replace(/m_Color: \{r: 0.8, g: 0.1, b: 0.1, a: 1\}\r?\n\s+m_Intensity: 1/, 'm_Color: {r: 0.8, g: 0.1, b: 0.1, a: 1}\n  m_Intensity: 0.05');

// Swap Trees to Dark-Tree.png
const greenTreeGuid = 'd513bf6944eab084f936d19fb9659053';
// Let's also check for Yellow and Red tree guids just in case they were used, but we can just swap Green for now.
const darkTreeGuid = 'df1d0934131ccfd48bcc8f2139d99702';

content = content.split(greenTreeGuid).join(darkTreeGuid);

fs.writeFileSync(level3Path, content);
console.log('Fixed lighting and updated trees!');
