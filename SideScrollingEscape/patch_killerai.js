const fs = require('fs');

const path = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Scripts/Enemy/KillerAI.cs';
let content = fs.readFileSync(path, 'utf8');

// 1. Add Scream to KillerState
content = content.replace('Chase,\r\n        /// <summary>In melee range and attacking.</summary>\r\n        Attack', 'Chase,\r\n        /// <summary>In melee range and attacking.</summary>\r\n        Attack,\r\n        /// <summary>Screaming to stun player.</summary>\r\n        Scream');

// 2. Add variables
const varsTarget = 'private static readonly int Attack1Hash = Animator.StringToHash("attack1");';
const varsReplacement = varsTarget + '\r\n        private static readonly int ScreamHash = Animator.StringToHash("scream");\r\n\r\n        [Header("Scream")]\r\n        [SerializeField] private float screamDuration = 0.8f;\r\n        private float screamTimer;';
content = content.replace(varsTarget, varsReplacement);

// 3. Update switch statement
const switchTarget = 'case KillerState.Attack:\r\n                    TickAttack(detected);\r\n                    break;';
const switchReplacement = switchTarget + '\r\n                case KillerState.Scream:\r\n                    TickScream(detected);\r\n                    break;';
content = content.replace(switchTarget, switchReplacement);

// 4. Modify EnterChase
const enterChaseTarget = `        private void EnterChase()
        {
            lastKnownPosition = player.position;
            if (state != KillerState.Chase && detectClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfxAt(detectClip, transform.position, 1f);
            }

            SetState(KillerState.Chase);
        }`;
const enterChaseReplacement = `        private void EnterChase()
        {
            lastKnownPosition = player.position;
            if (state != KillerState.Chase && state != KillerState.Scream)
            {
                if (detectClip != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySfxAt(detectClip, transform.position, 1f);
                }
                if (playerController != null)
                {
                    playerController.Stun(screamDuration);
                }
                if (animator != null)
                {
                    animator.SetTrigger(ScreamHash);
                }
                screamTimer = screamDuration;
                SetState(KillerState.Scream);
            }
            else if (state != KillerState.Scream)
            {
                SetState(KillerState.Chase);
            }
        }`;
content = content.replace(enterChaseTarget, enterChaseReplacement);

// 5. Add TickScream
const addMethodTarget = '        private void EnterChase()';
const addMethodReplacement = `        private void TickScream(bool detected)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            screamTimer -= Time.deltaTime;
            facing = player.position.x >= transform.position.x ? 1 : -1;
            ApplyFacing();
            if (screamTimer <= 0f)
            {
                SetState(KillerState.Chase);
            }
        }

        private void EnterChase()`;
content = content.replace(addMethodTarget, addMethodReplacement);

fs.writeFileSync(path, content);
console.log('Modified KillerAI.cs!');
