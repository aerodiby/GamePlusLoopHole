using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Player2DController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

    [Header("Ground Check Settings")]
    [Tooltip("Biarkan Default/Everything, script otomatis mengabaikan collider Player sendiri")]
    public LayerMask groundLayer = ~0; // Default: Everything
    public float groundCheckDistance = 0.1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private float moveInput;
    [SerializeField] private bool isGrounded; // Ditampilkan di Inspector untuk debug

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Deteksi tanah otomatis dari titik paling bawah Collider Player_2D
        CheckGround();

        // Lompat dengan Spasi atau W
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Flip arah hadap
        if (moveInput > 0.01f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
        else if (moveInput < -0.01f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
    }

    void CheckGround()
    {
        // Ambil titik tengah bawah dari Collider2D milik Player_2D
        Bounds bounds = col.bounds;
        Vector2 footPosition = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * 0.8f, groundCheckDistance);

        // Cek semua collider di bawah kaki, kecuali collider milik Player_2D sendiri
        Collider2D[] hits = Physics2D.OverlapBoxAll(footPosition, boxSize, 0f, groundLayer);
        isGrounded = false;

        foreach (Collider2D hit in hits)
        {
            if (hit != col && !hit.isTrigger)
            {
                isGrounded = true;
                break;
            }
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (transform.position.z != 0f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }
    }

    void OnDisable()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}