using Unity.VisualScripting;
using UnityEngine;

public class PlayerCar : Car
{
    [SerializeField] private Transform _carTransform;
    [SerializeField] private Transform _visualTransform;
    [SerializeField] private Rigidbody _carRigidbody;
    [SerializeField] private CarData _carPrefabs;
    [SerializeField] private Transform _visualParent;
    [SerializeField] private GameObject _otherCarData;
    [SerializeField] private GameObject _limousineData;
    [SerializeField] private ParticleSystem _limousineLeftSmoke;
    [SerializeField] private ParticleSystem _limousineRightSmoke;

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        _leftTrail = _leftSmoke.gameObject.GetComponent<TrailRenderer>();
        _rightTrail = _rightSmoke.gameObject.GetComponent<TrailRenderer>();
    }

    public int CoinsCollected = 0;

    private void Start()
    {
        if (MenuManager.GameActive == true)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        int currentCar = indexOfCurrentCar.GetIndexOfCurrentCar();
        SetupCurrentCar(currentCar);
        _speed = Progress.Instance.GetSpeed(indexOfCurrentCar.GetIndexOfCurrentCar());
        Health = Progress.Instance.GetHealth(indexOfCurrentCar.GetIndexOfCurrentCar());
        maxHealth = Health;
        CoinManager.Instance.ShowHP();
        if (currentCar == 2)
        {
            _otherCarData.SetActive(false);
            _limousineData.SetActive(true);
            _currentLeftSmoke = _limousineLeftSmoke;
            _currentRightSmoke = _limousineRightSmoke;
        }
        else
        {
             _currentLeftSmoke = _leftSmoke;
             _currentRightSmoke = _rightSmoke;
        }
    }

    void Update()
    {
        if (MenuManager.GameActive == true)
        {
            if (Input.GetKey(KeyCode.A))
            {
                TipsHowToControl.Instance.TurnOffTips();
                if (_visualRotation > 0) _visualRotation *= 0.99f;
                _carRotation -= _deltaCarRotation * Time.deltaTime;
                _visualRotation -= _deltaVisualRotation * Time.deltaTime;

                if (_visualRotation < -30)
                {
                    _leftTrail.emitting = true;
                    _rightTrail.emitting = true;
                    _currentLeftSmoke.Play();
                    _currentRightSmoke.Play();
                }
                if (!_drift.isPlaying)
                {
                    _drift.Play();
                }
            }
            else if (Input.GetKey(KeyCode.D))
            {
                TipsHowToControl.Instance.TurnOffTips();
                if (_visualRotation < 0) _visualRotation *= 0.99f;
                _carRotation += _deltaCarRotation * Time.deltaTime;
                _visualRotation += _deltaVisualRotation * Time.deltaTime;
                
                if (_visualRotation > 30)
                {
                    _leftTrail.emitting = true;
                    _rightTrail.emitting = true;
                    _currentLeftSmoke.Play();
                    _currentRightSmoke.Play();
                }
                if (!_drift.isPlaying)
                {
                    _drift.Play();
                }
            }
            else
            {
                _visualRotation *= 0.95f;
                _currentLeftSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                _currentRightSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                _leftTrail.emitting = false;
                _rightTrail.emitting = false;
                _drift.Stop();
            }
            _visualRotation = Mathf.Clamp(_visualRotation, -60f, 60f);

            Vector3 speed;

            speed = transform.forward * _speed;
            if (!_carRigidbody.isKinematic) _carRigidbody.velocity = speed;
            _carTransform.localEulerAngles = new Vector3(0, _carRotation, 0);
            _visualTransform.localEulerAngles = new Vector3(0, _visualRotation, 0);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Coin>() is Coin coin)
        {
            coin.Collect();
            CoinsCollected += 1;
        }
        if (other.gameObject.GetComponent<Health>() is Health health)
        {
            health.Collect();
        }
    }
    private void SetupCurrentCar(int Index)
    {
        GameObject currentCar = _carPrefabs.GetCarData((CarType)Index).CarPrefab;
        Instantiate(currentCar, _visualParent);
    }

}
