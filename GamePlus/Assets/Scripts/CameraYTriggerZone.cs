using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraYTriggerZone : MonoBehaviour
{
    [Header("Camera Y Target")]
    [Tooltip("Titik koordinat Y kamera selama Player berada di dalam collider ini")]
    public float zoneCameraY = 5f;

    [Tooltip("Jika ada 2 collider bertumpukan, nilai priority lebih tinggi yang akan dipilih")]
    public int priority = 0;

    [Header("Gizmo Color (Hanya terlihat di Scene View)")]
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.25f);

    void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;
            Vector3 center = transform.position + (Vector3)col.offset;
            Vector3 size = new Vector3(col.size.x * transform.lossyScale.x, col.size.y * transform.lossyScale.y, 0.1f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}