const fs = require('fs');

const path = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Scripts/Enemy/KillerAI.cs';
let content = fs.readFileSync(path, 'utf8');

// 1. Add Cooldown variables
const varTarget = 'private float screamTimer;';
const varReplacement = 'private float screamTimer;\r\n        [SerializeField] private float screamCooldown = 4.5f;\r\n        private float screamCooldownTimer;';
content = content.replace(varTarget, varReplacement);

// 2. Add Cooldown Tick
const updateTarget = 'attackTimer -= Time.deltaTime;';
const updateReplacement = 'attackTimer -= Time.deltaTime;\r\n            if (screamCooldownTimer > 0f) screamCooldownTimer -= Time.deltaTime;';
content = content.replace(updateTarget, updateReplacement);

// 3. Replace EnterChase
const enterChaseRegex = /private void EnterChase\(\)[\s\S]*?SetState\(KillerState\.Chase\);\r?\n\s*}\r?\n\s*}/;
const newEnterChase = `private void EnterChase()
        {
            lastKnownPosition = player.position;
            if (state != KillerState.Chase && state != KillerState.Scream)
            {
                if (detectClip != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySfxAt(detectClip, transform.position, 1f);
                }

                if (screamCooldownTimer <= 0f && CanSeePlayerForScream())
                {
                    TriggerScream();
                }
                else
                {
                    SetState(KillerState.Chase);
                }
            }
            else if (state != KillerState.Scream)
            {
                SetState(KillerState.Chase);
            }
        }`;
content = content.replace(enterChaseRegex, newEnterChase);

// 4. Add Helper Methods
const detectPlayerTarget = 'private bool DetectPlayer()';
const helperMethods = `private bool CanSeePlayerForScream()
        {
            if (playerHiding != null && playerHiding.IsHidden) return false;
            
            Vector2 eye = (Vector2)transform.position + Vector2.up * eyeHeight;
            Vector2 toPlayer = (Vector2)player.position + Vector2.up * eyeHeight - eye;
            float dist = toPlayer.magnitude;

            if (dist > 15f) return false;

            Vector2 facingDir = new Vector2(facing, 0f);
            float angle = Vector2.Angle(facingDir, toPlayer);
            if (angle > sightHalfAngle) return false;

            RaycastHit2D hit = Physics2D.Raycast(eye, toPlayer.normalized, dist, obstacleMask);
            return hit.collider == null;
        }

        private void TriggerScream()
        {
            if (playerController != null)
            {
                playerController.Stun(screamDuration);
            }
            if (animator != null)
            {
                animator.SetTrigger(ScreamHash);
            }
            screamTimer = screamDuration;
            screamCooldownTimer = screamCooldown;
            SetState(KillerState.Scream);
        }

        private bool DetectPlayer()`;
content = content.replace(detectPlayerTarget, helperMethods);

// 5. Update TickChase
const tickChaseTarget = `private void TickChase(bool detected)
        {
            float dist = Vector2.Distance(player.position, transform.position);`;
const tickChaseReplacement = `private void TickChase(bool detected)
        {
            if (screamCooldownTimer <= 0f && CanSeePlayerForScream())
            {
                TriggerScream();
                return;
            }

            float dist = Vector2.Distance(player.position, transform.position);`;
content = content.replace(tickChaseTarget, tickChaseReplacement);

fs.writeFileSync(path, content);
console.log('Modified KillerAI.cs with cooldown logic!');
