using UnityEngine;
using UnityEngine.InputSystem;

public class RTSCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 50f;
    public float smoothing = 15f;
    public Vector2 boundaryMin = new Vector2(0, 0);
    public Vector2 boundaryMax = new Vector2(100, 100);

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 10f;
    public float maxZoom = 80f;
    public float startHeight = 40f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 90f;
    [Tooltip("The vertical angle. 90 is looking straight down. 45 is standard Isometric.")]
    public float lookDownAngle = 60f;
    [Tooltip("The starting horizontal angle. 0 = North, 90 = East. Set to -45 for Diagonal.")]
    public float initialYRotation = 0f; // Changed to 0 for "Straight" alignment

    private Vector3 targetPosition;
    private float targetRotationY;
    private Camera cam;

    // Internal Input Action Storage
    private InputAction moveAction;
    private InputAction zoomAction;
    private InputAction rotateAction;
    private InputActionMap rtsInputMap;

    private void Start()
    {
        cam = GetComponent<Camera>();

        // --- 1. Auto-Center the Camera ---
        float midX = (boundaryMin.x + boundaryMax.x) / 2f;
        float midZ = (boundaryMin.y + boundaryMax.y) / 2f;

        targetPosition = new Vector3(midX, startHeight, midZ);
        transform.position = targetPosition;

        // --- 2. Set Rotation from Inspector Variables ---
        targetRotationY = initialYRotation;
        transform.rotation = Quaternion.Euler(lookDownAngle, targetRotationY, 0);

        SetupInput();
    }

    private void SetupInput()
    {
        rtsInputMap = new InputActionMap("RTS");

        moveAction = rtsInputMap.AddAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        zoomAction = rtsInputMap.AddAction("Zoom", binding: "<Mouse>/scroll/y");

        rotateAction = rtsInputMap.AddAction("Rotate", binding: "<Keyboard>/q");
        rotateAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/q")
            .With("Positive", "<Keyboard>/e");

        rtsInputMap.Enable();
    }

    private void OnDestroy()
    {
        if (rtsInputMap != null) rtsInputMap.Disable();
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();

        // Smoothly move
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);

        // Smoothly rotate using the Inspector 'LookDownAngle'
        Quaternion targetRot = Quaternion.Euler(lookDownAngle, targetRotationY, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothing);
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (forward * input.y + right * input.x).normalized;
        targetPosition += moveDir * moveSpeed * Time.deltaTime;

        targetPosition.x = Mathf.Clamp(targetPosition.x, boundaryMin.x, boundaryMax.x);
        targetPosition.z = Mathf.Clamp(targetPosition.z, boundaryMin.y, boundaryMax.y);
    }

    void HandleZoom()
    {
        float scroll = zoomAction.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.1f)
        {
            targetPosition.y -= Mathf.Sign(scroll) * zoomSpeed;
            targetPosition.y = Mathf.Clamp(targetPosition.y, minZoom, maxZoom);
        }
    }

    void HandleRotation()
    {
        float input = rotateAction.ReadValue<float>();
        if (input != 0 && Quaternion.Angle(transform.rotation, Quaternion.Euler(lookDownAngle, targetRotationY, 0)) < 5f)
        {
            targetRotationY += input * 90f;
        }
    }
}