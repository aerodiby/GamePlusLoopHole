using UnityEngine;

public class GBAPlayer : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public GBAScreenPower screen;

    [Header("Nama state — HARUS persis sama")]
    public string useState   = "OnUse";
    public string unUseState = "UnUse";

    [Header("Nama parameter")]
    public string usingParam = "IsUsing";

    public bool IsUsing { get; private set; }
    public bool IsBusy  { get; private set; }

    bool _waitRaise;
    bool _waitLower;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        animator.SetBool(usingParam, false);
    }

    void Update()
    {
        var s = animator.GetCurrentAnimatorStateInfo(0);

        // ---- tunggu OnUse selesai ----
        if (_waitRaise && s.IsName(useState) && s.normalizedTime >= 1f)
        {
            _waitRaise = false;
            IsBusy     = false;
            if (screen) screen.Play();
            Debug.Log("[GBA] OnUse selesai → layar nyala");
        }

        // ---- tunggu UnUse selesai ----
        if (_waitLower && s.IsName(unUseState) && s.normalizedTime >= 1f)
        {
            _waitLower = false;
            IsBusy     = false;
            if (screen) screen.PowerOffImmediate();
            Debug.Log("[GBA] UnUse selesai → layar mati");
        }
    }

    public void Use()
    {
        if (IsUsing || IsBusy) return;

        IsUsing    = true;
        IsBusy     = true;
        _waitRaise = true;
        animator.SetBool(usingParam, true);
    }

    public void UnUse()
    {
        if (!IsUsing || IsBusy) return;

        IsUsing    = false;
        IsBusy     = true;
        _waitLower = true;
        animator.SetBool(usingParam, false);
    }
}