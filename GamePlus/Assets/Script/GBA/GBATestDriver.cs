using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class GBATestDriver : MonoBehaviour
{
    [Header("Referensi (kosong = auto-find)")]
    public Animator animator;
    public GBAScreenPower screen;
    public GBAHandsController hands;

    [Header("Nama State — HARUS persis sama")]
    public string idleState  = "Idle";
    public string useState   = "OnUse";
    public string unUseState = "UnUse";

    [Header("Nama Parameter Bool")]
    public string usingParam = "IsUsing";

    [Header("Input")]
    public KeyCode useKey   = KeyCode.E;
    public KeyCode unUseKey = KeyCode.Q;
    public KeyCode resetKey = KeyCode.R;

    [Header("Auto Test")]
    public bool  autoTest       = false;
    public float autoUseDelay   = 2f;
    public float autoUnUseDelay = 5f;

    [Header("Debug")]
    public bool showHud = true;

    public bool IsUsing { get; private set; }
    public bool IsBusy  { get; private set; }

    bool   _waitRaise, _waitLower;
    bool   _ready;
    string _lastState = "";
    float  _autoTimer;

    // ============================================================
    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!hands)    hands    = GetComponent<GBAHandsController>();

#if UNITY_2023_1_OR_NEWER
        if (!screen) screen = Object.FindFirstObjectByType<GBAScreenPower>();
#else
        if (!screen) screen = Object.FindObjectOfType<GBAScreenPower>();
#endif

        _ready = Validate();

        if (_ready)
        {
            animator.SetBool(usingParam, false);
            _autoTimer = autoUseDelay;
        }
    }

    bool Validate()
    {
        if (!animator)
        {
            Debug.LogError("[GBA Test] Animator tidak ada di GameObject ini.", this);
            return false;
        }
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("[GBA Test] Animator belum punya Controller.", this);
            return false;
        }

        bool ok = true;

        if (!HasBoolParam(usingParam))
        {
            Debug.LogError($"[GBA Test] Parameter Bool '{usingParam}' tidak ada.", this);
            ok = false;
        }

        ok &= CheckState(idleState);
        ok &= CheckState(useState);
        ok &= CheckState(unUseState);

        if (!screen) Debug.LogWarning("[GBA Test] GBAScreenPower tidak ditemukan. Layar tidak akan nyala.", this);
        if (!hands)  Debug.LogWarning("[GBA Test] GBAHandsController tidak ditemukan. Tangan tidak disembunyikan.", this);

        if (ok) Debug.Log("[GBA Test] ✓ Setup valid. Tekan " + useKey + " untuk Use, " + unUseKey + " untuk UnUse.");
        return ok;
    }

    bool CheckState(string n)
    {
        if (string.IsNullOrEmpty(n)) return true;
        if (animator.HasState(0, Animator.StringToHash(n))) return true;
        Debug.LogError($"[GBA Test] State '{n}' tidak ada di layer 0.", this);
        return false;
    }

    bool HasBoolParam(string n)
    {
        foreach (var p in animator.parameters)
            if (p.name == n && p.type == AnimatorControllerParameterType.Bool) return true;
        return false;
    }

    // ============================================================
    void Update()
    {
        if (!_ready) return;

        var s = animator.GetCurrentAnimatorStateInfo(0);

        // ---- log tiap ganti state ----
        string cur = StateName(s);
        if (cur != _lastState)
        {
            Debug.Log($"[GBA Test] State: {_lastState} → {cur}");
            _lastState = cur;
        }

        // ---- tangan: kelihatan hanya di OnUse / UnUse ----
        if (hands)
        {
            bool inGba = s.IsName(useState) || s.IsName(unUseState);
            hands.SetHandsVisible(inGba);
        }

        // ---- OnUse selesai? ----
        if (_waitRaise && s.IsName(useState) && s.normalizedTime >= 1f)
        {
            _waitRaise = false;
            IsBusy     = false;
            Debug.Log("[GBA Test] ✓ OnUse selesai → layar nyala");
            if (screen) screen.Play();
        }

        // ---- UnUse selesai? ----
        if (_waitLower && s.IsName(unUseState) && s.normalizedTime >= 1f)
        {
            _waitLower = false;
            IsBusy     = false;
            Debug.Log("[GBA Test] ✓ UnUse selesai → layar mati");
            if (screen) screen.PowerOffImmediate();
        }

        // ---- input ----
        if (Input.GetKeyDown(useKey))   Use();
        if (Input.GetKeyDown(unUseKey)) UnUse();
        if (Input.GetKeyDown(resetKey)) ResetAll();

        // ---- auto test ----
        if (autoTest)
        {
            _autoTimer -= Time.deltaTime;
            if (_autoTimer <= 0f)
            {
                if (!IsUsing) { Use();   _autoTimer = autoUnUseDelay; }
                else          { UnUse(); _autoTimer = autoUseDelay;   }
            }
        }
    }

    string StateName(AnimatorStateInfo s)
    {
        if (!string.IsNullOrEmpty(idleState)  && s.IsName(idleState))  return idleState;
        if (!string.IsNullOrEmpty(useState)   && s.IsName(useState))   return useState;
        if (!string.IsNullOrEmpty(unUseState) && s.IsName(unUseState)) return unUseState;
        return "#" + s.shortNameHash;
    }

    // ============================================================
    public void Use()
    {
        if (!_ready || IsUsing || IsBusy) return;

        IsUsing    = true;
        IsBusy     = true;
        _waitRaise = true;
        animator.SetBool(usingParam, true);
        Debug.Log("[GBA Test] USE →");
    }

    public void UnUse()
    {
        if (!_ready || !IsUsing || IsBusy) return;

        IsUsing    = false;
        IsBusy     = true;
        _waitLower = true;
        animator.SetBool(usingParam, false);
        Debug.Log("[GBA Test] UNUSE →");
    }

    public void ResetAll()
    {
        StopAllCoroutines();

        _waitRaise = _waitLower = false;
        IsUsing    = IsBusy = false;
        _autoTimer = autoUseDelay;

        animator.SetBool(usingParam, false);
        animator.Play(idleState, 0, 0f);

        if (screen) screen.PowerOffImmediate();
        if (hands)  hands.SetHandsVisible(false);

        Debug.Log("[GBA Test] ⟲ RESET");
    }

    // ============================================================
    void OnGUI()
    {
        if (!showHud) return;

        var s = animator ? animator.GetCurrentAnimatorStateInfo(0) : default;

        GUILayout.BeginArea(new Rect(12, 12, 380, 210), GUI.skin.box);

        GUILayout.Label("=== GBA TEST ===");
        GUILayout.Label($"Ready     : {_ready}");
        GUILayout.Label($"State     : {(animator ? StateName(s) : "-")}");
        GUILayout.Label($"Time      : {(animator ? s.normalizedTime.ToString("F2") : "-")}");
        GUILayout.Label($"IsUsing   : {IsUsing}    IsBusy: {IsBusy}");
        GUILayout.Label($"Waiting   : raise={_waitRaise}  lower={_waitLower}");
        GUILayout.Label($"Screen    : {(screen ? "OK" : "MISSING")}   Hands: {(hands ? "OK" : "MISSING")}");
        GUILayout.Space(6);
        GUILayout.Label($"[{useKey}] Use    [{unUseKey}] UnUse    [{resetKey}] Reset");
        GUILayout.Label($"AutoTest: {autoTest}  (next in {_autoTimer:F1}s)");

        GUILayout.EndArea();
    }
}