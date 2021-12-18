using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road : MonoBehaviour
{
    public GameManager gameManager;

    public Transform grounds;
    public Transform carSpawners;

    int groundsPassed;
    int totalGroundsPassed = 1;
    Transform firstGroundPassed;

    public int carSpawnerPassed;
    Transform firstCarSpawnerPassed;
    BoxCollider lastCarSpawnerDetector;

    Transform myTransform;

    void Awake()
    {
        myTransform = transform;
    }

    void Start() { }

    public void GroundHasPassed()
    {
        groundsPassed++;
        totalGroundsPassed++;

        SetNewGroundIfPossible();
    }

    void SetNewGroundIfPossible()
    {
        if (gameManager.isLevelCompleted)
            return;

        if (groundsPassed == 2)
        {
            groundsPassed = 1;

            firstGroundPassed = grounds.GetChild(0);

            firstGroundPassed.parent = null;
            firstGroundPassed.parent = grounds;

            firstGroundPassed.localPosition = new Vector3(firstGroundPassed.localPosition.x, firstGroundPassed.localPosition.y, (totalGroundsPassed * 60.55f));

            firstGroundPassed.transform.GetComponentInChildren<GroundEndDetector>().SetIsCheckedToFalse();
        }
    }

    public void CarSpawnerHasPassed()
    {
        carSpawnerPassed++;

        SetNewCarsIfPossible();
    }

    void SetNewCarsIfPossible()
    {
        if (carSpawnerPassed == 2)
        {
            carSpawnerPassed = 1;

            firstCarSpawnerPassed = carSpawners.GetChild(0);
            lastCarSpawnerDetector = carSpawners.GetChild(2).GetComponent<BoxCollider>();

            firstCarSpawnerPassed.parent = null;
            firstCarSpawnerPassed.parent = carSpawners;

            firstCarSpawnerPassed.localPosition = new Vector3(firstCarSpawnerPassed.localPosition.x, firstCarSpawnerPassed.localPosition.y,
                (lastCarSpawnerDetector.center.z + lastCarSpawnerDetector.transform.localPosition.z));

            firstCarSpawnerPassed.GetComponent<CarSpawnerEndDetector>().SetIsCheckedToFalse();

            int childCount = firstCarSpawnerPassed.childCount;

            for (int i = 0; i < childCount; i++)
            {
                CarPool.DestroyCarAndAddToPool(firstCarSpawnerPassed.GetChild(0).gameObject);
            }

            firstCarSpawnerPassed.GetComponent<CarSpawner>().SpawnCars();
        }
    }
}
