using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class SeekerController : PlayerController
{
    [SerializeField] private int _hitCount;
    [SerializeField] private Button _interactButton;
    private int _health = 2;
    private bool _isDead;
    private int numberOfInteractablesInArea;
    

    protected override void Start()
    {
        base.Start();
        if (IsClient && IsOwner)
        {
            _interactButton.onClick.AddListener(() =>
            {
                if (numberOfInteractablesInArea != 0)
                {
                    if (!_colliders[0].TryGetComponent(out Interactable _interactable)) return;
                    _interactable.Interact();
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

    private void UpdateClient()
    {
         numberOfInteractablesInArea = Physics.OverlapSphereNonAlloc(_sphereCollider.transform.position,
            _sphereCollider.radius, _colliders,
            _interactLayerMask);

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

    [ClientRpc]
    private void OnDeathClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("i've died ClientRpc");
        //Death Features go here
    }
}