using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Police : MonoBehaviour
{
    [SerializeField] public static float speed = 16;
    [SerializeField] private float rotatingSpeed = 8;

    private GameObject target;
    private Rigidbody policeCar;
    [SerializeField] private Transform _visualTransform;

    private float _visualRotation = 0f;
    [SerializeField] private float _deltaVisualRotation = 45f;

    Vector3 driftAngle = new Vector3(0, 1.6f , 0);
    


    public static int ActiveCount { get; private set; }
    private void OnEnable() { ActiveCount++; }
    private void OnDisable() { ActiveCount = Mathf.Max(0, ActiveCount - 1); }

    private void Awake()
    {   
        policeCar = GetComponent<Rigidbody>();
        if (PlayerCar.Instance != null)
        {
            target = PlayerCar.Instance.gameObject;
            return;
        }
    }

    private void FixedUpdate()
    {
        if (MenuManager.GameActive == true)
        {
            if (target == null)
            {
                if (PlayerCar.Instance == null) return;
                target = PlayerCar.Instance.gameObject;
            }
            if (Mathf.Abs(policeCar.angularVelocity.y) > Mathf.Abs(driftAngle.y))
            {
                if (policeCar.angularVelocity.y > Vector3.zero.y)
                {
                    _visualRotation *= 0.99f;
                    _visualRotation += _deltaVisualRotation * Time.deltaTime;
                }

                if (policeCar.angularVelocity.y < Vector3.zero.y)
                {
                    _visualRotation *= 0.99f;
                    _visualRotation -= _deltaVisualRotation * Time.deltaTime;
                }

                _visualRotation = Mathf.Clamp(_visualRotation, -60f, 60f);
            }

            else
            {
                _visualRotation *= 0.95f;
            }

            _visualTransform.localEulerAngles = new Vector3(0, _visualRotation, 0);

            Vector3 pointTarget = transform.position - target.transform.position;
            pointTarget.Normalize();

            float value = Vector3.Cross(pointTarget, transform.forward).y;

            policeCar.angularVelocity = rotatingSpeed * value * new Vector3(0, 1, 0);
            policeCar.velocity = transform.forward * speed;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
