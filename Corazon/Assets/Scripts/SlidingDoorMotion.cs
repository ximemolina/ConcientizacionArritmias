using UnityEngine;

/// <summary>
/// Slides the visible barn-door leaf along its local X rail.
/// </summary>
public static class SlidingDoorMotion
{
    public const float DefaultSlideDistance = 2.2f;

    public static Transform GetSlidingPanel(Transform doorOrRoot)
    {
        if (doorOrRoot == null)
            return null;

        Transform panel = doorOrRoot;
        if (!IsEntireExitDoor(doorOrRoot))
        {
            panel = FindNamed(doorOrRoot, "Door");
            if (panel == null)
                panel = FindLargestRenderer(doorOrRoot);
            if (panel == null)
                panel = doorOrRoot;
        }

        PrepareForRuntimeMove(doorOrRoot);
        return panel;
    }

    /// <param name="forcedSlideDistance">
    /// If greater than 0, slide exactly this far on local X (ignores the open marker).
    /// </param>
    public static Vector3 GetOpenLocalPosition(Transform panel, Vector3 openPointWorld, float forcedSlideDistance = -1f)
    {
        Transform leaf = panel != null ? FindNamed(panel, "Door") : null;
        if (leaf != null && leaf != panel)
        {
            Vector3 leafTarget = SlideAlongLocalX(leaf, openPointWorld, forcedSlideDistance);
            Vector3 worldDelta = leaf.TransformVector(leafTarget - leaf.localPosition);
            if (panel.parent != null)
                return panel.localPosition + panel.parent.InverseTransformVector(worldDelta);
            return panel.localPosition + worldDelta;
        }

        return SlideAlongLocalX(panel, openPointWorld, forcedSlideDistance);
    }

    private static Vector3 SlideAlongLocalX(Transform panel, Vector3 openPointWorld, float forcedSlideDistance)
    {
        Vector3 local = panel.localPosition;
        float dx = DefaultSlideDistance;

        if (forcedSlideDistance > 0f)
        {
            dx = forcedSlideDistance;
        }
        else if (panel.parent != null)
        {
            float openX = panel.parent.InverseTransformPoint(openPointWorld).x;
            float fromOpen = openX - local.x;
            if (Mathf.Abs(fromOpen) >= 0.25f)
                dx = Mathf.Clamp(fromOpen, -3.5f, 3.5f);
        }

        local.x += dx;
        return local;
    }

    /// <summary>
    /// Parents hanger wheels, axles and other sliding hardware to the leaf.
    /// The long rail stays on the frame.
    /// </summary>
    public static void AttachSlidingHardware(Transform doorRoot, Transform panel)
    {
        if (doorRoot == null || panel == null)
            return;

        // Whole PuertaSalidaMinijuego* already moves as one object.
        if (panel == doorRoot || IsEntireExitDoor(doorRoot) || IsEntireExitDoor(panel))
            return;

        Transform hanger = FindNamed(doorRoot, "Hanger");
        Transform assembly = hanger != null ? hanger : doorRoot;

        // Door is nested under a wheel in the prefab; lift it first so those
        // wheels can be parented onto the leaf.
        if (panel != assembly && panel.IsChildOf(assembly) && panel.parent != assembly)
            panel.SetParent(assembly, true);

        Transform rail = FindRailChild(assembly, panel);

        for (int i = assembly.childCount - 1; i >= 0; i--)
        {
            Transform child = assembly.GetChild(i);
            if (child == panel || child == rail)
                continue;
            if (child.GetComponentInChildren<Renderer>(true) == null)
                continue;

            child.SetParent(panel, true);
        }
    }

    public static bool MoveLocalTowards(Transform panel, Vector3 localTarget, float speed)
    {
        panel.localPosition = Vector3.MoveTowards(panel.localPosition, localTarget, speed * Time.deltaTime);
        return (panel.localPosition - localTarget).sqrMagnitude < 0.000001f;
    }

    private static bool IsEntireExitDoor(Transform t)
    {
        return t != null && t.name.StartsWith("PuertaSalida");
    }

    private static Transform FindNamed(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name == name)
                return child;
        }
        return null;
    }

    private static Transform FindLargestRenderer(Transform root)
    {
        Transform best = null;
        float bestSize = -1f;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Vector3 size = renderer.bounds.size;
            float volume = size.x * size.y * size.z;
            if (volume > bestSize)
            {
                bestSize = volume;
                best = renderer.transform;
            }
        }
        return best;
    }

    private static Transform FindRailChild(Transform hanger, Transform panel)
    {
        Transform best = null;
        float bestLength = -1f;

        for (int i = 0; i < hanger.childCount; i++)
        {
            Transform child = hanger.GetChild(i);
            if (child == panel)
                continue;

            Bounds bounds = GetCombinedBounds(child);
            float length = Mathf.Max(bounds.size.x, bounds.size.z);
            string n = child.name.ToLowerInvariant();
            if (n.Contains("cube") || n.Contains("rail") || n.Contains("track"))
                length += 10f;

            if (length > bestLength)
            {
                bestLength = length;
                best = child;
            }
        }

        return best;
    }

    private static Bounds GetCombinedBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void PrepareForRuntimeMove(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.isStatic = false;
            var animator = t.GetComponent<Animator>();
            if (animator != null)
                animator.enabled = false;
        }
    }
}
