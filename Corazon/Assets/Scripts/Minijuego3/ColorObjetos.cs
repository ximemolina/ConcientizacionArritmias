using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private Renderer rendererObject;
    public Color defaultColor;

    void Start()
    {
        rendererObject = GetComponent<Renderer>();
        Material mat = rendererObject.material;
        Color color = defaultColor;
        color.a = 1f;
        mat.color = color;

        bool glowBackground = color.b >= color.r && color.b >= color.g;
        if (glowBackground && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 1.8f);
        }
    }
}
