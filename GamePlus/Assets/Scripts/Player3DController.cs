using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player3DController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float gravity = -19.62f;

    [Header("Mouse Look Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    private float xRotation = 0f;

    [Header("Grab / Push Box Settings")]
    [Tooltip("Jarak maksimal mengambil box")]
    public float interactRange = 4f;
    [Tooltip("Kecepatan box mengikuti titik pegangan")]
    public float boxFollowSpeed = 12f;
    [Tooltip("Tag untuk Box_3D yang bisa didorong/ditarik")]
    public string pushableTag = "Pushable";

    private CharacterController controller;
    private Vector3 velocity;
    
    // Variabel untuk sistem Grab/Push
    private Rigidbody grabbedBox;
    private float currentHoldDistance;
    private float lockedBoxY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleBoxInteraction();
    }

    void FixedUpdate()
    {
        // Menggerakkan box dengan mulus menggunakan fisika (tidak tembus tembok 3D)
        if (grabbedBox != null)
        {
            MoveGrabbedBox();
        }
    }

    void HandleMouseLook()
    {
        if (playerCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxisRaw("Horizontal"); // A & D
        float z = Input.GetAxisRaw("Vertical");   // W & S

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleBoxInteraction()
    {
        // Tekan Klik Kiri atau E untuk mengambil / melepas Box
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            if (grabbedBox == null)
                TryGrabBox();
            else
                ReleaseBox();
        }

        // Scroll Mouse untuk memajukan/memundurkan box secara presisi saat dipegang
        if (grabbedBox != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentHoldDistance = Mathf.Clamp(currentHoldDistance + scroll * 2f, 1.5f, interactRange + 2f);
            }
        }
    }

    void TryGrabBox()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            if (hit.collider.CompareTag(pushableTag) && hit.rigidbody != null)
            {
                grabbedBox = hit.rigidbody;
                currentHoldDistance = Vector3.Distance(playerCamera.position, grabbedBox.position);
                lockedBoxY = grabbedBox.position.y;

                // Kunci rotasi dan gravitasi sementara agar mudah digeser di sumbu X dan Z
                grabbedBox.useGravity = false;
                grabbedBox.freezeRotation = true;
            }
        }
    }

    public void ReleaseBox()
    {
        if (grabbedBox != null)
        {
            grabbedBox.useGravity = true;
            grabbedBox.linearVelocity = Vector3.zero;
            grabbedBox = null;
        }
    }

    void MoveGrabbedBox()
    {
        Vector3 targetPoint = playerCamera.position + playerCamera.forward * currentHoldDistance;
        
        // Jaga agar ketinggian (Y) box tetap stabil di lantai saat digeser
        targetPoint.y = lockedBoxY;

        Vector3 direction = targetPoint - grabbedBox.position;
        grabbedBox.linearVelocity = direction * boxFollowSpeed;

        // Paksa rotasi box tetap lurus (0,0,0) agar bentuk collider 2D tetap kotak sempurna
        grabbedBox.MoveRotation(Quaternion.identity);
    }

    void OnDisable()
    {
        // Otomatis lepas box jika pemain menekan Tab untuk pindah ke Mode 2D
        ReleaseBox();
    }
}