using UnityEngine;

/// <summary>
/// Bikin objek 3D selalu menghadap kamera => terbaca seperti sprite 2D.
/// Taruh di karakter / pohon / item yang mau tampil "flat".
/// </summary>
public class Billboard2D : MonoBehaviour
{
    public enum Mode
    {
        YAxisOnly,   // hanya berputar horizontal (paling umum untuk karakter)
        Full         // benar-benar menghadap kamera
    }

    public Mode mode = Mode.YAxisOnly;

    [Tooltip("Kalau modelmu menghadap +Z (bukan -Z), aktifkan ini.")]
    public bool flip180 = false;

    Camera _cam;

    void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector3 f = _cam.transform.forward;
        if (mode == Mode.YAxisOnly) f.y = 0f;

        if (f.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
        if (flip180) transform.Rotate(0f, 180f, 0f, Space.World);
    }
}
