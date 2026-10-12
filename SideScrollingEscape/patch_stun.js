const fs = require('fs');
const path = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Scripts/Player/PlayerController.cs';
let content = fs.readFileSync(path, 'utf8');

const targetRegex = /private System\.Collections\.IEnumerator StunRoutine\(float duration\)\s*\{\s*MovementLocked = true;\s*if \(body != null\)\s*\{\s*body\.linearVelocity = new Vector2\(0f, body\.linearVelocity\.y\);\s*\}\s*yield return new WaitForSeconds\(duration\);\s*MovementLocked = false;\s*\}/;

const replacement = `private System.Collections.IEnumerator StunRoutine(float duration)
        {
            MovementLocked = true;
            if (body != null)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }

            GameObject textObj = new GameObject("StunText");
            textObj.transform.position = transform.position + Vector3.up * 1.8f;
            textObj.transform.SetParent(transform);
            var tmp = textObj.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "STUNNED!";
            tmp.color = Color.red;
            tmp.fontSize = 5;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.sortingOrder = 50;

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
            if (textObj != null) Destroy(textObj);

            MovementLocked = false;
        }`;

content = content.replace(targetRegex, replacement);
fs.writeFileSync(path, content);
console.log("StunRoutine replaced!");
