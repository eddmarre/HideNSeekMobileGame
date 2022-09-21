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
    private GameObject _myPlayer;

    private void Start()
    {
        StartCoroutine(DestroyAfterSomeTime(5f));
    }

    private IEnumerator DestroyAfterSomeTime(float time)
    {
        yield return new WaitForSeconds(time);
        DespawnOnNetwork();
    }

    private void DespawnOnNetwork()
    {
        if (IsServer)
        {
            Destroy(gameObject);
        }
        
        if (IsClient && IsOwner)
        {
            DespawnOnNetworkServerRpc();
        }

    }

    [ServerRpc]
    private void DespawnOnNetworkServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
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

    public void SetPlayer(GameObject player)
    {
        _myPlayer = player;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // if (!IsOwner) return;
        if (collision.gameObject.Equals(_myPlayer)) return;

        Debug.Log(collision.transform.name);
        DespawnOnNetwork();
    }
}