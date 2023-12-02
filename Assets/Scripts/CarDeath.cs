using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarDeath : MonoBehaviour
{
    public int Health = 3;
    [SerializeField] private GameObject _dieEffect;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.GetComponent<UnbreakableObject>())
        {
            Die();
        }

        if (collision.gameObject.GetComponent<BreakableObject>())
        {
            Health--;

            if(Health <= 0)
            {
               Die();
            }
        }

        if (collision.gameObject.GetComponent<Police>())
        {
            Health--;

            if (Health <= 0)
            {
                Instantiate(_dieEffect, transform.position, Quaternion.identity);
                Die();
            }
        }

        if (collision.gameObject.GetComponent<Car>())
        {
            Die();
        }
    }

    private void Die()
    {
        if (gameObject.GetComponent<Car>())
        {
            Instantiate(_dieEffect, transform.position, Quaternion.identity);
            StartCoroutine(GoBack());
            return;
        }
        Destroy(gameObject);
        Instantiate(_dieEffect, transform.position, Quaternion.identity);
    }

    IEnumerator GoBack()
    {
        Debug.Log("maksim lox");
        GetComponent<Rigidbody>().isKinematic = true;
        Vector3 scale = Car.Instance.transform.localScale;
        Car.Instance.transform.localScale = Vector3.zero;
        yield return new WaitForSeconds(2);
        GetComponent<Rigidbody>().isKinematic = false;
        Car.Instance.transform.localScale = scale;

        Car.Instance._carRotation = 0;
        Car.Instance._visualRotation = 0;

        MenuManager.Instance.TurnOnMenu();
        Health = 3;

        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        transform.position = MenuManager.Instance.transform.position;
        transform.rotation = MenuManager.Instance.transform.rotation;
    }
}
