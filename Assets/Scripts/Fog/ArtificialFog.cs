using UnityEngine;

public class ArtificialFog : MonoBehaviour
{
    public Material fogMaterial;  // material transparente con color de niebla
    public float maxOpacity = 0.5f;
    public float speed = 0.2f;

    private float t = 0f;

    void Update()
    {
        t += Time.deltaTime * speed;
        float alpha = Mathf.PingPong(t, maxOpacity);
        Color c = fogMaterial.color;
        c.a = alpha;
        fogMaterial.color = c;
    }
}
