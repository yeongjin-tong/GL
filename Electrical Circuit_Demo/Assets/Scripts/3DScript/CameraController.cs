using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("ī�޶��� �̵� �ӵ�")]
    public float moveSpeed = 5.0f;

    [Tooltip("���콺 ȸ�� ����")]
    public float lookSpeed = 2.0f;

    private float rotationX = 0;
    private float rotationY = 0;

    void Update()
    {
        // --- ī�޶� �̵� (Ű���� WASD, QE) ---
        float moveX = Input.GetAxis("Horizontal"); // A, D Ű
        float moveZ = Input.GetAxis("Vertical");   // W, S Ű

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.position += move * moveSpeed * Time.deltaTime;

        // ��/�Ʒ� �̵� (Q, E Ű)
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += transform.up * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position -= transform.up * moveSpeed * Time.deltaTime;
        }

        // --- ī�޶� ȸ�� (���콺 ������ ��ư ���� ����) ---
        if (Input.GetMouseButton(1)) // ���콺 ������ ��ư
        {
            // Ŀ�� ����� �� ����
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY += Input.GetAxis("Mouse X") * lookSpeed;

            // ���� ȸ�� ���� ���� (-90�� ~ 90��)
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
        else
        {
            // ���콺 ������ ��ư�� ���� Ŀ�� ����
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}