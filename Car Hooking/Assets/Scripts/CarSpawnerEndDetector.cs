using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawnerEndDetector : MonoBehaviour
{
    Road road;
    BoxCollider myBoxCollider;

    float colliderMovingSpeed;

    bool isChecked;

    void Awake()
    {
        road = transform.root.GetComponent<Road>();
        myBoxCollider = GetComponent<BoxCollider>();
    }

    void Start()
    {
        colliderMovingSpeed = CarPool.poolCars[0].GetComponent<Car>().carMovingSpeed;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isChecked)
        {
            if (other.tag == "Player")
            {
                isChecked = true;
                road.CarSpawnerHasPassed();
            }
        }
    }

    void Update()
    {
        if (!isChecked)
            MoveColliderForward();
    }

    void MoveColliderForward()
    {
        myBoxCollider.center = Vector3.MoveTowards(myBoxCollider.center, myBoxCollider.center + Vector3.forward, Time.deltaTime * colliderMovingSpeed);
    }

    public void SetIsCheckedToFalse() //Called from Road.
    {
        isChecked = false;

        myBoxCollider.center = new Vector3(myBoxCollider.center.x, myBoxCollider.center.y, 55f);
    }
}
