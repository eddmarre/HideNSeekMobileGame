using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class Interactable : NetworkBehaviour
{
    public void Interact()
    {
        if (IsServer)
        {
            Destroy(gameObject);
        }

        if (IsClient)
        {
            DestroyObjectServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyObjectServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
    }
}