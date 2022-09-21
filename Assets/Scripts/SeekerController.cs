using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class SeekerController : PlayerController
{
    [Header("Seeker Controls")] [SerializeField]
    private int _hitCount;

    [SerializeField] private Button _interactButton;
    private int _health = 2;
    private bool _isDead;
    private int numberOfInteractablesInArea;

    #region Monobehaviors

    protected override void Start()
    {
        base.Start();
        if (IsClient && IsOwner)
        {
            _interactButton.interactable = false;
            _interactButton.onClick.AddListener(() =>
            {
                if (numberOfInteractablesInArea != 0)
                {
                    if (!_colliders[0].TryGetComponent(out Interactable _interactable)) return;
                    _interactable.Interact();
                    // _interactButton.interactable = false;
                }
            });
        }
    }

    protected override void Update()
    {
        if (_isDead) return;
        base.Update();
        if (IsClient && IsOwner)
        {
            UpdateClient();
        }
    }

    #endregion

    private void UpdateClient()
    {
        numberOfInteractablesInArea = Physics.OverlapSphereNonAlloc(_sphereCollider.transform.position,
            _sphereCollider.radius, _colliders,
            _interactLayerMask);

        if (numberOfInteractablesInArea != 0)
        {
            _interactButton.interactable = true;
        }
        else
        {
            _interactButton.interactable = false;
        }

        if (Input.GetKeyDown(KeyCode.E) && numberOfInteractablesInArea != 0)
        {
            if (!_colliders[0].TryGetComponent(out Interactable _interactable)) return;
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
                _isDead = true;
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

    #region ServerRpc

    [ServerRpc(RequireOwnership = false)]
    private void HitPlayerServerRpc(ulong clientId)
    {
        ++_hitCount;

        if (_hitCount > _health)
        {
            _isDead = true;
            _animator.SetTrigger("isDead");
            OnDeathClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] {clientId}
                }
            });
        }
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    private void OnDeathClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("i've died ClientRpc");
        //Death Features go here
    }

    #endregion
}