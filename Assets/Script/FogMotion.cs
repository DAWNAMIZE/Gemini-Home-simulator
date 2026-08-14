using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FogMotion : MonoBehaviour
{
    public float scrollSpeed = 0.02f;
    private Renderer rend;

    float offset;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        if (rend.material.HasProperty("_MainTex"))
        {
            offset = Time.time * scrollSpeed;
            rend.material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }
    }
}
