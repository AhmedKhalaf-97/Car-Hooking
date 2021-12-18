using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothing = 5f;

    [Header("Camera View")]
    public bool is3DView = true;

    public Vector3 viewCamera2DPosition;
    public Vector3 viewCamera3DPosition;

    public Vector3 viewCamera2DRotation;
    public Vector3 viewCamera3DRotation;

    Vector3 offset2D;
    Vector3 offset3D;

    Vector3 offset;
    Vector3 targetCamPos;

    Transform myTransform;

    Vector3 lerpPos;

    void Awake()
    {
        if (DataSaveManager.IsDataExist("Game View"))
            is3DView = (bool)DataSaveManager.LoadData("Game View");
    }

    void Start()
    {
        myTransform = transform;

        offset2D = viewCamera2DPosition - target.position;
        offset3D = viewCamera3DPosition - target.position;

        SetOffset();
    }

    void Update()
    {
        targetCamPos = target.position + offset;
        lerpPos = Vector3.Lerp(myTransform.position, targetCamPos, smoothing * Time.deltaTime);

        myTransform.position = new Vector3(myTransform.position.x, lerpPos.y, lerpPos.z);
    }

    void SetOffset()
    {
        if (is3DView)
        {
            offset = offset3D;
            myTransform.eulerAngles = viewCamera3DRotation;
        }
        else
        {
            offset = offset2D;
            myTransform.eulerAngles = viewCamera2DRotation;
        }
    }

    public void ChangeCameraView()
    {
        is3DView = !is3DView;   

        SetOffset();

        DataSaveManager.SaveData("Game View", is3DView);
    }
}