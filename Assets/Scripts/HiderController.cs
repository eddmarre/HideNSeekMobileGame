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
    private bool _isDead;
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
            _interactButton.interactable = false;
            _healthText.text = _health.ToString();
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
    }

    protected override void Update()
    {
        if (IsServer)
        {
            _animator.SetBool("hasDied",_netPlayerState._playerState == PlayerStateContainer.PlayerState.Dead);
            _animator.SetBool("isDead", _netIsDead.Value);
            _netPlayerState._playerState = PlayerStateContainer.PlayerState.Dead;
        }

        if (_netIsDead.Value) return;
        base.Update();
        if (IsClient && IsOwner)
        {
            UpdateClient();
        }
    }

    #endregion

    private void UpdateClient()
    {
        var _detectionPosition = _sphereCollider.transform.position;
        _numberOfInteractablesInArea = Physics.OverlapSphereNonAlloc(_detectionPosition,
            _sphereCollider.radius, _interactableColliders,
            _interactLayerMask);

        _numberOfPlayersInArea = Physics.OverlapSphereNonAlloc(_detectionPosition,
            _sphereCollider.radius,
            _hiderColliders, _playerLayerMask);

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

        if (Input.GetKeyDown(KeyCode.E) && _numberOfInteractablesInArea != 0)
        {
            if (!_interactableColliders[0].TryGetComponent(out Interactable _interactable)) return;
            _interactable.Interact();
        }
    }

    public void HitPlayer()
    {
        var clientID = GetComponent<NetworkObject>().OwnerClientId;
        if (IsServer)
        {
            ++_hitCount;
            var currentHealth = _health - _hitCount;
            HitPlayerClientRpc(currentHealth, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] {clientID}
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
                        TargetClientIds = new ulong[] {clientID}
                    }
                });
                OnDeath?.Invoke(clientID);
            }
        }
        else if (IsClient)
        {
            HitPlayerServerRpc(clientID);
        }
    }

    public bool GetIsDead()
    {
        return _netIsDead.Value;
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
        _interactButton.interactable = false;
        _sprintButton.interactable = false;
        _isDead = true;
        //Death Features go here
    }

    [ClientRpc]
    private void ReviveClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("I'm being revived");
        _interactButton.interactable = true;
        _sprintButton.interactable = true;
        _isDead = false;
        var singleHealth = 1;
        _healthText.text = singleHealth.ToString();
    }

    #endregion
}

[Serializable]
public class PlayerStateContainer : NetworkVariableBase
{
    public enum PlayerState
    {
        Alive = 0,
        Dying = 1,
        Dead = 2
    }

    public PlayerState _playerState;

    public override void WriteDelta(FastBufferWriter writer)
    {
    }

    public override void WriteField(FastBufferWriter writer)
    {
        writer.WriteValueSafe((int) _playerState);
    }

    public override void ReadField(FastBufferReader reader)
    {
        int tempPlayerState;
        reader.ReadValueSafe(out tempPlayerState);
        _playerState = (PlayerState) tempPlayerState;
    }

    public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
    {
    }
}