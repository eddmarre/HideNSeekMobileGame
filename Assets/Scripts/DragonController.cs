using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DragonController : PlayerController
{
    [Header("Dragon Controls")] [SerializeField]
    private Button _shootFireBallButton;

    [SerializeField] private Button _attackButton;

    [Header("Dragon Fire Ball")] [SerializeField]
    private Transform _fireBallSpawnLocationTransform;

    [SerializeField] private GameObject _fireBall;
    [SerializeField] private float _fireBallCooldownTime = 5f;


    private int numberOfInteractablesInArea;
    private float _timeSinceLastFireBallShot = 5f;

    private NetworkVariable<bool> _hasShotFireball = new NetworkVariable<bool>();

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
            if (_hasShotFireball.Value)
            {
                _timeSinceLastFireBallShot -= Time.deltaTime;
                if (_timeSinceLastFireBallShot < -0f)
                {
                    _timeSinceLastFireBallShot = _fireBallCooldownTime;
                    _hasShotFireball.Value = false;
                }
            }
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

        if (_hasShotFireball.Value)
        {
            _shootFireBallButton.interactable = false;
        }
        else
        {
            _shootFireBallButton.interactable = true;
        }

        if (numberOfInteractablesInArea != 0)
        {
            _attackButton.interactable = true;
        }
        else
        {
            _attackButton.interactable = false;
        }

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
        if (_hasShotFireball.Value) return;

        _hasShotFireball.Value = true;

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