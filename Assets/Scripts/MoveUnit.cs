using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class MoveUnit : NetworkBehaviour
{
    public GameObject _character;
    [SerializeField]
    private bool _clientAuth = true;

    private Entity _entity;

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
        _entity = GetComponent<Entity>();
    }

    public void DoMove(Vector3 targetDir)
    {
        //Prevent unexpected falling
        if (_clientAuth || (!_clientAuth && base.IsServer))
        {
            if (!Physics.Linecast(transform.position + new Vector3(0f, 0.3f, 0f), transform.position - (Vector3.one * 20f)))
                transform.position += new Vector3(0f, 3f, 0f);
        }
        if (_clientAuth)
            Move(targetDir);
        else
            ServerMove(targetDir);
    }

    [ServerRpc]
    private void ServerMove(Vector3 targetDir)
    {
        Move(targetDir);
    }

    private void Move(Vector3 targetDir)
    {
        transform.Translate(targetDir*_entity.GetSpeed() * Time.deltaTime);

      /*  Vector3 vectorDistance = (targetPosition - transform.position);
        vectorDistance.y = transform.position.y;

        Vector3 dirToward = vectorDistance.normalized;
      */
        Rotating(targetDir);
    }

    private void Rotating(Vector3 targetDirection)
    {
        targetDirection.y = 0;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            Quaternion newRotation = Quaternion.Slerp(_character.transform.rotation, targetRotation, _entity.GetTurnRate());
            _character.transform.rotation = newRotation;
        }
    }
}
