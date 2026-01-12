using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class HandTracking : MonoBehaviour
{
    public SocketClient socketClient;
    public GameObject[] landmarkPoints;

    void Start()
    {
        if (socketClient == null)
        {
            socketClient = FindObjectOfType<SocketClient>();
            if (socketClient == null)
            {
                Debug.LogError("SocketClient is not assigned and not found in the scene.");
                return;
            }
        }
    }

    void Update()
    {
        if (socketClient == null) return;
        
        string data = socketClient.Data;
        
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        try
        {
            // Remove all brackets, parentheses and extra spaces
            data = data.Replace("[", "").Replace("]", "");
            data = data.Replace("(", "").Replace(")", "");
            data = data.Replace(" ", "");

            string[] points = data.Split(',');

            // Ensure we have enough data points (9 landmarks * 2 coordinates = 18)
            if (points.Length < 18)
            {
                return;
            }

            for (int i = 0; i < 9; i++)
            {
                // Use TryParse for safer parsing
                if (!float.TryParse(points[i * 2], NumberStyles.Any, CultureInfo.InvariantCulture, out float xRaw) ||
                    !float.TryParse(points[i * 2 + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out float yRaw))
                {
                    continue; // Skip this point if parsing fails
                }

                float x = 2.5f + xRaw / (-90);
                float y = 3.5f + -(yRaw / 100);

                landmarkPoints[i].transform.localPosition = new Vector3(x, y, 30);

                if (i == 1)
                {
                    landmarkPoints[9].transform.localPosition = new Vector3(x, y - 0.7f, 30);
                    landmarkPoints[10].transform.localPosition = new Vector3(x, y - 1.5f, 30);
                }

                if (i == 0)
                {
                    landmarkPoints[11].transform.localPosition = new Vector3(x, y - 0.7f, 30);
                    landmarkPoints[12].transform.localPosition = new Vector3(x, y - 1.5f, 30);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error parsing hand tracking data: {e.Message}");
        }
    }
}
