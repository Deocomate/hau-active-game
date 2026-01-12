using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc phải có thư viện này để chuyển cảnh

public class HolePortal : MonoBehaviour
{
    [Header("Cấu hình xoay")]
    public float rotationSpeed = 20f;

    void Update()
    {
        // Hiệu ứng xoay tròn vật thể quanh trục Y
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    // Hàm này kích hoạt khi người chơi bước vào vùng Trigger của cổng
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra nếu vật thể chạm vào có Tag là "Player"
        if (other.CompareTag("Player"))
        {
            // Lưu lại điểm số hiện tại vào PlayerPrefs trước khi chuyển cảnh
            int currentPoint = Mathf.FloorToInt(PlayerManager.point);
            PlayerPrefs.SetInt("LastScore", currentPoint);

            // Chuyển sang màn chơi tên là "Hole"
            SceneManager.LoadScene("Hole");

            Debug.Log("Đang chuyển sang màn chơi Hole...");
        }
    }
}