using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class HiderController : PlayerController
{
    [Header("Hider Controls")] [SerializeField]
    private int _hitCount;

    [SerializeField] private Button _interactButton;
    [SerializeField] private LayerMask _playerLayerMask;
    [SerializeField] private Collider[] _hiderColliders;
    private int _health = 2;
    private bool _isDead;
    private int _numberOfInteractablesInArea;
    private int _numberOfPlayersInArea;

    private NetworkVariable<bool> _netIsDead = new NetworkVariable<bool>();

    #region Monobehaviors

    protected override void Start()
    {
        base.Start();
        _hiderColliders = new Collider[2];
        if (IsClient && IsOwner)
        {
            _interactButton.interactable = false;
            _interactButton.onClick.AddListener(() =>
            {
                if (_numberOfInteractablesInArea != 0)
                {
                    if (!_interactableColliders[0].TryGetComponent(out Interactable _interactable)) return;
                    _interactable.Interact();
                    // _interactButton.interactable = false;
                }
                else if (_numberOfPlayersInArea > 1)
                {
                    // if (!_hiderColliders[1].GetComponent<HiderController>().GetIsDead()) return;
                    // var client = _hiderColliders[1].GetComponent<NetworkObject>().OwnerClientId;
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
            _animator.SetBool("isDead", _netIsDead.Value);
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

            if (_hitCount > _health)
            {
                // _isDead = true;
                _netIsDead.Value = true;
                _animator.SetTrigger("isDead");
                OnDeathClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] {clientID}
                    }
                });
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

    public void SetIsDead(bool value)
    {
        _netIsDead.Value = value;
    }

    #region ServerRpc

    [ServerRpc(RequireOwnership = false)]
    private void HitPlayerServerRpc(ulong clientId)
    {
        ++_hitCount;

        if (_hitCount > _health)
        {
            //_isDead = true;
            _netIsDead.Value = true;
            // _animator.SetBool("isDead",_netIsDead.Value);
            OnDeathClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] {clientId}
                }
            });
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReviveServerRpc(ulong clientID)
    {
        var hider = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<HiderController>();
        hider.SetIsDead(false);

        hider.ReviveClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] {clientID}
            }
        });
    }

    #endregion

    #region ClientRpc

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
        // ReviveServerRpc();
    }

    #endregion
}