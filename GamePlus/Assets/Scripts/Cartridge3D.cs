using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Cartridge3D : MonoBehaviour, IInteractable
{
    [Header("Visual Settings")]
    public float rotateSpeed = 90f;
    public string promptText = "Tekan E / Klik Kiri untuk ambil Cartridge 3D";

    void Update()
    {
        // Efek berputar agar terlihat seperti item collectible
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }

    public void Interact()
    {
        if (StageObjectiveManager.Instance != null)
        {
            StageObjectiveManager.Instance.CollectCartridge3D();
        }

        // Hilangkan cartridge setelah diambil
        gameObject.SetActive(false);
    }

    public string GetInteractPrompt()
    {
        return promptText;
    }
}