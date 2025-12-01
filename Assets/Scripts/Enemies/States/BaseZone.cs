using UnityEngine;

public class BaseZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var ec = other.GetComponent<EnemyController>();
        if (ec != null && ec.IsRecruited)
        {
            var f = ec.fsm.CurrentState as FriendlyFollowState;
            if (f != null)
            {
                f.SetInsideBase(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var ec = other.GetComponent<EnemyController>();
        if (ec != null && ec.IsRecruited)
        {
            var f = ec.fsm.CurrentState as FriendlyFollowState;
            if (f != null)
            {
                f.SetInsideBase(false);
            }
        }
    }
}
