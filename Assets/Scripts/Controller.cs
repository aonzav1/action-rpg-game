
using UnityEngine;

public class Controller : MonoBehaviour
{
    private MoveUnit target;
    private CombatUnit targetCombat;

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
        targetCombat = newTarget.GetComponent<CombatUnit>();

        Cursor.lockState = CursorLockMode.Locked;

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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(Cursor.lockState == CursorLockMode.None)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;
        }

        RelocateCamera();
        ManageAttack();
        ManageJump();
        ManageDodge();
    }

    private void FixedUpdate()
    {
        if (target == null)
            return;
        ManageMove();
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

    private void ManageMove()
    {
        if (targetCombat && !targetCombat.IsReadyToAttack())
            return;
        target.DoMove(GetTargetDirection());
    }
    
    private void ManageDodge()
    {
        if (targetCombat && !targetCombat.IsReadyToAttack())
            return;
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Vector3 normalizedDir = GetTargetDirection().normalized;
            target.DoDodge(normalizedDir);
        }
    }

    private void ManageJump()
    {
        if (targetCombat && !targetCombat.IsReadyToAttack())
            return;
        if (Input.GetKeyDown(KeyCode.Space))
            target.DoJump();
    }

    private void ManageAttack()
    {
        if (targetCombat == null)
            return;
        if (!IsReadyToAttack())
            return;
        if (Input.GetMouseButtonDown(0))
        {
            targetCombat.RequestAttack(0);
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            targetCombat.RequestAttack(1);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            targetCombat.RequestAttack(2);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            targetCombat.RequestAttack(3);
        }
    }

    private bool IsReadyToAttack()
    {
        return targetCombat.IsReadyToAttack() && !target.IsDodging();
    }

    public void TargetMainMenu()
    {
        transform.position = _mainPos;
        transform.eulerAngles = _mainRot;
    }

    private Vector3 GetTargetDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.TransformDirection(Vector3.forward);

        forward.y = 0.0f;
        forward = forward.normalized;
        Vector3 inputDir = Vector3.ClampMagnitude(new Vector3(h, v, 0), 1);

        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        return forward * inputDir.y + right * inputDir.x;
    }
}