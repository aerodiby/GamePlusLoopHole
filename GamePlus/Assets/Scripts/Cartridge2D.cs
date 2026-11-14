using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Cartridge2D : MonoBehaviour
{
    [Header("Floating Animation")]
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 3f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        
        // Pastikan collider berstatus Trigger agar tidak menghalangi langkah Player_2D
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        // Efek melayang naik-turun di bidang 2D
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Cek apakah yang menyentuh adalah Player_2D
        if (other.GetComponent<Player2DController>() != null)
        {
            if (StageObjectiveManager.Instance != null)
            {
                StageObjectiveManager.Instance.CollectCartridge2D();
            }

            // Hilangkan cartridge 2D setelah tersentuh
            gameObject.SetActive(false);
        }
    }
}