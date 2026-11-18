using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Setup kamera ala GBA: render ke RenderTexture 240x160, point filter,
/// upscale ke layar dengan letterbox 3:2. Jalan di Built-in maupun URP.
/// </summary>
[RequireComponent(typeof(Camera))]
public class GBACamera : MonoBehaviour
{
    [Header("Resolusi (GBA asli = 240x160)")]
    public int renderWidth  = 240;
    public int renderHeight = 160;

    [Header("Bikin 3D terlihat 2D")]
    [Tooltip("Orthographic = tidak ada perspective, semua sejajar seperti 2D.")]
    public bool forceOrthographic = true;
    public float orthographicSize = 5f;

    [Header("Tampilan")]
    [Tooltip("Aktif = letterbox hitam di pinggir, rasio tetap 3:2.")]
    public bool preserveAspect = true;
    public Color backgroundColor = Color.black;

    Camera _cam;
    RenderTexture _rt;
    GameObject _canvasGO;

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
        _cam.allowHDR  = false;
        _cam.allowMSAA = false;

        if (forceOrthographic)
        {
            _cam.orthographic     = true;
            _cam.orthographicSize = orthographicSize;
        }

        Build();
    }

    void OnDisable()
    {
        if (_cam != null) _cam.targetTexture = null;

        if (_rt != null)
        {
            _rt.Release();
            DestroyImmediate(_rt);
            _rt = null;
        }

        if (_canvasGO != null)
        {
            DestroyImmediate(_canvasGO);
            _canvasGO = null;
        }
    }

    void Build()
    {
        // ---- Render target resolusi rendah ----
        _rt = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32)
        {
            filterMode   = FilterMode.Point,   // WAJIB: bikin piksel kotak
            antiAliasing = 1,
            useMipMap    = false
        };
        _rt.Create();
        _cam.targetTexture = _rt;

        // ---- Canvas untuk menampilkan hasil ----
        _canvasGO = new GameObject("GBA Screen");
        _canvasGO.transform.SetParent(transform, false);

        var canvas = _canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = _canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        // Background hitam (letterbox)
        var bg = new GameObject("BG").AddComponent<Image>();
        bg.transform.SetParent(_canvasGO.transform, false);
        bg.color = backgroundColor;
        bg.raycastTarget = false;
        Stretch(bg.rectTransform);

        // Layar game
        var screen = new GameObject("Screen").AddComponent<RawImage>();
        screen.transform.SetParent(_canvasGO.transform, false);
        screen.texture = _rt;
        screen.raycastTarget = false;
        Stretch(screen.rectTransform);

        if (preserveAspect)
        {
            var fitter = screen.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode  = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)renderWidth / renderHeight;
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void OnValidate()
    {
        renderWidth  = Mathf.Max(16, renderWidth);
        renderHeight = Mathf.Max(16, renderHeight);
    }
}
