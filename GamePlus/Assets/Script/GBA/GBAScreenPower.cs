using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class GBAScreenPower : MonoBehaviour
{
    [Header("Target Material")]
    [Tooltip("Kosongkan = pakai material dari Renderer (dibuat instance otomatis)")]
    public Material material;

    [Header("Nama Properti Shader")]
    public string powerProp  = "_Power";
    public string rollProp   = "_Roll";
    public string glitchProp = "_Glitch";
    public string seedProp   = "_Seed";
    public string offProp    = "_Off";

    [Header("Menunggu sebelum animasi")]
    [Tooltip("Jeda dalam detik sebelum animasi power mulai")]
    public float waitBeforeStart = 1.5f;

    [Header("Animasi Power")]
    public float powerDuration = 0.9f;
    public AnimationCurve powerCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool    playOnStart  = true;
    public bool    startPowered = false;
    public KeyCode replayKey    = KeyCode.None;

    [Header("V-Roll (terus bergerak)")]
    public bool  rollEnabled = true;
    [Tooltip("Siklus per detik. Negatif = arah berlawanan")]
    public float rollSpeed   = 0.35f;
    [Tooltip("Pengali kecepatan selama animasi power berjalan")]
    public float rollSpeedDuringPower = 3f;
    public bool  rollOnlyAfterPower = false;
    [Tooltip("HARUS sama dengan pengali _Roll di shader (0.35)")]
    public float shaderRollScale = 0.35f;

    [Header("Glitch saat power")]
    public float glitchAmount = 1f;
    public float seedFps      = 20f;

    public event Action OnPowerComplete;

    Renderer  _rend;
    Material  _mat;
    bool      _owned;
    bool      _playing;
    bool      _powered;
    float     _phase;          // 0..1 fase roll
    Coroutine _routine;

    // ------------------------------------------------------------
    void Awake()
    {
        _rend = GetComponent<Renderer>();

        if (material != null)
        {
            _mat = material;
        }
        else
        {
            _mat   = _rend.material;      // instance otomatis
            _owned = true;
        }

        if (_mat == null || !_mat.HasProperty(powerProp))
        {
            Debug.LogError("[GBAScreenPower] Material tidak valid / bukan shader GBA.", this);
            enabled = false;
            return;
        }

        _powered = startPowered;
        SetPower(startPowered ? 1f : 0f);
        SetGlitch(0f);
    }

    void Start()
    {
        if (playOnStart) Play();
    }

    void Update()
    {
        // ---- V-ROLL: terus bergerak, nggak pernah berhenti ----
        if (rollEnabled && (!rollOnlyAfterPower || _powered))
        {
            float speed = rollSpeed * (_playing ? rollSpeedDuringPower : 1f);

            _phase = Mathf.Repeat(_phase + speed * Time.deltaTime, 1f);

            // shader: uv.y = frac(uv.y + _Roll * 0.35)
            // jadi _Roll harus 0 .. 1/0.35 supaya wrap-nya mulus tanpa lompat
            _mat.SetFloat(rollProp, _phase / Mathf.Max(shaderRollScale, 1e-4f));
        }

        if (replayKey != KeyCode.None && Input.GetKeyDown(replayKey))
            Play();
    }

    // ------------------------------------------------------------
    public void Play()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PowerRoutine());
    }

    public void Stop()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        _playing = false;
    }

    IEnumerator PowerRoutine()
    {
        _playing = true;
        _powered = false;
        SetPower(0f);
        SetGlitch(0f);

        // ---- MENUNGGU SEBELUM ANIMASI ----
        float w = 0f;
        while (w < waitBeforeStart)
        {
            w += Time.deltaTime;
            yield return null;
        }

        // ---- ANIMASI POWER ----
        float t = 0f;
        while (t < powerDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / powerDuration);

            SetPower(powerCurve.Evaluate(k));

            // glitch naik lalu reda (kurva sinus: 0 → 1 → 0)
            SetGlitch(Mathf.Sin(k * Mathf.PI) * glitchAmount);
            SetSeed(Mathf.Floor(Time.time * seedFps));

            yield return null;
        }

        SetPower(1f);
        SetGlitch(0f);

        _playing = false;
        _powered = true;
        _routine = null;
        OnPowerComplete?.Invoke();
    }

    // ------------------------------------------------------------
    void SetPower(float v)  => _mat.SetFloat(powerProp, Mathf.Clamp01(v));

    void SetGlitch(float v)
    {
        if (_mat.HasProperty(glitchProp)) _mat.SetFloat(glitchProp, v);
    }

    void SetSeed(float v)
    {
        if (_mat.HasProperty(seedProp)) _mat.SetFloat(seedProp, v);
    }

    public void PowerOffImmediate()
    {
        Stop();
        _powered = false;
        SetPower(0f);
        SetGlitch(0f);
        if (_mat.HasProperty(offProp)) _mat.SetFloat(offProp, 1f);
    }

    void OnDestroy()
    {
        if (_owned && _mat != null) Destroy(_mat);
    }
}