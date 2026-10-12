const fs = require('fs');
const path = require('path');

const level3Path = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Scenes/Level3.unity';
let content = fs.readFileSync(level3Path, 'utf8');

// 1. Lighting Fix (Blood Moon)
content = content.replace(/m_Color: \{r: 0.8, g: 0.1, b: 0.1, a: 1\}\r?\n\s+m_Intensity: 0.05/, 'm_Color: {r: 0.9, g: 0.15, b: 0.15, a: 1}\n  m_Intensity: 0.25');

// Just in case it wasn't replaced properly before (if it's still m_Intensity: 1)
content = content.replace(/m_Color: \{r: 0.8, g: 0.1, b: 0.1, a: 1\}\r?\n\s+m_Intensity: 1/, 'm_Color: {r: 0.9, g: 0.15, b: 0.15, a: 1}\n  m_Intensity: 0.25');

// 2. Mix Dark and Red Trees
const darkTreeGuid = 'df1d0934131ccfd48bcc8f2139d99702';
const redTreeGuid = '0713d5d75dcf2fa4bbd145e69e5a9bc6';

const parts = content.split(darkTreeGuid);
let newContent = parts[0];
for (let i = 1; i < parts.length; i++) {
    // Alternating trees: one red, one dark
    const guidToUse = (i % 2 === 0) ? darkTreeGuid : redTreeGuid;
    newContent += guidToUse + parts[i];
}
content = newContent;

fs.writeFileSync(level3Path, content);
console.log('Blood Moon Overhaul applied successfully!');
