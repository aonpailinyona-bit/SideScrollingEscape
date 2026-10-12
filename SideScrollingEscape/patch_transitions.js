const fs = require('fs');
const path = 'c:/Users/aonpa/OneDrive/Desktop/SideScrollingEscape/SideScrollingEscape/Assets/Animations/Killer/KillerAnimator.controller';
let content = fs.readFileSync(path, 'utf8');

if (!content.includes('998877665544')) {
    // I need to find the Scream state. 
    // From patch_anim.js, Scream state ID is 11223344.
    // The Idle state ID is -3924765857999034071
    
    // Add a transition ID: 11112222
    const transitionBlock = `
--- !u!1101 &11112222
AnimatorStateTransition:
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: 
  m_Conditions: []
  m_DstStateMachine: {fileID: 0}
  m_DstState: {fileID: -3924765857999034071}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: 0.1
  m_TransitionOffset: 0
  m_ExitTime: 1
  m_HasExitTime: 1
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1`;

    content += transitionBlock;

    // add to m_Transitions of Scream state
    content = content.replace(
        /m_Name: Scream\s+m_Speed: 1\s+m_CycleOffset: 0\s+m_Transitions: \[\]/,
        'm_Name: Scream\r\n  m_Speed: 1\r\n  m_CycleOffset: 0\r\n  m_Transitions:\r\n  - {fileID: 11112222}'
    );
    
    // Fallback for unix line endings
    content = content.replace(
        /m_Name: Scream\n  m_Speed: 1\n  m_CycleOffset: 0\n  m_Transitions: \[\]/,
        'm_Name: Scream\n  m_Speed: 1\n  m_CycleOffset: 0\n  m_Transitions:\n  - {fileID: 11112222}'
    );

    fs.writeFileSync(path, content);
    console.log('Patched animator transitions!');
}
