using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


public class MoveUnit : NetworkBehaviour
{
    [SerializeField]
    private bool _clientAuth = true;

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
        if(base.IsOwner)
            Controller.Get().AssignTarget(this);
    }
    public override void OnStopClient()
    {
        base.OnStopClient();
        if (base.IsOwner)
            Controller.Get().TargetMainMenu();
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _entity = GetComponent<Entity>();
        _collider = GetComponent<Collider>();
    }

    private void Update()
    {
        _collider.enabled = !isDodging;
        _rigidbody.useGravity = !isDodging;

        if (!base.IsOwner)
            return;

        if (_clientAuth || (!_clientAuth && base.IsServer))
        {
            _entity.GetAnim().SetBool("isGrounded", IsGrounded());
        }
    }

    public void DoMove(Vector3 targetDir)
    {
        if (_clientAuth)
            Move(targetDir);
        else
            ServerMove(targetDir);
    }
    public void DoJump()
    {
        if (_clientAuth)
            Jump();
    }
    public void DoDodge(Vector3 targetDir)
    {
        if (_clientAuth)
            Dodge(targetDir);
    }

    private bool IsGrounded()
    {
        return Physics.Linecast(transform.position + new Vector3(0f, 0.3f, 0f), transform.position - (Vector3.one * 0.5f));
    }

    [ServerRpc]
    private void ServerMove(Vector3 targetDir)
    {
        Move(targetDir);
    }

    private void Move(Vector3 targetDir)
    {
        if (isDodging)
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
        if (!IsGrounded() || isDodging)
            return;

        if (!_entity.ConsumeStamina(jumpCost))
            return;

        _rigidbody.AddForce(0, jumpForce, 0);
        ShowJumpAnimation();
    }

    [ObserversRpc]
    private void ShowJumpAnimation()
    {
        _entity.GetAnim().Play("Jump");
    }

    private void Dodge(Vector3 targetDir)
    {
        if (!IsGrounded() || isDodging)
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

    [ObserversRpc]
    private void ShowDodgeAnimation()
    {
        _entity.GetAnim().Play("Dodge");
    }

    public bool IsDodging()
    {
        return isDodging;
    }


}
