const fs = require('fs');
const path = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Scripts/Player/PlayerController.cs';
let content = fs.readFileSync(path, 'utf8');

const targetRegex = /private System\.Collections\.IEnumerator StunRoutine\(float duration\)[\s\S]*?MovementLocked = false;\s*\}/;

const replacement = `private System.Collections.IEnumerator StunRoutine(float duration)
        {
            MovementLocked = true;
            if (body != null)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }

            float blinkInterval = 0.15f;
            float elapsed = 0f;
            bool isVisible = true;
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();

            while (elapsed < duration)
            {
                if (sr != null)
                {
                    isVisible = !isVisible;
                    sr.enabled = isVisible;
                }
                yield return new WaitForSeconds(Mathf.Min(blinkInterval, duration - elapsed));
                elapsed += blinkInterval;
            }

            if (sr != null) sr.enabled = true;
            MovementLocked = false;
        }`;

content = content.replace(targetRegex, replacement);
fs.writeFileSync(path, content);
console.log("StunRoutine cleaned!");
