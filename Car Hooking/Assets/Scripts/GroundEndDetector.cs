using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundEndDetector : MonoBehaviour
{
    Road road;

    bool isChecked;

    void Awake()
    {
        road = transform.root.GetComponent<Road>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isChecked)
        {
            if (other.tag == "Player")
            {
                isChecked = true;
                road.GroundHasPassed();
            }
        }
    }

    public void SetIsCheckedToFalse()
    {
        isChecked = false;
    }
}
