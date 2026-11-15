using UnityEngine;

public class CameraYController : MonoBehaviour
{
    public static CameraYController Instance { get; private set; }

    [Header("Target Reference")]
    [Tooltip("Tarik objek Player_2D ke sini")]
    public Transform player2D;

    [Header("Y Position Settings")]
    [Tooltip("Ketinggian Y default jika Player sedang tidak berada di dalam collider zona manapun")]
    public float defaultY = 1.5f;

    [Tooltip("Kecepatan transisi kamera saat berpindah zona Y")]
    public float yTransitionSpeed = 5f;

    [Header("Debug Info (Read-Only)")]
    [SerializeField] private string currentActiveZoneName = "None (Default)";
    [SerializeField] private float targetY;

    private float currentY;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        targetY = defaultY;
        currentY = defaultY;
    }

    void LateUpdate()
    {
        if (player2D == null) return;

        // 1. Selalu cek setiap frame: Player_2D sedang berada di dalam Collider Zona mana?
        CheckCurrentZone();

        // 2. Transisi mulus menuju targetY dari zona yang sedang ditempati
        currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * yTransitionSpeed);

        // 3. Sumbu X mengikuti Player_2D, Sumbu Y terkunci di currentY
        transform.position = new Vector3(player2D.position.x, currentY, player2D.position.z);
    }

    void CheckCurrentZone()
    {
        // Mengecek semua Collider2D yang melingkupi titik posisi Player_2D saat ini
        Collider2D[] hits = Physics2D.OverlapPointAll(player2D.position);

        CameraYTriggerZone activeZone = null;
        int highestPriority = int.MinValue;

        foreach (Collider2D hit in hits)
        {
            CameraYTriggerZone zone = hit.GetComponent<CameraYTriggerZone>();
            if (zone != null && zone.priority >= highestPriority)
            {
                highestPriority = zone.priority;
                activeZone = zone;
            }
        }

        // Jika Player berada di dalam sebuah zona collider, ikuti Y zona tersebut
        if (activeZone != null)
        {
            targetY = activeZone.zoneCameraY;
            currentActiveZoneName = activeZone.gameObject.name;
        }
        else
        {
            // Opsional: Jika keluar dari semua collider, kembali ke defaultY 
            // (Atau hapus baris targetY = defaultY; jika ingin tetap menyimpan Y terakhir)
            targetY = defaultY;
            currentActiveZoneName = "None (Using Default Y)";
        }
    }
}