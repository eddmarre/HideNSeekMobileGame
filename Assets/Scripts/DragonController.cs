using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DragonController : PlayerController
{
    [SerializeField] private Transform _fireBallSpawnLocationTransform;
    [SerializeField] private GameObject _fireBall;
    [SerializeField] private Button _shootFireBallButton;
    [SerializeField] private Button _attackButton;
    private int numberOfInteractablesInArea;

    protected override void Start()
    {
        base.Start();

        if (IsClient && IsOwner)
        {
            _shootFireBallButton.onClick.AddListener(() =>
            {
                ShootFireBallServerRpc(_fireBallSpawnLocationTransform.position,
                    GetComponent<NetworkObject>().OwnerClientId);
            });

            _attackButton.onClick.AddListener(() =>
            {
                if (numberOfInteractablesInArea != 0)
                {
                    if (!_colliders[0].TryGetComponent(out SeekerController _seekerController)) return;

                    var otherPlayerPosition = _seekerController.transform.position;
                    AttackPlayerServerRpc(new Vector3(otherPlayerPosition.x, transform.position.y,
                        otherPlayerPosition.z));


                    _seekerController.HitPlayer();
                }
            });
        }
    }

    protected override void Update()
    {
        base.Update();

        if (IsServer)
        {
        }

        if (IsClient && IsOwner)
        {
            UpdateClient();
        }
    }


    private void UpdateClient()
    {
        numberOfInteractablesInArea = Physics.OverlapSphereNonAlloc(_sphereCollider.transform.position,
            _sphereCollider.radius, _colliders,
            _interactLayerMask);

        if (Input.GetKeyDown(KeyCode.Q) && numberOfInteractablesInArea != 0)
        {
            if (!_colliders[0].TryGetComponent(out SeekerController _seekerController)) return;

            var otherPlayerPosition = _seekerController.transform.position;
            AttackPlayerServerRpc(new Vector3(otherPlayerPosition.x, transform.position.y,
                otherPlayerPosition.z));


            _seekerController.HitPlayer();
        }


        if (Input.GetKeyDown(KeyCode.E))
        {
            ShootFireBallServerRpc(_fireBallSpawnLocationTransform.position,
                GetComponent<NetworkObject>().OwnerClientId);
        }
    }


    [ServerRpc]
    private void ShootFireBallServerRpc(Vector3 position, ulong clientID)
    {
        _animator.SetTrigger("fire");

        var fireball = Instantiate(_fireBall, position, Quaternion.identity);
        var fireBallFunc = fireball.GetComponent<FireBall>();

        fireBallFunc.SetForwardDirection(transform.TransformDirection(Vector3.forward));
        fireBallFunc.SetPlayer(NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject
            .GetComponent<GameObject>());

        fireBallFunc.GetComponent<NetworkObject>().SpawnWithOwnership(clientID);
    }


    [ServerRpc]
    private void AttackPlayerServerRpc(Vector3 attackedPlayerPosition)
    {
        transform.LookAt(attackedPlayerPosition);
        _animator.SetTrigger("attack");
    }
}