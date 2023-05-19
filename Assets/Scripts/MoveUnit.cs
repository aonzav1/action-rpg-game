using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


public class MoveUnit : NetworkBehaviour
{

    [SerializeField] private float speed = 4;
    [SerializeField] private float turnRate = 0.15f;
    [SerializeField] private float jumpForce = 250;
    [SerializeField] private float dodgeSpeed = 15;
    [SerializeField] private float dodgeTime = 0.5f;

    [SerializeField] private float jumpCost = 10;
    [SerializeField] private float dodgeCost = 15;

    private Rigidbody _rigidbody;
    private Entity _entity;
    private Collider _collider;

    private bool isDodging;
    private Vector3 dodgeDir;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
            Controller.Get().AssignTarget(this);
        else
            _rigidbody.useGravity = false;
    }
    public override void OnStopClient()
    {
        base.OnStopClient();
        if (base.IsOwner)
            Controller.Get().TargetMainMenu();
    }

    private void Awake()
    {
        _entity = GetComponent<Entity>();
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        Vector3 newVector = transform.localEulerAngles;
        _entity._character.transform.localEulerAngles = newVector;
        transform.eulerAngles = Vector3.zero ;
    }

    private void Update()
    {
        if(_entity.IsDie())
        {
            _collider.enabled = false;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            return;
        }
        _collider.enabled = !isDodging;

        if (!IsAuthorized())
        {
            _rigidbody.isKinematic = true;
            return;
        }
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = !isDodging;
        _entity.GetAnim().SetBool("isGrounded", IsGrounded());
    }

    public bool IsAuthorized()
    {
        if(_entity.serverAuth && base.IsServer)
            return true;
        if (!_entity.serverAuth && base.IsOwner)
            return true;
        return false;
    }

    public void DoMove(Vector3 targetDir)
    {
        Move(targetDir);
    }
    public void DoRotate(Vector3 targetDir)
    {
        Rotating(targetDir,false);
    }
    public void DoJump()
    {
        Jump();
    }
    public void DoDodge(Vector3 targetDir)
    {
        Dodge(targetDir);
    }

    private bool IsGrounded()
    {
        return Physics.Linecast(transform.position + new Vector3(0f, 0.3f, 0f), transform.position - (Vector3.one * 0.5f));
    }

    private void Move(Vector3 targetDir)
    {
        if (isDodging || !_entity.IsControllable())
            return;

        transform.Translate(targetDir*speed * Time.deltaTime);

        _entity.GetAnim().SetBool("isMoving", targetDir.magnitude > 0.01f);
        Rotating(targetDir,false);
    }

    private void Rotating(Vector3 targetDirection, bool forceRotate)
    {
        targetDirection.y = 0;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            if (!forceRotate)
            {
                Quaternion newRotation = Quaternion.Slerp(_entity.GetCharacterTransform().rotation, targetRotation, turnRate);
                _entity.GetCharacterTransform().rotation = newRotation;
            }
            else
            {
                _entity.GetCharacterTransform().rotation = targetRotation;
            }
        }
    }

    private void Jump()
    {
        if (!IsGrounded() || isDodging || !_entity.IsControllable())
            return;

        if (!_entity.ConsumeStamina(jumpCost))
            return;

        _rigidbody.AddForce(0, jumpForce, 0);
        if(!_entity.serverAuth)
            RequestJump();
        else
            ShowJumpAnimation();
    }

    [ServerRpc(RequireOwnership =false)]
    private void RequestJump()
    {
        ShowJumpAnimation();
    }

    [ObserversRpc(RunLocally = true)]
    private void ShowJumpAnimation()
    {
        _entity.GetAnim().Play("Jump");
    }

    private void Dodge(Vector3 targetDir)
    {
        if (!IsGrounded() || isDodging || !_entity.IsControllable())
            return;

        if (!_entity.ConsumeStamina(dodgeCost))
            return;

        if (targetDir.magnitude <= 0.01f)
        {
            targetDir = _entity.GetCharacterTransform().forward;
        }
        else
        {
            Rotating(targetDir, true);
        }
        _rigidbody.velocity = targetDir * dodgeSpeed;
        StartCoroutine(DodgeScenario());
        if (!_entity.serverAuth)
            RequestDodge();
        else
            ShowDodgeAnimation();
    }

    IEnumerator DodgeScenario()
    {
        isDodging = true;
        yield return new WaitForSeconds(dodgeTime);
        isDodging = false;

        _entity.GetCharacterTransform().localPosition = new Vector3(0, 0, 0);
        RemoveForces();
    }

    private void RemoveForces()
    {
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.velocity = Vector3.zero;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDodge()
    {
        StartCoroutine(DodgeScenario());
        ShowDodgeAnimation();
    }

    [ObserversRpc(RunLocally =true)]
    private void ShowDodgeAnimation()
    {
        _entity.GetAnim().Play("Dodge");
    }

    public bool IsDodging()
    {
        return isDodging;
    }

    public void StopMove()
    {
        _entity.GetAnim().SetBool("isMoving", false);
    }


}
