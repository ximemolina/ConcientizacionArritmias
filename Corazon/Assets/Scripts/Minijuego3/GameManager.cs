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

    [SerializeField] private QuadImageSlideshow slideshow;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (gameWon)
        {
            Vector3 currentPos = door.position;
            float newZ = Mathf.MoveTowards(currentPos.z, doorOpenTarget.position.z, Time.deltaTime * doorSpeed);

            door.position = new Vector3(currentPos.x, currentPos.y, newZ);
        }
    }

    public void OnContainerFilled()
    {
        filledContainers++;
        if (filledContainers >= totalContainers)
        {
            gameWon = true;
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