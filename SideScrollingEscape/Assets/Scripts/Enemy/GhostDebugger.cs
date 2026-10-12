using UnityEngine;
using SideScrollingEscape.Enemy;

public class GhostDebugger : MonoBehaviour
{
    private KillerAI killerAI;

    void Start()
    {
        killerAI = GetComponent<KillerAI>();
        if (killerAI != null)
        {
            killerAI.OnStateChanged += (newState) => {
                Debug.Log($"[GhostDebugger] State changed to: {newState} at time {Time.time}");
            };
        }
    }
}
