using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class PlayerController : NetworkBehaviour
{
    private NetworkVariable<Vector3> _netMovementPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> _netRotationPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<float> _netWalking = new NetworkVariable<float>();
    private NetworkVariable<float> _netMovement = new NetworkVariable<float>();
    private NetworkVariable<float> _netSprintTime = new NetworkVariable<float>();

    [Header("Player Controller Core")] [SerializeField]
    private bl_Joystick _joystick;

    [SerializeField] private Canvas _canvas;
    [SerializeField] private TextMeshProUGUI _showObjectiveText;
    [SerializeField] protected Animator _animator;
    [SerializeField] protected Button _sprintButton;
    [SerializeField] protected SphereCollider _sphereCollider;
    [SerializeField] protected LayerMask _interactLayerMask;

    [Header("Player movements")] [SerializeField]
    private float _movementSpeed = 1f;

    [SerializeField] private float _sprintSpeed = 2f;
    [SerializeField] private float _rotationSpeed = 20f;


    protected Collider[] _interactableColliders;

    private Vector3 _oldMovementPosition;
    private Vector3 _oldRotationPosition;
    private float _oldWalking;

    private float _initMovementSpeed;
    private bool _isRunning;

    private float _sprintTime;
    private bool _canRun = true;

    #region Monobehavior

    protected virtual void Start()
    {
        _initMovementSpeed = _movementSpeed;
        _interactableColliders = new Collider[1];
        if (IsServer)
            _netMovement.Value = 1f;
    }


    protected virtual void Update()
    {
        if (IsServer)
        {
            UpdateServer();
        }

        if (IsClient && IsOwner)
        {
            UpdateClient();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient && IsOwner)
        {
            PlayerCameraFollow.Instance.FollowPlayer(transform);
            _canvas.gameObject.SetActive(true);
        }
    }

    #endregion

    #region Methods

    private void UpdateServer()
    {
        transform.position += _netMovementPosition.Value *
                              Time.deltaTime * _netMovement.Value;


        transform.Rotate(_netRotationPosition.Value * Time.deltaTime * _rotationSpeed);

        _animator.SetFloat("walking", _netWalking.Value);
        _animator.SetBool("isRunning", _netMovement.Value > _initMovementSpeed);
    }

    private void UpdateClient()
    {
        Vector3 movementPosition = Vector3.zero;
        Vector3 rotation = Vector3.zero;

        movementPosition = transform.TransformDirection(new Vector3(0f, 0f, _joystick.Vertical));
        rotation = new Vector3(0f, _joystick.Horizontal, 0f);


        if (_isRunning && _joystick.Vertical > 0f && _canRun)
        {
            ChangeMovementSpeedServerRpc(_sprintSpeed, NetworkManager.Singleton.LocalClientId);
        }

        if (!_isRunning || !_canRun)
        {
            ChangeMovementSpeedServerRpc(_initMovementSpeed, NetworkManager.Singleton.LocalClientId);
        }


#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftShift) && _joystick.Vertical > 0f)
        {
            if (!_canRun) return;
            ChangeMovementSpeedServerRpc(_sprintSpeed, NetworkManager.Singleton.LocalClientId);
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) || !_canRun)
        {
            ChangeMovementSpeedServerRpc(_initMovementSpeed, NetworkManager.Singleton.LocalClientId);
        }
#endif

        if (_oldMovementPosition != movementPosition ||
            _oldRotationPosition != rotation)
        {
            _oldMovementPosition = movementPosition;
            _oldRotationPosition = rotation;
            MovePlayerServerRpc(movementPosition, rotation);
        }

        float walking = 0f;
        walking = _joystick.Vertical;
        if (walking != _oldWalking)
        {
            _oldWalking = walking;
            ChangePlayerAnimationServerRpc(walking);
        }
    }

    public void SetShowObjectiveText(string message)
    {
        _showObjectiveText.gameObject.SetActive(true);
        _showObjectiveText.text = message;
        StartCoroutine(DisableText());
    }

    #endregion
    
    #region TriggerEvents

    //Event Trigger
    public void SprintOnPointerDown()
    {
        if (_canRun)
            _isRunning = true;
        else
        {
            _isRunning = false;
        }
    }

    //Event Trigger
    public void SprintOnPointerUp()
    {
        _isRunning = false;
    }

    #endregion
    
    #region Coroutines

    private IEnumerator DisableText()
    {
        yield return new WaitForSeconds(10f);
        _showObjectiveText.gameObject.SetActive(false);
    }

    private IEnumerator ResetSprintButtonAfterSetTime()
    {
        yield return new WaitForSeconds(5f);
        _sprintButton.interactable = true;
        _canRun = true;
    }

    #endregion

    #region ServerRpc

    [ServerRpc]
    private void ChangeMovementSpeedServerRpc(float speed, ulong clientID)
    {
        _netMovement.Value = speed;

        if (speed >= _sprintSpeed)
        {
            _netSprintTime.Value += Time.deltaTime;
            if (_netSprintTime.Value >= 5f)
            {
                _netMovement.Value = _initMovementSpeed;
                DisableSprintClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] {clientID}
                    }
                });
                _netSprintTime.Value = 0f;
            }
        }
    }

    [ServerRpc]
    private void MovePlayerServerRpc(Vector3 movementPosition, Vector3 rotation)
    {
        _netMovementPosition.Value = movementPosition;
        _netRotationPosition.Value = rotation;
    }

    [ServerRpc]
    private void ChangePlayerAnimationServerRpc(float walking)
    {
        _netWalking.Value = walking;
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    private void DisableSprintClientRpc(ClientRpcParams rpcParams = default)
    {
        _canRun = false;
        _sprintButton.interactable = false;
        StartCoroutine(ResetSprintButtonAfterSetTime());
    }

    [ClientRpc]
    public void SetShowObjectiveTextClientRpc(string message, ClientRpcParams rpcParams = default)
    {
        _showObjectiveText.gameObject.SetActive(true);
        _showObjectiveText.text = message;
        StartCoroutine(DisableText());
    }

    #endregion
}