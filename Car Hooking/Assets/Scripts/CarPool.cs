using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarPool : MonoBehaviour
{
    public int poolCarsCount = 60;

    public GameObject[] carPrefabs;

    static GameObject[] staticCarPrefabs;

    public static List<GameObject> poolCars = new List<GameObject>();

    static Transform myTransform;

    void Awake()
    {
        myTransform = transform;

        staticCarPrefabs = carPrefabs;

        FillUpThePool();
    }

    void FillUpThePool()
    {
        for (int i = 0; i < poolCarsCount; i++)
        {
            GameObject carGO = Instantiate(carPrefabs[Random.Range(0, 2)]);

            carGO.SetActive(false);
            carGO.transform.parent = myTransform;

            poolCars.Add(carGO);
        }
    }

    public static GameObject InstantiateCarFromPool()
    {
        GameObject carGO = null;

        if (poolCars.Count != 0)
        {
            //print("Car from pool");
            carGO = poolCars[0];
            carGO.SetActive(true);
            poolCars.RemoveAt(0);
        }
        else
        {
            //print("New Car");
            carGO = Instantiate(staticCarPrefabs[Random.Range(0, 2)]);
        }

        return carGO;
    }

    public static void DestroyCarAndAddToPool(GameObject carGO)
    {
        poolCars.Add(carGO);

        carGO.transform.parent = myTransform;

        carGO.GetComponent<Car>().ResetCarToDefaultState();
    }
}