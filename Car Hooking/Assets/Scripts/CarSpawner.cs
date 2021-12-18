using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public int carCountInRow;

    public float minSpaceBetweenCars = 1f;
    public float maxSpaceBetweenCars = 3f;

    Transform myTransform;

    float[] rowsZPositions = { 3.5f, 1f, -1.6f, -4f };


    void Awake()
    {
        myTransform = transform;

        Invoke("SpawnCars", 1f);
    }

    public void SpawnCars() //Called From OnEnable and Road.
    {
        SpawnCarInRows(rowsZPositions);
    }

    void SpawnCarInRows(float[] rowsZPos)
    {
        float zNewPos = 0;
        float zOldPos = 0;

        for (int x = 0; x < rowsZPos.Length; x++)
        {
            zNewPos = 0;
            zOldPos = 0;
 
            for (int i = 0; i < carCountInRow; i++)
            {
                GameObject carGO = CarPool.InstantiateCarFromPool();

                zNewPos = Random.Range((zOldPos + minSpaceBetweenCars), (zOldPos + maxSpaceBetweenCars));

                carGO.transform.parent = myTransform;
                carGO.transform.localPosition = new Vector3(rowsZPos[x], 0.6f, zNewPos);

                zOldPos = carGO.transform.localPosition.z;
            }
        }
    }
}