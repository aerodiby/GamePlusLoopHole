using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class WinZone2D : MonoBehaviour
{
    [Header("Win UI & Scene Settings")]
    [Tooltip("Tarik Panel / GameObject UI Win dari Canvas ke sini")]
    public GameObject winUIPanel;

    [Tooltip("Nama Scene Main Menu di Build Settings")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Waktu tunggu sebelum kembali ke Main Menu (detik)")]
    public float delayBeforeMainMenu = 5f;

    [Header("Optional: Cartridge Requirement")]
    [Tooltip("Centang jika player wajib mengumpulkan 2 Cartridge (3D & 2D) dulu sebelum bisa menang")]
    public bool requireCartridges = false;

    [Tooltip("Opsional: Tarik GameManager jika ingin mematikan fitur tekan Tab saat sudah menang")]
    public DimensionSwitchManager switchManager;

    private bool hasWon = false;

    void Awake()
    {
        // Pastikan collider otomatis menjadi Trigger
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Pastikan UI Win disembunyikan saat game baru mulai
        if (winUIPanel != null)
        {
            winUIPanel.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasWon) return;

        // Cek apakah yang menyentuh adalah Player_2D
        Player2DController player2D = other.GetComponent<Player2DController>();
        if (player2D != null)
        {
            // Jika menggunakan syarat 2 Cartridge dari StageObjectiveManager, cek apakah sudah terkumpul
            if (requireCartridges && StageObjectiveManager.Instance != null)
            {
                if (!StageObjectiveManager.Instance.isDoorUnlocked)
                {
                    Debug.Log("[WinZone] Kumpulkan Cartridge 3D & 2D terlebih dahulu!");
                    return;
                }
            }

            StartCoroutine(TriggerWinSequence(player2D));
        }
    }

    IEnumerator TriggerWinSequence(Player2DController player2D)
    {
        hasWon = true;

        // 1. Hentikan pergerakan Player_2D & matikan kontrolnya
        player2D.enabled = false;
        Rigidbody2D rb = player2D.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // 2. Matikan DimensionSwitchManager agar player tidak bisa tekan Tab saat layar Win muncul
        if (switchManager == null)
        {
            switchManager = FindFirstObjectByType<DimensionSwitchManager>();
        }
        if (switchManager != null)
        {
            switchManager.enabled = false;
        }

        // 3. Bebaskan kursor mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. Munculkan UI Win
        if (winUIPanel != null)
        {
            winUIPanel.SetActive(true);
        }

        Debug.Log($"[WinZone] YOU WIN! Kembali ke {mainMenuSceneName} dalam {delayBeforeMainMenu} detik...");

        // 5. Tunggu 5 detik (menggunakan WaitForSecondsRealtime agar tetap jalan walau Time.timeScale = 0)
        yield return new WaitForSecondsRealtime(delayBeforeMainMenu);

        // 6. Pindah ke Scene Main Menu
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
