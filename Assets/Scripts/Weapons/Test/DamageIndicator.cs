using UnityEngine;
using TMPro;

public class DamageIndicator : MonoBehaviour
{
    public TextMeshPro text;
    public float speed = 1f;
    public float life = 1f;

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
        life -= Time.deltaTime;
        if (life <= 0) Destroy(gameObject);
    }

    public void Setup(int dmg)
    {
        text.text = dmg.ToString();
    }
}
