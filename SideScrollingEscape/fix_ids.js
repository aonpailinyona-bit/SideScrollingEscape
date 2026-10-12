const fs = require('fs');
const path = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Animations/Killer/KillerAnimator.controller';

let content = fs.readFileSync(path, 'utf8');

content = content.replace(/1122334455667788990/g, '11223344');
content = content.replace(/9988776655443322110/g, '99887766');

fs.writeFileSync(path, content);
console.log('Fixed IDs!');
