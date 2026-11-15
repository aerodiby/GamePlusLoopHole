using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Player2DController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

    [Header("Ground Check Settings")]
    public LayerMask groundLayer = ~0;
    public float groundCheckDistance = 0.1f;

    [Header("Animation Reference")]
    [Tooltip("Biarkan kosong jika Animator dipasang langsung di Player_2D")]
    public Animator animator;

    // Nama Parameter di Window Animator (Harus sama persis tulisannya)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int YVelocityHash = Animator.StringToHash("YVelocity");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private Rigidbody2D rb;
    private Collider2D col;
    private float moveInput;
    [SerializeField] private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;

        // Otomatis cari komponen Animator di objek ini atau child-nya
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // 1. Cek apakah kaki menyentuh tanah / Proxy_2D
        CheckGround();

        // 2. Input Lompat
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 3. Balik arah hadap sprite (Flip Kiri / Kanan)
        if (moveInput > 0.01f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
        else if (moveInput < -0.01f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);

        // 4. Update Parameter Animasi
        UpdateAnimations();
    }

    void CheckGround()
    {
        Bounds bounds = col.bounds;
        Vector2 footPosition = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * 0.8f, groundCheckDistance);

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

    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat(SpeedHash, Mathf.Abs(moveInput));
        animator.SetFloat(YVelocityHash, rb.linearVelocity.y);
        animator.SetBool(IsGroundedHash, isGrounded);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Kunci posisi Z tetap di bidang 0
        if (transform.position.z != 0f)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }
    }

    void OnDisable()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Reset ke animasi Idle saat pindah ke Mode 3D
        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetFloat(YVelocityHash, 0f);
            animator.SetBool(IsGroundedHash, true);
        }
    }
}