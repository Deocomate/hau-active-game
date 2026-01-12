using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lớp này giúp gom nhóm các thông số của từng Mini Game lại cho gọn
[System.Serializable]
public class MiniGameData
{
    public string gameName;              // Tên để bạn dễ phân biệt (Fruit, Hole...)
    public GameObject prefab;            // Kéo file Prefab Portal vào đây
    public float nextSpawnZ = 500f;      // Mốc quãng đường đầu tiên sẽ xuất hiện
    public float minRandomDist = 0f;     // Khoảng cách ngẫu nhiên thêm vào (tối thiểu)
    public float maxRandomDist = 100f;   // Khoảng cách ngẫu nhiên thêm vào (tối đa)
    public float distanceBetweenSpawns = 500f; // Khoảng cách giãn cách sau mỗi lần ăn
    public float[] lanes = { -3f, 0f, 3f };    // Các làn đường mà game này có thể xuất hiện
}

public class FloorManager : MonoBehaviour
{
    [Header("Floor Prefabs")]
    public GameObject[] hallwayPrefabs;
    public GameObject[] shortFloorPrefabs;
    public GameObject[] longFloorPrefabs;
    public GameObject[] startPrefabs;

    [Header("Mini Games Settings")]
    public MiniGameData fruitGame; // Thông số cho game Fruit
    public MiniGameData holeGame;  // Thông số cho game Hole

    [Header("General Settings")]
    public Transform playerTransform;
    public int numberOfFloors = 5;
    public float hallwayLength = 74f;
    public float shortFloorLength = 74f;
    public float longFloorLength = 105f;

    private List<GameObject> activeFloors = new List<GameObject>();
    private float zSpawn = 0;
    private bool spawnHallways = false;

    void Start()
    {
        // Khởi tạo mốc ngẫu nhiên ban đầu cho cả 2 mini-game để tránh chúng luôn cố định
        fruitGame.nextSpawnZ += Random.Range(fruitGame.minRandomDist, fruitGame.maxRandomDist);
        holeGame.nextSpawnZ += Random.Range(holeGame.minRandomDist, holeGame.maxRandomDist);

        // Tạo các đoạn đường ban đầu
        for (int i = 0; i < numberOfFloors; i++)
        {
            if (i == 0) StartFloor(Random.Range(0, startPrefabs.Length));
            else if (spawnHallways) SpawnRandomHallways();
            else SpawnInitialFloor();
        }
    }

    void Update()
    {
        // Kiểm tra nếu người chơi chạy gần hết đường thì spawn thêm đường mới
        if (playerTransform.position.z > zSpawn - (numberOfFloors * shortFloorLength))
        {
            if (spawnHallways) SpawnRandomHallways();
            else SpawnInitialFloor();
            DeleteFloors();
        }
    }

    // Hàm dùng chung để kiểm tra và đặt Mini Game lên sàn
    private void CheckAndSpawnMiniGame(MiniGameData game, float currentZ, float floorLength)
    {
        if (game.prefab == null) return;

        // Nếu điểm mục tiêu NextSpawnZ nằm trong phạm vi của đoạn sàn đang xây
        if (game.nextSpawnZ >= currentZ && game.nextSpawnZ < currentZ + floorLength)
        {
            // 1. Chọn làn ngẫu nhiên từ danh sách lanes riêng của game đó
            float randomX = game.lanes[Random.Range(0, game.lanes.Length)];

            // 2. Tạo Portal (độ cao Y = 1.5f để lơ lửng)
            Vector3 spawnPos = new Vector3(randomX, 1.5f, game.nextSpawnZ);
            GameObject miniGameObj = Instantiate(game.prefab, spawnPos, Quaternion.identity);

            // 3. Gắn Portal làm con của đoạn sàn vừa tạo để nó tự biến mất khi sàn bị xóa
            miniGameObj.transform.parent = activeFloors[activeFloors.Count - 1].transform;

            // 4. Tính toán mốc xuất hiện tiếp theo
            game.nextSpawnZ += game.distanceBetweenSpawns + Random.Range(game.minRandomDist, game.maxRandomDist);

            Debug.Log("<color=green>Spawned " + game.gameName + "</color> at Z: " + spawnPos.z);
        }
    }

    private void SpawnFloor(GameObject[] floorPrefabs, float floorLength)
    {
        int floorIndex = Random.Range(0, floorPrefabs.Length);
        GameObject go = Instantiate(floorPrefabs[floorIndex], new Vector3(0, 0, zSpawn), Quaternion.identity);
        activeFloors.Add(go);

        // Kiểm tra cho cả 2 loại mini game mỗi khi xây sàn mới
        CheckAndSpawnMiniGame(fruitGame, zSpawn, floorLength);
        CheckAndSpawnMiniGame(holeGame, zSpawn, floorLength);

        zSpawn += floorLength;
    }

    private void StartFloor(int floorIndex)
    {
        GameObject go = Instantiate(startPrefabs[floorIndex], new Vector3(0, 0, zSpawn), Quaternion.identity);
        activeFloors.Add(go);
        float currentLength = go.name.Contains("Short") ? shortFloorLength : longFloorLength;

        CheckAndSpawnMiniGame(fruitGame, zSpawn, currentLength);
        CheckAndSpawnMiniGame(holeGame, zSpawn, currentLength);

        zSpawn += currentLength;
    }

    private void SpawnInitialFloor()
    {
        if (Random.value > 0.5f) SpawnFloor(shortFloorPrefabs, shortFloorLength);
        else SpawnFloor(longFloorPrefabs, longFloorLength);
        spawnHallways = true;
    }

    private void SpawnRandomHallways()
    {
        int hallwayCount = Random.Range(2, 6);
        for (int i = 0; i < hallwayCount; i++) SpawnFloor(hallwayPrefabs, hallwayLength);
        spawnHallways = false;
    }

    private void DeleteFloors()
    {
        float playerZ = playerTransform.position.z;
        int floorsToDelete = 0;
        for (int i = 0; i < activeFloors.Count; i++)
        {
            if (activeFloors[i].transform.position.z < playerZ - 100f) floorsToDelete++;
            else break;
        }
        for (int i = 0; i < floorsToDelete; i++)
        {
            Destroy(activeFloors[0]);
            activeFloors.RemoveAt(0);
        }
    }
}