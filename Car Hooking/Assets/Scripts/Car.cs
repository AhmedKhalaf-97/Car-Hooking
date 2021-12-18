using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    [Header("Moving Forward")]
    public bool isSlowing;
    public bool isAligning;
    public float carMovingSpeed = 5f;
    float defaultCarMovingSpeed = 5f;
    public float carAligningSpeed = 10f;
    public float actualMovingSpeed;

    [Header("Steering")]
    public bool isSteering;
    public float carSteeringSpeed = 50f;
    public float carSteeringAngle = 10f;

    float[] roadLanes = { 3.5f, 1f, -1.6f, -4f };
    float currentLane;
    float expectedLane;

    Coroutine turnningCoroutine;

    [Header("Maneuver")]
    public bool isFrontMiddleCarSeen;
    public bool isFrontRightCarSeen;
    public bool isFrontLeftCarSeen;
    public bool isMiddleRightCarSeen;
    public bool isMiddleLeftCarSeen;

    public bool canTurnRight;
    public bool canTurnLeft;

    [Header("Front Sensor")]
    public float frontRayDistance = 2f;
    public float frontSidesRayDistance = 2.5f;
    public Transform frontSensor;

    Vector3 frontSidesPosOffset = new Vector3(0.45f, 0f, 0f);
    Vector3 frontRightDirection = new Vector3(0.45f, 0f, 1f);
    Vector3 frontLeftDirection = new Vector3(-0.45f, 0f, 1f);

    RaycastHit frontMiddleHit;
    RaycastHit frontRightHit;
    RaycastHit frontLeftHit;

    [Header("Middle Sensor")]
    public float middleRayDistance = 1.6f;
    public Transform middleSensor;

    Vector3 middleRayOffset = new Vector3(0f, 0f, 0.9f);

    RaycastHit middleRightHit;
    RaycastHit middleLeftHit;



    [Header("Lights")]
    public float redLightTimeLength = 1f;
    public float steeringLightSignalRate = 0.5f;
    public GameObject leftBackLight;
    public GameObject rightBackLight;
    public GameObject redLights;

    Coroutine steeringLightSignalVar;
    Coroutine redLightsOnVar;

    [Header("Materials")]
    public MeshRenderer carMeshRenderer;
    public Material[] carMaterials;


    Transform myTransform;
    GameObject carModel;
    bool shouldDestroyed;

    CarSpawner carSpawner;
    GrapplingHook grapplingHook;


    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Hookable")
        {
            if (myTransform.localPosition.z < other.transform.localPosition.z)
            {
                actualMovingSpeed = 0f;

                if (!shouldDestroyed)
                    StartCoroutine(DestroyThisCar());
            }
        }
    }

    void Awake()
    {
        myTransform = transform;

        carModel = myTransform.GetChild(2).gameObject;

        actualMovingSpeed = carMovingSpeed;
        defaultCarMovingSpeed = carMovingSpeed;

        grapplingHook = GameObject.FindGameObjectWithTag("Player").GetComponent<GrapplingHook>();

        //Set Random Material.
        carMeshRenderer.material = carMaterials[Random.Range(0, carMaterials.Length)];
    }

    void OnEnable()
    {
        Invoke("SetCarSpawnerVar", 1f);
    }

    void SetCarSpawnerVar() //Called From OnEnable.
    {
        if (myTransform.parent != null)
            carSpawner = myTransform.parent.GetComponent<CarSpawner>();
    }

    void Update()
    {
        if (shouldDestroyed)
            return;

        CarMoving();

        CarManeuver();

        if (isAligning)
            AlignCar();
    }

    void CarMoving()
    {
        myTransform.Translate(Vector3.forward * actualMovingSpeed * Time.deltaTime);
    }

    void CarMovingAfterSlowing()
    {
        if (redLightsOnVar != null)
            StopCoroutine(redLightsOnVar);

        redLights.SetActive(false);

        isSlowing = false;
        actualMovingSpeed = carMovingSpeed;

        AlignCar();
    }

    void AlignCar()
    {
        isAligning = true;

        myTransform.localEulerAngles = new Vector3(myTransform.localEulerAngles.x, Mathf.LerpAngle(myTransform.localEulerAngles.y, 0f, carAligningSpeed * Time.deltaTime), myTransform.localEulerAngles.z);

        if (IsEularAngleAxisBetweenValues(myTransform.localEulerAngles.y, -1f, 1f))
        {
            isSteering = false;

            myTransform.localEulerAngles = Vector3.zero;

            isAligning = false;
        }
    }

    void CarSteering(Vector3 direction)
    {
        if (myTransform.localPosition.x > 0)
        {
            if (expectedLane == roadLanes[3])
                expectedLane = roadLanes[0];
        }
        else
        {
            if (expectedLane == roadLanes[0])
                expectedLane = roadLanes[3];
        }

        if (IsEularAngleAxisBetweenValues(myTransform.localEulerAngles.y, -carSteeringAngle, carSteeringAngle))
        {
            isSteering = true;

            myTransform.Rotate(direction, carSteeringSpeed * Time.deltaTime, Space.Self);

            if (steeringLightSignalVar != null)
                StopCoroutine(steeringLightSignalVar);

            steeringLightSignalVar = StartCoroutine(SteeringLightSignal(myTransform.localRotation.y));
        }
    }

    IEnumerator TurnCar(Vector3 direction)
    {
        currentLane = myTransform.localPosition.x;

        for (int i = 0; i < roadLanes.Length; i++)
        {
            if(currentLane == roadLanes[i])
            {
                if (direction.y > 0f)
                {
                    if (0 < i)
                        expectedLane = roadLanes[i - 1];
                }
                else
                {
                    if (i < (roadLanes.Length - 1))
                        expectedLane = roadLanes[i + 1];
                }
            }
        }

        while (myTransform.localPosition.x != expectedLane)
        {
            CarSteering(direction);

            if (IsFloatBetweenValues(myTransform.localPosition.x, (expectedLane - 0.05f), (expectedLane + 0.05f)))
            {
                isAligning = true;
                break;
            }

            yield return null;
        }

        while (isAligning)
        {
            if (isSlowing)
            {
                CarMovingAfterSlowing();
            }
            else
            {
                AlignCar();
            }

            yield return null;
        }

        myTransform.localPosition = new Vector3(expectedLane, myTransform.localPosition.y, myTransform.localPosition.z);

    }

    void SlowingDown(float speed)
    {
        if (redLightsOnVar != null)
            StopCoroutine(redLightsOnVar);

        redLightsOnVar = StartCoroutine(TurnRedLightsOn());

        if(actualMovingSpeed != speed)
        {
            isSlowing = true;
            actualMovingSpeed = speed;
        }
    }

    void CarManeuver()
    {
        if (Physics.Raycast(frontSensor.position, Vector3.forward, out frontMiddleHit, frontRayDistance))
        {
            if(frontMiddleHit.transform.tag == "Hookable")
            {
                isFrontMiddleCarSeen = true;
                //print("Middle Front Car Deteted");
            }
        }
        else
        {
            isFrontMiddleCarSeen = false;
        }


        if (isFrontMiddleCarSeen)
        {
            RunAllSensors();

            if (!canTurnLeft && !isFrontRightCarSeen && !isMiddleRightCarSeen && myTransform.localPosition.x <= 2.8f)
            {
                //print("Car can pass from right side");

                canTurnRight = true;

                SlowingDown(actualMovingSpeed);

                if (turnningCoroutine != null)
                    StopCoroutine(turnningCoroutine);

                turnningCoroutine = StartCoroutine(TurnCar(Vector3.up));
            }
            else
            {
                canTurnRight = false;
            }

            if (!canTurnRight && !isFrontLeftCarSeen && !isMiddleLeftCarSeen && myTransform.localPosition.x >= -2.8f)
            {
                //print("Car can pass from left side");

                canTurnLeft = true;

                SlowingDown(actualMovingSpeed);

                if (turnningCoroutine != null)
                    StopCoroutine(turnningCoroutine);

                turnningCoroutine = StartCoroutine(TurnCar(Vector3.down));
            }
            else
            {
                canTurnLeft = false;
            }

            if (!canTurnLeft && !canTurnRight)
            {
                //print("Car only slowing down");

                if (turnningCoroutine != null)
                    StopCoroutine(turnningCoroutine);

                if (frontMiddleHit.transform.GetComponent<Car>() != null)
                {
                    float frontMiddleCarSpeed = frontMiddleHit.transform.GetComponent<Car>().actualMovingSpeed;

                    if (!isSlowing)
                    {
                        SlowingDown((frontMiddleCarSpeed - 1f));
                    }

                    carMovingSpeed = frontMiddleCarSpeed;
                }

                Invoke("CarMovingAfterSlowing", redLightTimeLength);
            }
        }
        else
        {
            ResetAllSensorBooleans();
        }
    }

    void RunAllSensors()
    {
        SensorsRaysDrawingDebug();

        if (Physics.Raycast(frontSensor.position + frontSidesPosOffset, frontRightDirection, out frontRightHit, frontSidesRayDistance))
        {
            if (frontRightHit.transform.tag == "Hookable")
            {
                isFrontRightCarSeen = true;
                //print("Right Front Car Deteted");
            }
        }
        else
        {
            isFrontRightCarSeen = false;
        }

        if (Physics.Raycast(frontSensor.position - frontSidesPosOffset, frontLeftDirection, out frontLeftHit, frontSidesRayDistance))
        {
            if (frontLeftHit.transform.tag == "Hookable")
            {
                isFrontLeftCarSeen = true;
                //print("Left Front Car Deteted");
            }
        }
        else
        {
            isFrontLeftCarSeen = false;
        }

        if (Physics.Raycast(middleSensor.position, Vector3.right, out middleRightHit, middleRayDistance)
            || Physics.Raycast(middleSensor.position + middleRayOffset, Vector3.right, out middleRightHit, middleRayDistance))
        {
            if (middleRightHit.transform.tag == "Hookable")
            {
                isMiddleRightCarSeen = true;
                //print("Middle Right Car Deteted");
            }
        }
        else
        {
            isMiddleRightCarSeen = false;
        }

        if (Physics.Raycast(middleSensor.position, Vector3.left, out middleLeftHit, middleRayDistance)
            || Physics.Raycast(middleSensor.position + middleRayOffset, Vector3.left, out middleLeftHit, middleRayDistance))
        {
            if (middleLeftHit.transform.tag == "Hookable")
            {
                isMiddleLeftCarSeen = true;
                //print("Middle Left Car Deteted");
            }
        }
        else
        {
            isMiddleLeftCarSeen = false;
        }
    }

    void ResetAllSensorBooleans()
    {
        isFrontRightCarSeen = false;
        isFrontLeftCarSeen = false;
        isMiddleRightCarSeen = false;
        isMiddleLeftCarSeen = false;

        canTurnRight = false;
        canTurnLeft = false;
    }

    bool IsEularAngleAxisBetweenValues(float axis, float minValue, float maxValue)
    {
        bool returnedValue;

        if (axis <= maxValue && axis >= 0 || axis >= (360 + minValue) && axis <= 360)
        {
            returnedValue = true;
        }
        else
        {
            returnedValue = false;
        }

        return returnedValue;
    }

    bool IsFloatBetweenValues(float currentValue, float minValue, float maxValue)
    {
        bool returnedValue;

        if (currentValue <= maxValue && currentValue >= minValue)
        {
            returnedValue = true;
        }
        else
        {
            returnedValue = false;
        }

        return returnedValue;
    }

    void TurnOffAllCarLights()
    {
        if (redLightsOnVar != null)
            StopCoroutine(redLightsOnVar);

        if (steeringLightSignalVar != null)
            StopCoroutine(steeringLightSignalVar);

        redLights.SetActive(false);

        rightBackLight.SetActive(false);
        leftBackLight.SetActive(false);
    }

    IEnumerator TurnRedLightsOn()
    {
        redLights.SetActive(true);
        yield return new WaitForSeconds(redLightTimeLength);
        redLights.SetActive(false);
    }

    IEnumerator SteeringLightSignal(float dir)
    {
        rightBackLight.SetActive(false);
        leftBackLight.SetActive(false);

        while (isSteering)
        {
            if (dir > 0)
            {
                rightBackLight.SetActive(true);
                yield return new WaitForSeconds(steeringLightSignalRate);
                rightBackLight.SetActive(false);
                yield return new WaitForSeconds(steeringLightSignalRate);

            }

            if (dir < 0)
            {
                leftBackLight.SetActive(true);
                yield return new WaitForSeconds(steeringLightSignalRate);
                leftBackLight.SetActive(false);
                yield return new WaitForSeconds(steeringLightSignalRate);
            }

            yield return null;
        }

        rightBackLight.SetActive(false);
        leftBackLight.SetActive(false);
    }

    IEnumerator DestroyThisCar()
    {
        shouldDestroyed = true;

        if (steeringLightSignalVar != null)
            StopCoroutine(steeringLightSignalVar);

        if (redLightsOnVar != null)
            StopCoroutine(redLightsOnVar);

        myTransform.tag = "Untagged";

        myTransform.GetChild(0).parent = carModel.transform;

        carModel.SetActive(false);
        yield return new WaitForSeconds(0.25f * Time.timeScale);
        carModel.SetActive(true);
        yield return new WaitForSeconds(0.25f * Time.timeScale);

        grapplingHook.DetachHookFromCar(myTransform);

        carModel.SetActive(false);
        yield return new WaitForSeconds(0.25f * Time.timeScale);
        carModel.SetActive(true);


        Destroy(gameObject);
    }

    public void IncreaseCarSpeedWhenPlayerConnected() //Called from GrapplingHook.
    {
        actualMovingSpeed += 2f;
    }

    public void ResetActualMovingSpeed() //Called from GrapplingHook.
    {
        actualMovingSpeed = carMovingSpeed;
    }

    public void ResetCarToDefaultState() //Called from CarPool.
    {
        if (shouldDestroyed)
        {
            Destroy(gameObject);
            return;
        }

        StopAllCoroutines();

        //Reset Position and Rotation.
        myTransform.localPosition = Vector3.zero;
        myTransform.localEulerAngles = Vector3.zero;

        //Reset Car Variables.
        isSlowing = false;
        isAligning = false;
        carMovingSpeed = defaultCarMovingSpeed;
        actualMovingSpeed = carMovingSpeed;

        isSteering = false;

        currentLane = 0f;
        expectedLane = 0f;

        isFrontMiddleCarSeen = false;
        ResetAllSensorBooleans();

        TurnOffAllCarLights();

        carSpawner = null;

        gameObject.SetActive(false);
    }

    void SensorsRaysDrawingDebug()
    {
        Debug.DrawRay(frontSensor.position, frontSensor.TransformDirection(Vector3.forward) * frontRayDistance);
        Debug.DrawRay(frontSensor.position + frontSidesPosOffset, frontSensor.TransformDirection(frontRightDirection) * frontSidesRayDistance);
        Debug.DrawRay(frontSensor.position - frontSidesPosOffset, frontSensor.TransformDirection(frontLeftDirection) * frontSidesRayDistance);

        Debug.DrawRay(middleSensor.position, middleSensor.TransformDirection(Vector3.right) * middleRayDistance);
        Debug.DrawRay(middleSensor.position, middleSensor.TransformDirection(Vector3.left) * middleRayDistance);

        Debug.DrawRay(middleSensor.position + middleRayOffset, middleSensor.TransformDirection(Vector3.right) * middleRayDistance);
        Debug.DrawRay(middleSensor.position + middleRayOffset, middleSensor.TransformDirection(Vector3.left) * middleRayDistance);
    }

    void TrialsControllerInputs()
    {
        if (Input.GetKey(KeyCode.A))
        {
            StartCoroutine(TurnCar(Vector3.down));
        }

        if (Input.GetKey(KeyCode.D))
        {
            StartCoroutine(TurnCar(Vector3.up));
        }

        if (Input.GetKey(KeyCode.S))
        {
            SlowingDown(3f);
        }

        if (Input.GetKey(KeyCode.W))
        {
            CarMovingAfterSlowing();
        }
    }
}