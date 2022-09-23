using System;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class HiderController : PlayerController
{
    [Header("Hider Controls")] [SerializeField]
    private int _hitCount;

    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private Button _interactButton;
    [SerializeField] private LayerMask _playerLayerMask;
    [SerializeField] private Collider[] _hiderColliders;
    [SerializeField] private HitVFX _hitVFX;

    private int _health = 2;
    private int _numberOfInteractablesInArea;
    private int _numberOfPlayersInArea;

    private NetworkVariable<bool> _netIsDead = new NetworkVariable<bool>();
    private PlayerStateContainer _netPlayerState = new PlayerStateContainer();

    public static event Action<ulong> OnDeath;
    public static event Action<ulong> OnRevive;

    #region Monobehaviors

    protected override void Start()
    {
        base.Start();
        _hiderColliders = new Collider[2];
        if (IsServer)
            _netPlayerState._playerState = PlayerStateContainer.PlayerState.Alive;
        if (IsClient && IsOwner)
        {
            InitializeInteractButton();
            _healthText.text = _health.ToString();
        }
    }


    protected override void Update()
    {
        if (IsServer)
        {
            HandleDeath();
        }

        if (_netIsDead.Value) return;
        base.Update();
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

        _numberOfPlayersInArea = CheckAreaAroundPlayer(_sphereCollider, _hiderColliders, _playerLayerMask);

        EnableOrDisableInteractableButtonIfItemsInCollider();
#if UNITY_EDITOR

        if (Input.GetKeyDown(KeyCode.E) && _numberOfInteractablesInArea != 0)
        {
            if (!_interactableColliders[0].TryGetComponent(out Interactable _interactable)) return;
            _interactable.Interact();
        }
#endif
    }

    private void EnableOrDisableInteractableButtonIfItemsInCollider()
    {
        if (_numberOfInteractablesInArea != 0)
        {
            _interactButton.interactable = true;
        }
        else if (_numberOfPlayersInArea > 1)
        {
            foreach (var collider in _hiderColliders)
            {
                if (collider.GetComponent<NetworkObject>().OwnerClientId != OwnerClientId)
                {
                    _interactButton.interactable = collider.GetComponent<HiderController>().GetIsDead();
                }
            }
        }
        else
        {
            _interactButton.interactable = false;
        }
    }
    
    private void InitializeInteractButton()
    {
        _interactButton.interactable = false;
        _interactButton.onClick.AddListener(() =>
        {
            if (_numberOfInteractablesInArea != 0)
            {
                if (!_interactableColliders[0].TryGetComponent(out Interactable _interactable)) return;
                _interactable.Interact();
            }
            else if (_numberOfPlayersInArea > 1)
            {
                ulong clientID;
                foreach (var collider in _hiderColliders)
                {
                    if (collider.GetComponent<NetworkObject>().OwnerClientId != OwnerClientId)
                    {
                        clientID = collider.GetComponent<NetworkObject>().OwnerClientId;
                        ReviveServerRpc(clientID);
                    }
                }
            }
        });
    }

    private Animator GetAnimator()
    {
        return _animator;
    }

    private void SetPlayerState(PlayerStateContainer.PlayerState playerState)
    {
        _netPlayerState._playerState = playerState;
    }

    private void SetIsDead(bool value)
    {
        _netIsDead.Value = value;
    }

    private void SetButtonsInteractable(bool value)
    {
        _interactButton.interactable = value;
        _sprintButton.interactable = value;
    }

    private void HandleDeath()
    {
        _animator.SetBool("hasDied", _netPlayerState._playerState == PlayerStateContainer.PlayerState.Dead);
        _animator.SetBool("isDead", _netIsDead.Value);
        _netPlayerState._playerState = PlayerStateContainer.PlayerState.Dead;
    }

    public void HitPlayer()
    {
        var clientID = GetComponent<NetworkObject>().OwnerClientId;
        HitPlayerServerRpc(clientID);
    }

    public bool GetIsDead()
    {
        return _netIsDead.Value;
    }

    #endregion

    #region ServerRpc

    [ServerRpc(RequireOwnership = false)]
    private void HitPlayerServerRpc(ulong clientId)
    {
        ++_hitCount;
        var currentHealth = _health - _hitCount;
        HitPlayerClientRpc(currentHealth, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] {clientId}
            }
        });
        if (_hitCount >= _health)
        {
            _netPlayerState._playerState = PlayerStateContainer.PlayerState.Dying;
            _netIsDead.Value = true;
            --_hitCount;
            OnDeathClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] {clientId}
                }
            });
            OnDeath?.Invoke(clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReviveServerRpc(ulong clientID)
    {
        var hider = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<HiderController>();
        hider.SetIsDead(false);
        hider.GetAnimator().SetBool("isDead", hider.GetIsDead());
        hider.SetPlayerState(PlayerStateContainer.PlayerState.Alive);
        hider.ReviveClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] {clientID}
            }
        });

        OnRevive?.Invoke(clientID);
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    private void HitPlayerClientRpc(int currentHealth, ClientRpcParams clientRpcParams = default)
    {
        _healthText.text = currentHealth.ToString();
        Instantiate(_hitVFX.gameObject, Vector3.zero, Quaternion.identity);
    }

    [ClientRpc]
    private void OnDeathClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("i've died ClientRpc");
        SetButtonsInteractable(false);
    }

    [ClientRpc]
    private void ReviveClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("I'm being revived");
        SetButtonsInteractable(true);
        var singleHealth = 1;
        _healthText.text = singleHealth.ToString();
    }

    #endregion
}