using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private Renderer rendererObject;
    public Color defaultColor;

    void Start()
    {
        rendererObject = GetComponent<Renderer>();
        rendererObject.material.color = defaultColor;
    }
}
