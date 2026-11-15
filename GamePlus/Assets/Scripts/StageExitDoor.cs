using UnityEngine;
using UnityEngine.SceneManagement;

public class StageExitDoor : MonoBehaviour
{
    [Header("Stage Settings")]
    [Tooltip("Nama scene berikutnya di Build Settings. Jika kosong, akan reload scene saat ini.")]
    public string nextSceneName = "";

    [Header("Visual Feedback")]
    public Renderer doorRenderer;
    public Color lockedColor = Color.red;
    public Color unlockedColor = Color.green;

    [Tooltip("Opsional: Objek penghalang pintu yang akan dimatikan saat terbuka")]
    public GameObject doorBlocker;

    private bool isOpen = false;

    void Start()
    {
        UpdateDoorVisual();
    }

    // Dipanggil melalui UnityEvent dari StageObjectiveManager
    public void UnlockDoor()
    {
        isOpen = true;
        UpdateDoorVisual();

        if (doorBlocker != null)
        {
            doorBlocker.SetActive(false);
        }
    }

    void UpdateDoorVisual()
    {
        if (doorRenderer != null && doorRenderer.material != null)
        {
            doorRenderer.material.color = isOpen ? unlockedColor : lockedColor;
        }
        else if (doorRenderer is SpriteRenderer sr)
        {
            sr.color = isOpen ? unlockedColor : lockedColor;
        }
    }

    // Jika Player masuk pintu saat di Mode 2D
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen) return;

        if (other.GetComponent<Player2DController>() != null)
        {
            LoadNextStage();
        }
    }

    // Jika Player masuk pintu saat di Mode 3D
    void OnTriggerEnter(Collider other)
    {
        if (!isOpen) return;

        if (other.GetComponent<Player3DController>() != null)
        {
            LoadNextStage();
        }
    }

    void LoadNextStage()
    {
        Debug.Log("Memasuki Next Stage!");
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Untuk testing prototype: reload scene saat ini jika nextSceneName belum diisi
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}