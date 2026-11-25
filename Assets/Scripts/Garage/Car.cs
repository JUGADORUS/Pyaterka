using UnityEngine;

public enum CarType
{
    Hypercar1 = 0,
    NissanGTR = 1,
    Hypercar2 = 2,
    Hypercar3 = 3,
    Gelik = 4,
    Cybertruck = 5
}

public class Car : MonoBehaviour
{
    [SerializeField] protected GiveCurrentCar indexOfCurrentCar;
    public CarType CarType;
}
