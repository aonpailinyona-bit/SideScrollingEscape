const fs = require('fs');
const path = require('path');

const level3Path = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Scenes/Level3.unity';
let content = fs.readFileSync(level3Path, 'utf8');

const yellowTreeGuid = 'fbdc2346fbcafb3418bb2ffd84bf4fb9';
const redTreeGuid = '0713d5d75dcf2fa4bbd145e69e5a9bc6';
const darkTreeGuid = 'df1d0934131ccfd48bcc8f2139d99702';

content = content.split(yellowTreeGuid).join(darkTreeGuid);
content = content.split(redTreeGuid).join(darkTreeGuid);

fs.writeFileSync(level3Path, content);
console.log('Swapped Yellow and Red trees to Dark trees!');
