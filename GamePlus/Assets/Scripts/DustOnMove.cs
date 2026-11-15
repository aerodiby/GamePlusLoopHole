using UnityEngine;

[DisallowMultipleComponent]
public class DustOnMove : MonoBehaviour
{
    [Header("Target (kosong = objek ini)")]
    public Transform target;

    [Header("Status Interaksi")]
    [Tooltip("Set true saat item di-grab / dipegang")]
    public bool isInteracting = false;

    [Tooltip("Kalau dicentang: emit tiap kali objek bergerak, tanpa perlu isInteracting")]
    public bool autoInteract = false;

    [Header("Deteksi Gerak")]
    public float minSpeed = 0.12f;
    public float maxSpeed = 4f;
    public float smoothing = 14f;

    [Tooltip("Abaikan lompatan posisi di atas nilai ini (misal saat snap ke tangan)")]
    public float teleportThreshold = 20f;

    [Header("Emisi")]
    [Tooltip("Partikel per meter jarak tempuh")]
    public float particlesPerMeter = 55f;
    public float maxRatePerSecond = 150f;
    public float spawnRadius = 0.3f;
    public float spawnHeightOffset = 0.02f;

    [Header("Arah Semburan")]
    [Tooltip("Dorongan ke belakang (berlawanan arah gerak)")]
    public float backBurst = 1.3f;
    [Tooltip("Dorongan ke atas")]
    public float upBurst = 0.9f;
    [Tooltip("Sebaran acak")]
    public float spread = 0.55f;
    [Tooltip("Ikut dorong ke arah gerak (0 = nggak sama sekali)")]
    public float forwardDrag = 0.2f;

    [Header("Partikel")]
    public Color  startColor = new Color(0.86f, 0.82f, 0.74f, 0.75f);
    public float  sizeMin = 0.35f;
    public float  sizeMax = 0.90f;
    public float  lifeMin = 0.6f;
    public float  lifeMax = 1.5f;

    [Header("Debug")]
    public bool drawGizmo = false;

    ParticleSystem _ps;
    Renderer       _rend;
    Vector3        _lastPos;
    Vector3        _velocity;
    Vector3        _dir = Vector3.back;
    float          _acc;

    // ------------------------------------------------------------
    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        if (!target) target = transform;

        _rend = target.GetComponentInChildren<Renderer>();
        _lastPos = target.position;

        if (_ps)
        {
            var main = _ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake     = false;
            main.loop            = true;

            var em = _ps.emission;
            em.rateOverTime = 0f;      // emisi 100% dari script

            _ps.Play();
        }
        else
        {
            Debug.LogWarning("[DustOnMove] Tidak ada ParticleSystem di GameObject ini.", this);
            enabled = false;
        }
    }

    // ------------------------------------------------------------
    // API buat script interaksi kamu
    public void BeginInteract() => isInteracting = true;
    public void EndInteract()   => isInteracting = false;

    // ------------------------------------------------------------
    void Update()
    {
        if (!target) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 pos     = target.position;
        Vector3 instVel = (pos - _lastPos) / dt;
        _lastPos = pos;

        // abaikan teleport (snap ke tangan, dsb)
        if (instVel.magnitude > teleportThreshold)
        {
            _velocity = Vector3.zero;
            _acc      = 0f;
            return;
        }

        _velocity = Vector3.Lerp(_velocity, instVel, 1f - Mathf.Exp(-smoothing * dt));
        float speed = _velocity.magnitude;

        // arah gerak yang dihaluskan
        if (speed > 0.001f)
        {
            Vector3 d = _velocity / speed;
            _dir = Vector3.Slerp(_dir, d, 1f - Mathf.Exp(-smoothing * dt)).normalized;
        }

        bool moving = speed > minSpeed;
        bool active = moving && (isInteracting || autoInteract);

        if (!active)
        {
            _acc = 0f;
            return;
        }

        // akumulasi berdasarkan JARAK TEMPUH -> kerapatan konsisten
        float clamped = Mathf.Min(speed, maxSpeed);
        _acc += clamped * particlesPerMeter * dt;

        int count = Mathf.FloorToInt(_acc);
        if (count <= 0) return;

        _acc -= count;
        count = Mathf.Min(count, Mathf.CeilToInt(maxRatePerSecond * dt) + 1);

        Emit(count, speed);
    }

    // ------------------------------------------------------------
    void Emit(int count, float speed)
    {
        float t        = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
        float strength = Mathf.Lerp(0.35f, 2.2f, t);

        // arah utama: ke belakang + ke atas
        Vector3 back = (-_dir * backBurst + Vector3.up * upBurst).normalized;

        float baseY = _rend ? _rend.bounds.min.y : target.position.y;

        var ep = new ParticleSystem.EmitParams();
        ep.applyShapeToPosition = false;

        for (int i = 0; i < count; i++)
        {
            Vector2 r = Random.insideUnitCircle * spawnRadius;

            ep.position = new Vector3(
                target.position.x + r.x,
                baseY + spawnHeightOffset,
                target.position.z + r.y);

            ep.velocity = back * strength
                        + _dir * forwardDrag * strength
                        + Random.insideUnitSphere * spread;

            ep.startLifetime = Random.Range(lifeMin, lifeMax);
            ep.startSize     = Random.Range(sizeMin, sizeMax);
            ep.startColor    = startColor;

            _ps.Emit(ep, 1);
        }
    }

    // ------------------------------------------------------------
    void OnDrawGizmosSelected()
    {
        if (!drawGizmo || !Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        Vector3 from = target ? target.position : transform.position;
        Gizmos.DrawRay(from, _dir * 1.5f);

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Gizmos.DrawRay(from, (-_dir * backBurst + Vector3.up * upBurst).normalized * 1.2f);
    }
}