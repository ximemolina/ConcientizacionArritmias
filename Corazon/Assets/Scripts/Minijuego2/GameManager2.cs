using UnityEngine;

public class GameManager2 : MonoBehaviour
{
    public static GameManager2 Instance;

    [SerializeField] private Transform door;
    [SerializeField] private Transform doorOpenPoint;
    [SerializeField] private float doorSpeed = 2f;

    public bool gameWon = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (gameWon)
        {
            Vector3 targetPosition = new Vector3(
                door.position.x,          // Mantiene X actual
                door.position.y,          // Mantiene Y actual
                doorOpenPoint.position.z  // Solo toma la Z del punto destino
            );

            door.position = Vector3.MoveTowards(
                door.position,
                targetPosition,
                doorSpeed * Time.deltaTime
            );
        }
    }
}
