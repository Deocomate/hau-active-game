using UnityEngine;
using UnityEngine.SceneManagement;

public class Wall_BackUp : MonoBehaviour
{
    private float speed;
    private Vector3 targetPosition = new Vector3(0, 0, -10);

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (transform.position == targetPosition)
        {
            Destroy(gameObject);
        }
    }

    // Phát hiện va chạm với Player
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra nếu vật thể chạm vào có Tag là "Player"
        if (other.CompareTag("Player"))
        {
            PlayerManager.gameOver = true;
            Debug.Log("Player hit wall - Game Over!");
            SceneManager.LoadScene("Run");
        }
    }
}
