using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("카메라의 이동 속도")]
    public float moveSpeed = 5.0f;

    [Tooltip("마우스 회전 감도")]
    public float lookSpeed = 2.0f;

    private float rotationX = 0;
    private float rotationY = 0;

    void Update()
    {
        // --- 카메라 이동 (키보드 WASD, QE) ---
        float moveX = Input.GetAxis("Horizontal"); // A, D 키
        float moveZ = Input.GetAxis("Vertical");   // W, S 키

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.position += move * moveSpeed * Time.deltaTime;

        // 위/아래 이동 (Q, E 키)
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += transform.up * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position -= transform.up * moveSpeed * Time.deltaTime;
        }

        // --- 카메라 회전 (마우스 오른쪽 버튼 누른 상태) ---
        if (Input.GetMouseButton(1)) // 마우스 오른쪽 버튼
        {
            // 커서 숨기기 및 고정
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY += Input.GetAxis("Mouse X") * lookSpeed;

            // 상하 회전 각도 제한 (-90도 ~ 90도)
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
        else
        {
            // 마우스 오른쪽 버튼을 떼면 커서 복구
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}