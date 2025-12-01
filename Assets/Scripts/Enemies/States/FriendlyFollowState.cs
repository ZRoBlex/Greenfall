using UnityEngine;

public class FriendlyFollowState : State<EnemyController>
{
    Transform player;
    bool insideBase = false;

    public override void Enter(EnemyController owner)
    {
        owner.debugStateName = "Friendly";

        player = GameObject.FindWithTag("Player")?.transform;

        owner.Recruit();

        if (owner.perception) owner.perception.enabled = false;
        if (owner.combat) owner.combat.enabled = false;

        // Anim idle
        owner.animatorBridge?.SetWalking(false);
    }

    public override void Tick(EnemyController owner)
    {
        owner.debugStateName = "Friendly";

        if (insideBase)
        {
            owner.movement.StopInstantly();
            owner.animatorBridge?.SetWalking(false);
            return;
        }

        if (player == null) return;

        float dist = Vector3.Distance(owner.transform.position, player.position);

        if (dist > owner.friendlyStopDistance)
        {
            owner.movement.MoveTowards(player.position, owner.friendlyFollowSpeed);
            owner.animatorBridge?.SetWalking(true);
        }
        else
        {
            owner.movement.StopInstantly();
            owner.animatorBridge?.SetWalking(false);
        }
    }

    public void SetInsideBase(bool v) => insideBase = v;
    public bool IsInsideBase() => insideBase;
}
