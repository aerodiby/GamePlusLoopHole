using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PerspectiveBoxProjector : MonoBehaviour
{
    [Header("References (Otomatis Dicari Jika Kosong)")]
    [Tooltip("Biarkan kosong untuk dicari otomatis saat Start/Awake")]
    public Camera cam2D;

    [Tooltip("GameObject dengan BoxCollider2D yang berada di bidang 2D (Otomatis dibuat jika kosong)")]
    public Transform proxyCollider2D;

    [Header("Auto-Find Camera Settings")]
    [Tooltip("Nama GameObject kamera 2D di Hierarchy")]
    public string cam2DObjectName = "Cam_2D";

    [Tooltip("Opsional: Tag kamera 2D (jika ingin mencari lewat Tag, pastikan Tag sudah dibuat di Unity)")]
    public string cam2DTag = "Cam2D";

    [Tooltip("Opsional: Layer untuk Proxy_2D jika dibuat otomatis")]
    public string proxyLayerName = "Default";

    [Header("Projection Settings")]
    [Tooltip("Koordinat Z tempat Player_2D berjalan (default: 0)")]
    public float targetPlaneZ = 0f;

    [Tooltip("Jarak aman minimum dari lensa Cam_2D")]
    public float minDistanceFromCam = 0.5f;

    [Tooltip("Jika hanya ingin pas dengan permukaan DEPAN kotak (abaikan ketebalan samping/atas), centang ini.")]
    public bool frontFaceOnly = false;

    private Renderer boxRenderer;
    private BoxCollider2D proxyBoxCol;
    private Vector2 initialColliderSize = Vector2.one;

    void Awake()
    {
        boxRenderer = GetComponent<Renderer>();

        // 1. Cari Cam_2D secara otomatis jika belum diisi di Inspector
        if (cam2D == null)
        {
            AutoFindCamera2D();
        }

        // 2. Buat Proxy_2D secara otomatis jika belum diisi di Inspector
        if (proxyCollider2D == null)
        {
            AutoCreateProxy2D();
        }

        // 3. Setup Proxy_2D
        if (proxyCollider2D != null)
        {
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

    void AutoFindCamera2D()
    {
        // Cara 1: Cari cepat lewat GameObject.Find (jika objek aktif)
        if (!string.IsNullOrEmpty(cam2DObjectName))
        {
            GameObject foundByName = GameObject.Find(cam2DObjectName);
            if (foundByName != null && foundByName.TryGetComponent(out Camera c))
            {
                cam2D = c;
                return;
            }
        }

        // Cara 2: Cari dari seluruh komponen Camera di Scene (termasuk yang sedang inactive/disabled!)
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera c in allCameras)
        {
            // Cek kecocokan Nama
            if (!string.IsNullOrEmpty(cam2DObjectName) && c.gameObject.name == cam2DObjectName)
            {
                cam2D = c;
                return;
            }

            // Cek kecocokan Tag (gunakan CompareTag dengan aman)
            if (!string.IsNullOrEmpty(cam2DTag) && c.gameObject.tag == cam2DTag)
            {
                cam2D = c;
                return;
            }
        }

        Debug.LogWarning($"[PerspectiveBoxProjector] Tidak dapat menemukan kamera '{cam2DObjectName}' di Scene pada objek {gameObject.name}!");
    }

    void AutoCreateProxy2D()
    {
        // Jika lupa membuat Proxy_2D, script otomatis membuatkannya di Hierarchy!
        GameObject autoProxy = new GameObject($"Proxy_2D_{gameObject.name}");
        proxyBoxCol = autoProxy.AddComponent<BoxCollider2D>();
        proxyBoxCol.size = Vector2.one;

        int layerIndex = LayerMask.NameToLayer(proxyLayerName);
        if (layerIndex != -1)
        {
            autoProxy.layer = layerIndex;
        }

        proxyCollider2D = autoProxy.transform;
    }

    void LateUpdate()
    {
        ProjectVisualBoundsTo2DPlane();
    }

    void ProjectVisualBoundsTo2DPlane()
    {
        if (cam2D == null || proxyCollider2D == null || boxRenderer == null) return;

        Bounds b = boxRenderer.bounds;

        float distCamToPlane = Mathf.Abs(targetPlaneZ - cam2D.transform.position.z);

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

        Vector3[] corners = new Vector3[8];
        float minZ = b.min.z;
        float maxZ = frontFaceOnly ? b.min.z : b.max.z;

        corners[0] = new Vector3(b.min.x, b.min.y, minZ);
        corners[1] = new Vector3(b.min.x, b.max.y, minZ);
        corners[2] = new Vector3(b.max.x, b.min.y, minZ);
        corners[3] = new Vector3(b.max.x, b.max.y, minZ);

        corners[4] = new Vector3(b.min.x, b.min.y, maxZ);
        corners[5] = new Vector3(b.min.x, b.max.y, maxZ);
        corners[6] = new Vector3(b.max.x, b.min.y, maxZ);
        corners[7] = new Vector3(b.max.x, b.max.y, maxZ);

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

        Vector3 worldBottomLeft = cam2D.ViewportToWorldPoint(new Vector3(minViewportX, minViewportY, distCamToPlane));
        Vector3 worldTopRight = cam2D.ViewportToWorldPoint(new Vector3(maxViewportX, maxViewportY, distCamToPlane));

        Vector3 projectedCenter = new Vector3(
            (worldBottomLeft.x + worldTopRight.x) * 0.5f,
            (worldBottomLeft.y + worldTopRight.y) * 0.5f,
            targetPlaneZ
        );

        Vector2 projectedSize = new Vector2(
            Mathf.Abs(worldTopRight.x - worldBottomLeft.x),
            Mathf.Abs(worldTopRight.y - worldBottomLeft.y)
        );

        proxyCollider2D.position = projectedCenter;
        proxyCollider2D.rotation = Quaternion.identity;

        proxyCollider2D.localScale = new Vector3(
            projectedSize.x / initialColliderSize.x,
            projectedSize.y / initialColliderSize.y,
            1f
        );
    }

    void OnDestroy()
    {
        // Bersihkan Proxy_2D otomatis jika Box_3D dihancurkan
        if (proxyCollider2D != null)
        {
            Destroy(proxyCollider2D.gameObject);
        }
    }
}