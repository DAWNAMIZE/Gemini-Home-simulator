using UnityEngine;

public class TouchLook : MonoBehaviour
{
    public Camera playerCamera;
    public float sensitivity = 0.15f;
    public float maxVerticalAngle = 80f;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        yaw = transform.eulerAngles.y;
        pitch = playerCamera.transform.localEulerAngles.x;
        if (pitch > 180) pitch -= 360f;
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                Vector2 delta = t.deltaPosition;

                yaw += delta.x * sensitivity;
                pitch -= delta.y * sensitivity;
                pitch = Mathf.Clamp(pitch, -maxVerticalAngle, maxVerticalAngle);

                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
    }
}
