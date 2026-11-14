using UnityEngine;

public class PerspectiveBoxProjector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kamera 2D (Wajib mode Perspective)")]
    public Camera cam2D;
    
    [Tooltip("GameObject dengan BoxCollider2D yang berada di bidang 2D")]
    public Transform proxyCollider2D;

    [Header("Plane Settings")]
    [Tooltip("Koordinat Z tempat karakter 2D berjalan (default: 0)")]
    public float targetPlaneZ = 0f;
    
    [Tooltip("Batas jarak minimum dari kamera 2D agar skala tidak meledak")]
    public float minDistanceFromCam = 2f;

    private Vector3 initialScale3D;

    void Start()
    {
        initialScale3D = transform.localScale;
        
        // Pastikan proxy tidak menjadi child langsung agar transform-nya independen
        if (proxyCollider2D.parent == transform)
        {
            proxyCollider2D.SetParent(null);
        }
    }

    void LateUpdate()
    {
        ProjectTo2DPlane();
    }

    void ProjectTo2DPlane()
    {
        if (cam2D == null || proxyCollider2D == null) return;

        Vector3 camPos = cam2D.transform.position;
        Vector3 boxPos = transform.position;

        // 1. Hitung jarak Z dari Kamera 2D ke Bidang 2D dan ke Box 3D
        float distCamToPlane = Mathf.Abs(targetPlaneZ - camPos.z);
        float distCamToBox = boxPos.z - camPos.z; // Asumsi kamera menghadap +Z

        // Cegah pembagian dengan nol atau posisi di belakang kamera
        if (distCamToBox < minDistanceFromCam)
        {
            proxyCollider2D.gameObject.SetActive(false);
            return;
        }
        else if (!proxyCollider2D.gameObject.activeSelf)
        {
            proxyCollider2D.gameObject.SetActive(true);
        }

        // 2. Rumus Rasio Segitiga Sebangun (Perspective Scale Factor)
        float scaleRatio = distCamToPlane / distCamToBox;

        // 3. Hitung Posisi X dan Y hasil proyeksi pada bidang Z = targetPlaneZ
        // Karena efek perspektif, posisi X dan Y di layar juga bergeser menjauhi/mendekati titik tengah kamera
        float projectedX = camPos.x + (boxPos.x - camPos.x) * scaleRatio;
        float projectedY = camPos.y + (boxPos.y - camPos.y) * scaleRatio;

        proxyCollider2D.position = new Vector3(projectedX, projectedY, targetPlaneZ);

        // 4. Terapkan Skala pada Proxy Collider 2D
        proxyCollider2D.localScale = new Vector3(
            initialScale3D.x * scaleRatio,
            initialScale3D.y * scaleRatio,
            1f
        );

        // Opsional: Sinkronkan rotasi sumbu Z jika box bisa berputar
        proxyCollider2D.rotation = Quaternion.Euler(0f, 0f, transform.eulerAngles.z);
    }
}