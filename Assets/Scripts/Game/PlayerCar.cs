using UnityEngine;

public class PlayerCar : Car
{
    [SerializeField] private Transform _carTransform;
    [SerializeField] private Transform _visualTransform;
    [SerializeField] private Rigidbody _carRigidbody;
    [SerializeField] private CarData _carPrefabs;
    [SerializeField] private Transform _visualParent;
    [SerializeField] private GameObject _otherCarData;

    [SerializeField] public float _speed = 0;

    [SerializeField] private float _deltaCarRotation = 115f;
    [SerializeField] private float _deltaVisualRotation = 35f;

    [SerializeField] private ParticleSystem _leftSmoke;
    [SerializeField] private ParticleSystem _rightSmoke;
    private TrailRenderer _leftTrail;
    private TrailRenderer _rightTrail;
    private ParticleSystem _currentLeftSmoke ;
    private ParticleSystem _currentRightSmoke;

[SerializeField] private AudioSource _drift;

    public float _carRotation = 0f;
    public float _visualRotation = 0f;
    public int Health = 1;
    public int maxHealth;

    public static PlayerCar Instance;

    public int CurrentCarIndex { get; private set; }
    public bool HasCrashShield { get; private set; }
    public int CoinsCollected = 0;
    private float steering;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Progress.EnsureLoaded();
    }

    private void Start()
    {
        int currentCar = indexOfCurrentCar.GetIndexOfCurrentCar();
        if (!Progress.Instance.IsCarUnlocked(currentCar)) currentCar = 0;
        CurrentCarIndex = currentCar;
        _deltaCarRotation = GameBalance.TurnSpeed[currentCar];
        GameObject model = _carPrefabs.GetCarData((CarType)currentCar).CarPrefab;
        Instantiate(model, _visualParent);
        _speed = Progress.Instance.GetSpeed(currentCar);
        maxHealth = Progress.Instance.GetHealth(currentCar);
        _otherCarData.SetActive(true);
        _currentLeftSmoke = _leftSmoke;
        _currentRightSmoke = _rightSmoke;
        _leftTrail = _currentLeftSmoke.GetComponent<TrailRenderer>();
        _rightTrail = _currentRightSmoke.GetComponent<TrailRenderer>();
        ResetRun();
    }

    private void Update()
    {
        if (!MenuManager.GameActive || _carRigidbody.isKinematic)
        {
            steering = 0f;
            SetDrift(false);
            return;
        }
        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ||
            (LeftButtonPressed.Instance != null && LeftButtonPressed.Instance.buttonPressed);
        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ||
            (RightButtonPressed.Instance != null && RightButtonPressed.Instance.buttonPressed);
        steering = left == right ? 0f : left ? -1f : 1f;
        if (steering != 0f)
        {
            if (TipsHowToControl.Instance != null) TipsHowToControl.Instance.TurnOffTips();
            if (_visualRotation * steering < 0f)
                _visualRotation *= Mathf.Pow(0.99f, Time.deltaTime * 60f);
            _visualRotation += steering * _deltaVisualRotation * Time.deltaTime;
        }
        else
        {
            _visualRotation *= Mathf.Pow(0.95f, Time.deltaTime * 60f);
        }
        _visualRotation = Mathf.Clamp(_visualRotation, -60f, 60f);
        _visualTransform.localEulerAngles = new Vector3(0f, _visualRotation, 0f);
        SetDrift(steering != 0f && Mathf.Abs(_visualRotation) > 30f);
    }

    private void FixedUpdate()
    {
        if (!MenuManager.GameActive || _carRigidbody.isKinematic) return;
        _carRotation += steering * _deltaCarRotation * Time.fixedDeltaTime;
        Quaternion rotation = Quaternion.Euler(0f, _carRotation, 0f);
        _carRigidbody.MoveRotation(rotation);
        _carRigidbody.velocity = rotation * Vector3.forward * _speed;
    }

    public void ResetRun()
    {
        StopMotion();
        Health = maxHealth;
        CoinsCollected = 0;
        HasCrashShield = CurrentCarIndex == GameBalance.PremiumCarIndex;
        _carRotation = _carTransform.eulerAngles.y;
        _visualRotation = 0f;
        _visualTransform.localRotation = Quaternion.identity;
        if (_leftTrail != null) _leftTrail.Clear();
        if (_rightTrail != null) _rightTrail.Clear();
        if (CoinManager.Instance != null) CoinManager.Instance.ShowHP();
    }

    public bool UseCrashShield()
    {
        if (!HasCrashShield) return false;
        HasCrashShield = false;
        Health = Mathf.Max(1, Health);
        _carRotation = (_carRotation + 180f) % 360f;
        _carRigidbody.rotation = Quaternion.Euler(0f, _carRotation, 0f);
        return true;
    }

    public void StopMotion()
    {
        steering = 0f;
        if (!_carRigidbody.isKinematic)
        {
            _carRigidbody.velocity = Vector3.zero;
            _carRigidbody.angularVelocity = Vector3.zero;
        }
        SetDrift(false);
        if (LeftButtonPressed.Instance != null) LeftButtonPressed.Instance.buttonPressed = false;
        if (RightButtonPressed.Instance != null) RightButtonPressed.Instance.buttonPressed = false;
    }

    private void SetDrift(bool drifting)
    {
        if (_leftTrail != null) _leftTrail.emitting = drifting;
        if (_rightTrail != null) _rightTrail.emitting = drifting;
        SetSmoke(_currentLeftSmoke, drifting);
        SetSmoke(_currentRightSmoke, drifting);
        if (_drift == null) return;
        if (drifting && !_drift.isPlaying) _drift.Play();
        else if (!drifting && _drift.isPlaying) _drift.Stop();
    }

    private static void SetSmoke(ParticleSystem smoke, bool playing)
    {
        if (smoke == null) return;
        if (playing && !smoke.isPlaying) smoke.Play();
        else if (!playing && smoke.isPlaying) smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!MenuManager.GameActive) return;
        Coin coin = other.GetComponentInParent<Coin>();
        if (coin != null && coin.Collect()) CoinsCollected++;
        Health health = other.GetComponentInParent<Health>();
        if (health != null) health.Collect();
    }
}
