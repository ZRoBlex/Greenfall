using UnityEngine;

public class DropPickupNPC : MonoBehaviour
{
    public float attractDistance = 4f;
    public float flySpeed = 6f;

    Transform player;
    bool attracted;

    void OnEnable()
    {
        attracted = false;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attractDistance)
            attracted = true;

        if (attracted)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                flySpeed * Time.deltaTime
            );
        }
    }

    public void PickUp()
    {
        gameObject.SetActive(false);
    }
}
