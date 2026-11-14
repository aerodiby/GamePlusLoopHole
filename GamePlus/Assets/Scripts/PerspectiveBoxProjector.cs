using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PerspectiveBoxProjector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kamera 2D (Wajib mode Perspective)")]
    public Camera cam2D;

    [Tooltip("GameObject dengan BoxCollider2D yang berada di bidang 2D")]
    public Transform proxyCollider2D;

    [Header("Projection Settings")]
    [Tooltip("Koordinat Z tempat Player_2D berjalan (default: 0)")]
    public float targetPlaneZ = 0f;

    [Tooltip("Jarak aman minimum dari lensa Cam_2D")]
    public float minDistanceFromCam = 0.5f;

    [Tooltip("Jika hanya ingin pas dengan permukaan DEPAN kotak (abaikan ketebalan samping/atas), centang ini. Jika ingin pas dengan SELURUH siluet visual 3D di layar, biarkan tidak dicentang.")]
    public bool frontFaceOnly = false;

    private Renderer boxRenderer;
    private BoxCollider2D proxyBoxCol;
    private Vector2 initialColliderSize = Vector2.one;

    void Awake()
    {
        boxRenderer = GetComponent<Renderer>();

        if (proxyCollider2D != null)
        {
            // Lepas dari parent jika tidak sengaja dijadikan child dari Box_3D
            if (proxyCollider2D.parent != null)
            {
                proxyCollider2D.SetParent(null);
            }

            proxyBoxCol = proxyCollider2D.GetComponent<BoxCollider2D>();
            if (proxyBoxCol != null)
            {
                initialColliderSize = proxyBoxCol.size;
            }
        }
    }

    void LateUpdate()
    {
        ProjectVisualBoundsTo2DPlane();
    }

    void ProjectVisualBoundsTo2DPlane()
    {
        if (cam2D == null || proxyCollider2D == null || boxRenderer == null) return;

        Bounds b = boxRenderer.bounds;

        // Jarak Z dari kamera ke bidang permainan 2D (Z = 0)
        float distCamToPlane = Mathf.Abs(targetPlaneZ - cam2D.transform.position.z);

        // Cek apakah sisi terdekat kotak menabrak lensa kamera
        float closestZToCam = b.min.z - cam2D.transform.position.z;
        if (closestZToCam < minDistanceFromCam)
        {
            proxyCollider2D.gameObject.SetActive(false);
            return;
        }
        else if (!proxyCollider2D.gameObject.activeSelf)
        {
            proxyCollider2D.gameObject.SetActive(true);
        }

        // Ambil 8 titik sudut dari Bounding Box 3D
        Vector3[] corners = new Vector3[8];
        float minZ = b.min.z;
        float maxZ = frontFaceOnly ? b.min.z : b.max.z; // Jika frontFaceOnly, ambil permukaan paling dekat ke kamera saja

        corners[0] = new Vector3(b.min.x, b.min.y, minZ);
        corners[1] = new Vector3(b.min.x, b.max.y, minZ);
        corners[2] = new Vector3(b.max.x, b.min.y, minZ);
        corners[3] = new Vector3(b.max.x, b.max.y, minZ);

        corners[4] = new Vector3(b.min.x, b.min.y, maxZ);
        corners[5] = new Vector3(b.min.x, b.max.y, maxZ);
        corners[6] = new Vector3(b.max.x, b.min.y, maxZ);
        corners[7] = new Vector3(b.max.x, b.max.y, maxZ);

        // Cari batas paling kiri, kanan, bawah, dan atas di layar Cam_2D (Viewport Space)
        float minViewportX = float.MaxValue;
        float maxViewportX = float.MinValue;
        float minViewportY = float.MaxValue;
        float maxViewportY = float.MinValue;

        for (int i = 0; i < 8; i++)
        {
            Vector3 vp = cam2D.WorldToViewportPoint(corners[i]);
            if (vp.x < minViewportX) minViewportX = vp.x;
            if (vp.x > maxViewportX) maxViewportX = vp.x;
            if (vp.y < minViewportY) minViewportY = vp.y;
            if (vp.y > maxViewportY) maxViewportY = vp.y;
        }

        // Kembalikan batas layar tersebut ke koordinat World pada bidang Z = targetPlaneZ
        Vector3 worldBottomLeft = cam2D.ViewportToWorldPoint(new Vector3(minViewportX, minViewportY, distCamToPlane));
        Vector3 worldTopRight = cam2D.ViewportToWorldPoint(new Vector3(maxViewportX, maxViewportY, distCamToPlane));

        // Hitung titik tengah dan ukuran tepat di bidang Z = 0
        Vector3 projectedCenter = new Vector3(
            (worldBottomLeft.x + worldTopRight.x) * 0.5f,
            (worldBottomLeft.y + worldTopRight.y) * 0.5f,
            targetPlaneZ
        );

        Vector2 projectedSize = new Vector2(
            Mathf.Abs(worldTopRight.x - worldBottomLeft.x),
            Mathf.Abs(worldTopRight.y - worldBottomLeft.y)
        );

        // Terapkan Posisi dan Scale ke Proxy_2D
        proxyCollider2D.position = projectedCenter;
        proxyCollider2D.rotation = Quaternion.identity;

        // Pastikan scale dibagi dengan size BoxCollider2D bawaan agar ukuran akhirnya 100% pas
        proxyCollider2D.localScale = new Vector3(
            projectedSize.x / initialColliderSize.x,
            projectedSize.y / initialColliderSize.y,
            1f
        );
    }
}