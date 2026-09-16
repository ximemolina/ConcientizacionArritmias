using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int totalContainers = 3;
    public Transform door;
    public Transform doorOpenTarget;
    public float doorSpeed = 2f;

    private int filledContainers = 0;
    private bool gameWon = false;
    private bool doorTargetCached;
    private Vector3 doorSlideTarget;
    private Transform doorPanel;

    [SerializeField] private QuadImageSlideshow slideshow;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (gameWon && door != null)
        {
            if (!doorTargetCached)
            {
                doorPanel = SlidingDoorMotion.GetSlidingPanel(door);
                SlidingDoorMotion.AttachSlidingHardware(door, doorPanel);
                Vector3 openPoint = doorOpenTarget != null
                    ? doorOpenTarget.position
                    : doorPanel.position + doorPanel.right * SlidingDoorMotion.DefaultSlideDistance;
                doorSlideTarget = SlidingDoorMotion.GetOpenLocalPosition(doorPanel, openPoint);
                doorTargetCached = true;
            }

            if (doorPanel != null)
                SlidingDoorMotion.MoveLocalTowards(doorPanel, doorSlideTarget, doorSpeed);
        }
    }

    public void OnContainerFilled()
    {
        filledContainers++;
        if (filledContainers >= totalContainers)
        {
            gameWon = true;
            doorTargetCached = false;
            Debug.Log("You win! Door opening.");
            slideshow.StartSlideshow();
        }
    }

    public void OnContainerEmptied()
    {
        filledContainers = Mathf.Max(0, filledContainers - 1);
        gameWon = false;
    }
}