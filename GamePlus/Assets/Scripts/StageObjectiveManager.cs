using UnityEngine;
using UnityEngine.Events;

public class StageObjectiveManager : MonoBehaviour
{
    public static StageObjectiveManager Instance { get; private set; }

    [Header("Cartridge Status")]
    public bool has3DCartridge = false;
    public bool has2DCartridge = false;
    public bool isDoorUnlocked = false;

    [Header("Events")]
    [Tooltip("Dipicu otomatis saat kedua cartridge (3D & 2D) sudah terkumpul")]
    public UnityEvent onAllCartridgesCollected;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void CollectCartridge3D()
    {
        if (has3DCartridge) return;

        has3DCartridge = true;
        Debug.Log("[Objective] Cartridge 3D berhasil diambil!");
        CheckCompletion();
    }

    public void CollectCartridge2D()
    {
        if (has2DCartridge) return;

        has2DCartridge = true;
        Debug.Log("[Objective] Cartridge 2D berhasil diambil!");
        CheckCompletion();
    }

    void CheckCompletion()
    {
        if (has3DCartridge && has2DCartridge && !isDoorUnlocked)
        {
            isDoorUnlocked = true;
            Debug.Log("[Objective] Semua Cartridge terkumpul! Pintu Next Stage terbuka!");
            onAllCartridgesCollected?.Invoke();
        }
    }
}