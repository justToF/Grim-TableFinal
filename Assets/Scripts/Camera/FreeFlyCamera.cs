using UnityEngine;
using Unity.Cinemachine;
using System.IO;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.F;
    public KeyCode screenshotKey = KeyCode.K;
    public float movementSpeed = 10f;
    public float boostMultiplier = 3f;
    public float lookSensitivity = 2f;

    [Header("Screenshots")]
    public string screenshotFolder = "Screenshots";
    public int superSize = 2;

    private CinemachineCamera vcam;
    private bool isFreeFlyActive = false;
    private int originalPriority;
    
    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        originalPriority = vcam.Priority;

        vcam.Priority = 0;
        
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationY = rot.y;
        rotationX = rot.x;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFreeFly();
        }

        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot();
        }

        if (isFreeFlyActive)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    private void ToggleFreeFly()
    {
        isFreeFlyActive = !isFreeFlyActive;
        vcam.Priority = isFreeFlyActive ? 100 : originalPriority;

        if (isFreeFlyActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Vector3 rot = transform.localRotation.eulerAngles;
            rotationY = rot.y;
            rotationX = rot.x;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        Debug.Log("Free Fly Camera: " + (isFreeFlyActive ? "ON" : "OFF"));
    }

    private void HandleMovement()
    {
        float speed = movementSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= boostMultiplier;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float up = 0;
        if (Input.GetKey(KeyCode.E)) up = 1;
        if (Input.GetKey(KeyCode.Q)) up = -1;

        Vector3 move = transform.forward * v + transform.right * h + transform.up * up;
        transform.position += move * speed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        rotationY += Input.GetAxis("Mouse X") * lookSensitivity;
        rotationX -= Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
    }

    private void TakeScreenshot()
    {
        if (!Directory.Exists(screenshotFolder))
        {
            Directory.CreateDirectory(screenshotFolder);
        }

        string fileName = Path.Combine(screenshotFolder, "Screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png");
        ScreenCapture.CaptureScreenshot(fileName, superSize);
        Debug.Log("<color=green>Screenshot captured:</color> " + fileName);
    }
}
