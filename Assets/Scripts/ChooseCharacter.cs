using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class ChooseCharacter : NetworkBehaviour
{
    [SerializeField] private GameObject _dragonPlayer;
    [SerializeField] private GameObject _hiderPlayer;
    [SerializeField] private Button _dragonButton;
    [SerializeField] private Button _hiderButton;
    [SerializeField] private Transform _spawnLocation;

    public static event Action<ulong> OnChooseSeeker;
    public static event Action<ulong> OnChooseHider;

    private void Start()
    {
        _dragonButton.onClick.AddListener(SpawnPlayerAsDragon);
        _hiderButton.onClick.AddListener(SpawnPlayerAsHider);
    }

    #region Methods

    private void SpawnPlayerAsDragon()
    {
        SpawnPlayerAsDragonServerRpc(NetworkManager.Singleton.LocalClientId);

        gameObject.SetActive(false);
    }

    private void SpawnPlayerAsHider()
    {
        SpawnPlayerAsHiderServerRpc(NetworkManager.Singleton.LocalClientId);

        gameObject.SetActive(false);
    }

    private Vector3 GenerateRandomPosition()
    {
        return new Vector3(UnityEngine.Random.Range(-5, 6), 0f, UnityEngine.Random.Range(-5, 6));
    }

    #endregion

    #region ServerRPC

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerAsDragonServerRpc(ulong clientId)
    {
        var dragon = Instantiate(_dragonPlayer, _spawnLocation.position + GenerateRandomPosition(),
            Quaternion.identity);
        var netDragon = dragon.GetComponent<NetworkObject>();
        netDragon.SpawnAsPlayerObject(clientId, true);

        OnChooseSeeker?.Invoke(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerAsHiderServerRpc(ulong clientId)
    {
        var dragon = Instantiate(_hiderPlayer, _spawnLocation.position + GenerateRandomPosition(), Quaternion.identity);
        var netDragon = dragon.GetComponent<NetworkObject>();
        netDragon.SpawnAsPlayerObject(clientId, true);

        OnChooseHider?.Invoke(clientId);
    }

    #endregion
}