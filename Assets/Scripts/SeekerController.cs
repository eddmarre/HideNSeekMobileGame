using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SeekerController : PlayerController
{
    [Header("Seeker Controls")] [SerializeField]
    private Button _shootFireBallButton;

    [SerializeField] private Button _attackButton;

    [Header("Seeker Fire Ball")] [SerializeField]
    private Transform _fireBallSpawnLocationTransform;

    [SerializeField] private GameObject _fireBall;
    [SerializeField] private float _fireBallCooldownTime = 5f;


    private int _numberOfInteractablesInArea;
    private float _timeSinceLastFireBallShot = 5f;

    private NetworkVariable<bool> _hasShotFireball = new NetworkVariable<bool>();

    #region Monobehaviors

    protected override void Start()
    {
        base.Start();

        if (IsClient && IsOwner)
        {
            InitializeSeekerButtons();
        }
    }
    
    protected override void Update()
    {
        base.Update();

        if (IsServer)
        {
            FireBallCoolDownHandler();
        }

        if (IsClient && IsOwner)
        {
            UpdateClient();
        }
    }

    #endregion

    #region Methods

    private void UpdateClient()
    {
        _numberOfInteractablesInArea =
            CheckAreaAroundPlayer(_sphereCollider, _interactableColliders, _interactLayerMask);

        CanShootFireBallButtonEnabler();

        CanAttackButtonEnabler();
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Q) && _numberOfInteractablesInArea != 0)
        {
            if (!_interactableColliders[0].TryGetComponent(out HiderController _seekerController)) return;

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
#endif
    }

    private void FireBallCoolDownHandler()
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

    private void InitializeSeekerButtons()
    {
        _shootFireBallButton.onClick.AddListener(() =>
        {
            ShootFireBallServerRpc(_fireBallSpawnLocationTransform.position,
                GetComponent<NetworkObject>().OwnerClientId);
        });

        _attackButton.onClick.AddListener(() =>
        {
            if (_numberOfInteractablesInArea != 0)
            {
                if (!_interactableColliders[0].TryGetComponent(out HiderController _seekerController)) return;
                
                if (_seekerController.GetIsDead()) return;
                
                var otherPlayerPosition = _seekerController.transform.position;
                AttackPlayerServerRpc(new Vector3(otherPlayerPosition.x, transform.position.y,
                    otherPlayerPosition.z));


                _seekerController.HitPlayer();
            }
        });

        _attackButton.interactable = false;
    }

    private void CanAttackButtonEnabler()
    {
        if (_numberOfInteractablesInArea != 0)
        {
            if (!_interactableColliders[0].TryGetComponent(out HiderController _hiderController))
            {
                _attackButton.interactable = false;
                return;
            }

            _attackButton.interactable = !_hiderController.GetIsDead();
        }
    }

    private void CanShootFireBallButtonEnabler()
    {
        if (_hasShotFireball.Value)
        {
            _shootFireBallButton.interactable = false;
        }
        else
        {
            _shootFireBallButton.interactable = true;
        }
    }

    #endregion

    #region ServerRpc

    [ServerRpc]
    private void ShootFireBallServerRpc(Vector3 position, ulong clientID)
    {
        if (_hasShotFireball.Value) return;

        _hasShotFireball.Value = true;

        _animator.SetTrigger("fire");

        var fireball = Instantiate(_fireBall, position, Quaternion.identity);
        var fireBallFunc = fireball.GetComponent<FireBall>();

        fireBallFunc.SetForwardDirection(transform.TransformDirection(Vector3.forward));


        fireBallFunc.GetComponent<NetworkObject>().SpawnWithOwnership(clientID);
    }


    [ServerRpc]
    private void AttackPlayerServerRpc(Vector3 attackedPlayerPosition)
    {
        transform.LookAt(attackedPlayerPosition);
        _animator.SetTrigger("attack");
    }

    #endregion
}