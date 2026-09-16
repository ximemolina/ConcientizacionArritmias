using UnityEngine;

public class GameManager2 : MonoBehaviour
{
    public static GameManager2 Instance;

    [SerializeField] private Transform door;
    [SerializeField] private Transform doorOpenPoint;
    [SerializeField] private float doorSpeed = 2f;
    [SerializeField] private float doorSlideDistance = 1.45f;

    public bool gameWon = false;

    private bool doorTargetCached;
    private Vector3 doorSlideTarget;
    private Transform doorPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!gameWon || door == null)
            return;

        if (!doorTargetCached)
        {
            doorPanel = SlidingDoorMotion.GetSlidingPanel(door);
            SlidingDoorMotion.AttachSlidingHardware(door, doorPanel);
            Vector3 openPoint = doorOpenPoint != null ? doorOpenPoint.position : doorPanel.position + doorPanel.right * SlidingDoorMotion.DefaultSlideDistance;
            doorSlideTarget = SlidingDoorMotion.GetOpenLocalPosition(doorPanel, openPoint, doorSlideDistance);
            doorTargetCached = true;
        }

        if (doorPanel != null)
            SlidingDoorMotion.MoveLocalTowards(doorPanel, doorSlideTarget, doorSpeed);
    }
}
