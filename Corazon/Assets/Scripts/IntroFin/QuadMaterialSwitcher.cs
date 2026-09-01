using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Renderer))]
public class QuadMaterialSwitcher : MonoBehaviour
{
    [SerializeField] private Material material1;
    [SerializeField] private Material material2;

    [Header("Eventos")]
    public UnityEvent onSwitchToMaterial1;
    public UnityEvent onSwitchToMaterial2;

    private Renderer quadRenderer;

    private void Awake()
    {
        quadRenderer = GetComponent<Renderer>();
    }

    public void SwitchToMaterial1()
    {
        if (material1 != null)
        {
            quadRenderer.material = material1;
            onSwitchToMaterial1.Invoke();
        }
        else
            Debug.LogWarning("Material 1 no asignado.");
    }

    public void SwitchToMaterial2()
    {
        if (material2 != null)
        {
            quadRenderer.material = material2;
            onSwitchToMaterial2.Invoke();
        }
        else
            Debug.LogWarning("Material 2 no asignado.");
    }
}
