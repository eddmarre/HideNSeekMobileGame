using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;

public class FireBall : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 50f;
    private Vector3 _forwardDirection;


    private void Start()
    {
        StartCoroutine(DestroyAfterSomeTime(5f));
    }

    private IEnumerator DestroyAfterSomeTime(float time)
    {
        yield return new WaitForSeconds(time);
        DespawnOnNetworkServerRpc();
    }

    private void Update()
    {
        if (IsServer)
            transform.position += _forwardDirection * Time.deltaTime * movementSpeed;
    }

    public void SetForwardDirection(Vector3 dir)
    {
        _forwardDirection = dir;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.TryGetComponent(out HiderController _seekerController))
        {
            var clientID = _seekerController.GetComponent<NetworkObject>().OwnerClientId;
            HitPlayerServerRpc(clientID);
        }


        DespawnOnNetworkServerRpc();
    }

    #region ServerRpc

    [ServerRpc(RequireOwnership = false)]
    private void DespawnOnNetworkServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void HitPlayerServerRpc(ulong seekerController)
    {
        NetworkManager.Singleton.ConnectedClients[seekerController].PlayerObject.GetComponent<HiderController>()
            .HitPlayer();
    }

    #endregion
}