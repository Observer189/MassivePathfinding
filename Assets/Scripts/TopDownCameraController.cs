using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopDownCameraController : MonoBehaviour
{
    public float baseMoveSpeed = 10f; // Base camera movement speed
    public float zoomSpeed = 5f; // Camera zoom speed
    public float minZoom = 1f; // Minimum camera zoom
    public float maxZoom = 10f; // Maximum camera zoom
    public int mousePositionSamples = 5; // Number of mouse position samples to average

    private Vector3 dragStartPosition; // Mouse drag start position
    private bool isDragging; // Flag to indicate if the mouse is dragging the camera
    private Queue<Vector3> mousePositionQueue; // Queue for storing mouse position samples

    void Start()
    {
        mousePositionQueue = new Queue<Vector3>(mousePositionSamples);
    }

    void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();
    }

    void HandleKeyboardInput()
    {
        float horizontalMove = Input.GetAxisRaw("Horizontal");
        float verticalMove = Input.GetAxisRaw("Vertical");

        float zoomFactor = Camera.main.orthographicSize / maxZoom;
        float adaptiveMoveSpeed = baseMoveSpeed * Mathf.Lerp(1f, 5f, zoomFactor); // Adjust the range (1f, 5f) as needed

        Vector3 movement = new Vector3(horizontalMove, verticalMove, 0) * adaptiveMoveSpeed * Time.deltaTime;
        transform.position += movement;
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(2)) // Left mouse button pressed
        {
            dragStartPosition = GetAverageMousePosition();
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(2)) // Left mouse button released
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 dragCurrentPosition = GetAverageMousePosition();
            Vector3 dragDelta = dragStartPosition - dragCurrentPosition;

            float zoomFactor = Camera.main.orthographicSize / maxZoom;
            float adaptiveMoveSpeed = baseMoveSpeed * Mathf.Lerp(1f, 5f, zoomFactor); // Adjust the range (1f, 5f) as needed

            transform.position += new Vector3(dragDelta.x, dragDelta.y, 0) * adaptiveMoveSpeed * Time.deltaTime;

            dragStartPosition = dragCurrentPosition;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            float newZoom = Mathf.Clamp(Camera.main.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
            Camera.main.orthographicSize = newZoom;
        }
    }

    Vector3 GetAverageMousePosition()
    {
        Vector3 averageMousePosition = Vector3.zero;

        // Add the current mouse position to the queue
        mousePositionQueue.Enqueue(Input.mousePosition);

        // Calculate the average mouse position from the queue
        int sampleCount = Mathf.Min(mousePositionQueue.Count, mousePositionSamples);
        for (int i = 0; i < sampleCount; i++)
        {
            averageMousePosition += mousePositionQueue.Dequeue();
        }
        averageMousePosition /= sampleCount;

        // Re-add the dequeued elements back to the queue
        for (int i = 0; i < sampleCount - 1; i++)
        {
            mousePositionQueue.Enqueue(mousePositionQueue.Dequeue());
        }

        return averageMousePosition;
    }
}
