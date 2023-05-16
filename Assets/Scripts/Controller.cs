using FishNet.Object;
using UnityEngine;
using static UnityEditor.SceneView;

public class Controller : MonoBehaviour
{
    private MoveUnit target;

    [Header("Main Menu offset")]
    [SerializeField] private Vector3 _mainPos;
    [SerializeField] private Vector3 _mainRot;

    [Header("Base options")]
    public float smooth = 10f;
    public float horizontalAimingSpeed = 6f;
    public float verticalAimingSpeed = 6f;
    public Vector3 pivotOffset = new Vector3(0.0f, 1.7f, 0.0f);
    public Vector3 camOffset = new Vector3(0.0f, 0.0f, -3.0f);

    public float maxVerticalAngle = 30f;
    public float minVerticalAngle = -60f;

    private float angleH = 0;
    private float angleV = 0;
    private Vector3 smoothPivotOffset;
    private Vector3 smoothCamOffset;

    public float GetH { get { return angleH; } }

    private Camera _camera;

    public static Controller instance;

    private void Awake()
    {
        instance = this;
    }

    public static Controller Get()
    {
        return instance;
    }

    public void AssignTarget(MoveUnit newTarget)
    {
        target = newTarget;

        Cursor.lockState = CursorLockMode.Locked;

        _camera = transform.GetComponent<Camera>();

        transform.position = target.transform.position + Quaternion.identity * pivotOffset + Quaternion.identity * camOffset;
        angleH = target.transform.eulerAngles.y;

        transform.rotation = Quaternion.identity;

        smoothPivotOffset = pivotOffset;
        smoothCamOffset = camOffset;

        if (camOffset.y > 0)
            Debug.LogWarning("Vertical Cam Offset (Y) will be ignored during collisions!\n" +
                "It is recommended to set all vertical offset in Pivot Offset.");
    }

    private void Update()
    {
        if (target == null)
            return;
        RelocateCamera();
    }

    private void FixedUpdate()
    {
        if (target == null)
            return;
        DoControlTarget();
    }

    private void RelocateCamera()
    {
        angleH += Mathf.Clamp(Input.GetAxis("Mouse X"), -1, 1) * horizontalAimingSpeed;
        angleV += Mathf.Clamp(Input.GetAxis("Mouse Y"), -1, 1) * verticalAimingSpeed;

        angleV = Mathf.Clamp(angleV, minVerticalAngle, maxVerticalAngle);

        Quaternion camYRotation = Quaternion.Euler(0, angleH, 0);
        Quaternion aimRotation = Quaternion.Euler(-angleV, angleH, 0);
        transform.rotation = aimRotation;

        Vector3 baseTempPosition = target.transform.position + camYRotation *  pivotOffset;

        Vector3 noCollisionOffset = camOffset;
        while (noCollisionOffset.magnitude >= 0.2f)
        {
            if (DoubleViewingPosCheck(baseTempPosition + aimRotation * noCollisionOffset))
                break;
            noCollisionOffset -= noCollisionOffset.normalized * 0.2f;
        }
        if (noCollisionOffset.magnitude < 0.2f)
            noCollisionOffset = Vector3.zero;

        bool customOffsetCollision = noCollisionOffset.sqrMagnitude < camOffset.sqrMagnitude;

        smoothPivotOffset = Vector3.Lerp(smoothPivotOffset, customOffsetCollision ? pivotOffset : pivotOffset, smooth * Time.deltaTime);
        smoothCamOffset = Vector3.Lerp(smoothCamOffset, customOffsetCollision ? Vector3.zero : noCollisionOffset, smooth * Time.deltaTime);

        transform.position = target.transform.position + camYRotation * smoothPivotOffset + aimRotation * smoothCamOffset;
    }

    bool DoubleViewingPosCheck(Vector3 checkPos)
    {
        return ViewingPosCheck(checkPos) && ReverseViewingPosCheck(checkPos);
    }

    bool ViewingPosCheck(Vector3 checkPos)
    {
        Vector3 targetPos = target.transform.position + pivotOffset;
        Vector3 direction = targetPos - checkPos;

        if (Physics.SphereCast(checkPos, 0.2f, direction, out RaycastHit hit, direction.magnitude))
        {
            if (hit.transform != target.transform && !hit.collider.isTrigger)
            {
                return false;
            }
        }

        return true;
    }


    bool ReverseViewingPosCheck(Vector3 checkPos)
    {
        Vector3 origin = target.transform.position + pivotOffset;
        Vector3 direction = checkPos - origin;
        if (Physics.SphereCast(origin, 0.2f, direction, out RaycastHit hit, direction.magnitude))
        {
            if (hit.transform != target.transform && hit.transform != transform && !hit.collider.isTrigger)
            {
                return false;
            }
        }
        return true;
    }

    private void DoControlTarget()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.TransformDirection(Vector3.forward);

        forward.y = 0.0f;
        forward = forward.normalized;
        Vector3 inputDir = Vector3.ClampMagnitude(new Vector3(h, v, 0), 1);

        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        Vector3 targetDirection = forward * inputDir.y + right * inputDir.x;

        target.DoMove(targetDirection);
    }

    public void TargetMainMenu()
    {
        transform.position = _mainPos;
        transform.eulerAngles = _mainRot;
    }


}