using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Controls the cursor using hand tracking data from MediaPipe.
/// NOTE: This controller uses RIGHT HAND ONLY for menu selection.
/// The left hand is disabled at the Python backend level (detection.py)
/// to avoid conflicts when both hands are visible to the camera.
/// </summary>
public class CursorController : MonoBehaviour
{
    public SocketClient socketClient;
    public RectTransform cursorTransform;
    public float hoverDuration = 2f;
    private float hoverTimer = 0f;
    private Button hoveredButton = null;
    private Canvas currentCanvas;

    void Start()
    {
        InitializeSocketClient();
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
            // Clean data - remove all brackets, parentheses and spaces
            data = data.Replace("[", "").Replace("]", "");
            data = data.Replace("(", "").Replace(")", "");
            data = data.Replace(" ", "");

            string[] points = data.Split(',');

            // Hand coordinates are at index 18 and 19 (after 9 landmarks * 2 coords each)
            // Check bounds before accessing
            if (points.Length < 20)
            {
                return;
            }

            // Use TryParse for safer parsing with invariant culture
            if (!float.TryParse(points[18], NumberStyles.Any, CultureInfo.InvariantCulture, out float handX) ||
                !float.TryParse(points[19], NumberStyles.Any, CultureInfo.InvariantCulture, out float handY))
            {
                return;
            }

            float x = 400 + handX * -2;
            float y = 250 + -(handY * 2);

            cursorTransform.localPosition = new Vector3(x, y, 0);

            CheckForHoverAndClick();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error parsing cursor data: {e.Message}");
        }
    }

    void CheckForHoverAndClick()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = cursorTransform.position
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            Button button = result.gameObject.GetComponent<Button>();
            if (button != null)
            {
                Debug.Log("Hovered over button: " + button.name);
                if (hoveredButton == button)
                {
                    hoverTimer += Time.deltaTime;
                    if (hoverTimer >= hoverDuration)
                    {
                        button.onClick.Invoke();
                        Debug.Log("Button clicked: " + button.name);
                        hoverTimer = 0f;
                    }
                }
                else
                {
                    hoveredButton = button;
                    hoverTimer = 0f;
                }
                return;
            }
        }

        // Reset if no button is hovered
        hoveredButton = null;
        hoverTimer = 0f;
    }

    public void SetCursorCanvas(Canvas canvas)
    {
        currentCanvas = canvas;
        cursorTransform.SetParent(currentCanvas.transform, false);
    }

    private void InitializeSocketClient()
    {
        if (socketClient == null)
        {
            socketClient = FindObjectOfType<SocketClient>();
            if (socketClient == null)
            {
                Debug.LogError("SocketClient is not assigned and not found in the scene.");
            }
        }
    }
}
