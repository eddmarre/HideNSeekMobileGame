using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class HideNSeekGameManager : NetworkBehaviour
{
    [SerializeField] private int _numberOfPlayersInLobby = 1;
    [SerializeField] private Button _seekerButton;
    [SerializeField] private Button _hiderButton;
    [SerializeField] private Button _startGameButton;
    [SerializeField] private GameContextUI _gameContextUI;
    [SerializeField] private float _gamePlayTime = 100f;
    [SerializeField] private bool _isTesting = true;
    [SerializeField] private Vector3[] _spawnPositions;

    private NetworkVariable<bool> _netIsSeekerAvailable = new NetworkVariable<bool>();
    private NetworkVariable<bool> _netIsHiderAvailable = new NetworkVariable<bool>();
    private NetworkVariable<int> _netNumberOfHiders = new NetworkVariable<int>();

    private bool _hasGameStarted;
    private ulong _hostClientID;

    private Dictionary<ulong, PlayerController> _playerControllers;
    private Dictionary<ulong, HiderController> _hidersAlive;
    private Dictionary<ulong, HiderController> _hidersDead;


    #region Monobehavior

    private void Awake()
    {
        _playerControllers = new Dictionary<ulong, PlayerController>();
        _hidersAlive = new Dictionary<ulong, HiderController>();
        _hidersDead = new Dictionary<ulong, HiderController>();
    }

    private void Start()
    {
        if (IsHost)
        {
            EnableStartGameButton();
        }

        if (!IsServer) return;

        SetIsSeekerAvailable(true);
        SetIsHiderAvailable(true);

        SubscribeToEvents();

        _hostClientID = OwnerClientId;
    }


    private void FixedUpdate()
    {
#if UNITY_EDITOR
        if (IsServer)
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                foreach (var player in _playerControllers)
                {
                    Debug.Log($"Players {player.Key} {player.Value}");
                }

                foreach (var player in _hidersAlive)
                {
                    Debug.Log($"Alive {player.Key} {player.Value}");
                }

                foreach (var player in _hidersDead)
                {
                    Debug.Log($"Dead {player.Key} {player.Value}");
                }
            }
        }
#endif

        if (IsServer && _hasGameStarted)
        {
            GameOverIfAllPlayersDie();
        }

        if (!IsServer || _hasGameStarted) return;

        CheckIfMultipleSeekers();

        if (!_netIsSeekerAvailable.Value)
        {
            EnableOrDisableSeekerButtonClientRpc();
        }

        if (!_netIsHiderAvailable.Value)
        {
            EnableOrDisableHiderButtonClientRpc();
        }

        if (_numberOfPlayersInLobby == 5)
        {
            _startGameButton.interactable = true;
        }
    }


    public override void OnDestroy()
    {
        base.OnDestroy();
        TryUnSubscribeToEvents();
    }

    #endregion

    #region Events

    private void OnGameStarted()
    {
        if (!_isTesting && _numberOfPlayersInLobby != 5) return;
        _gameContextUI.SetStartTime(_gamePlayTime);
        _gameContextUI.SetHasGameStarted(true);
        _hasGameStarted = true;
        InteractSpawner.Instance.SpawnObjects();
        _startGameButton.gameObject.SetActive(false);

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var randomSpawnPoint = Random.Range(0, _spawnPositions.Length);
            
            var clientPlayerController = client.Value.PlayerObject.GetComponent<PlayerController>();
            clientPlayerController.transform.position = _spawnPositions[randomSpawnPoint];

            DisplayGameStartMessageToSeeker(clientPlayerController, client);

            DisplayGameStartMessageToHiders(clientPlayerController, client);
        }
    }

    private void ChooseCharacter_OnChooseSeeker(ulong clientID)
    {
        SetIsSeekerAvailable(false);
        _playerControllers.Add(clientID,
            NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<PlayerController>());
        EnableOrDisableSeekerButtonClientRpc();
    }

    private void ChooseCharacter_OnChooseHider(ulong clientID)
    {
        _netNumberOfHiders.Value += 1;

        if (_netNumberOfHiders.Value == 4)
        {
            SetIsHiderAvailable(false);
        }
        else
        {
            SetIsHiderAvailable(true);
        }

        EnableOrDisableHiderButtonClientRpc();

        _playerControllers.Add(clientID,
            NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<PlayerController>());
    }

    private void NetworkManager_Singleton_OnClientConnectedCallback(ulong connectedClientID)
    {
        ++_numberOfPlayersInLobby;
        try
        {
            _gameContextUI.SetPlayerCount(_numberOfPlayersInLobby);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

        if (!_netIsSeekerAvailable.Value)
        {
            EnableOrDisableSeekerButtonClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] {connectedClientID}
                }
            });
        }

        if (!_netIsHiderAvailable.Value)
        {
            EnableOrDisableHiderButtonClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] {connectedClientID}
                }
            });
        }
    }

    private void NetworkManager_Singleton_OnClientDisconnectCallback(ulong connectedClientID)
    {
        --_numberOfPlayersInLobby;
        try
        {
            _gameContextUI.SetPlayerCount(_numberOfPlayersInLobby);

            IfHiderDisconnectsMakeButtonActive(connectedClientID);

            IfSeekerDisconnectsMakeButtonActive(connectedClientID);

            _playerControllers.Remove(connectedClientID);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    private void GameContextUI_OnGameOver()
    {
        foreach (var playerController in _playerControllers)
        {
            playerController.Value.SetShowObjectiveTextClientRpc("GameOver", new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] {playerController.Key}
                }
            });
        }

        _gameContextUI.SetHasGameStarted(false);
    }

    private void HiderController_OnDeath(ulong clientID)
    {
        _hidersDead.Add(clientID, _hidersAlive[clientID]);
        _hidersAlive.Remove(clientID);
    }

    private void HiderController_OnRevive(ulong clientID)
    {
        _hidersAlive.Add(clientID, _hidersDead[clientID]);
        _hidersDead.Remove(clientID);
    }

    #endregion

    #region Method

    private void IfSeekerDisconnectsMakeButtonActive(ulong connectedClientID)
    {
        if (_playerControllers[connectedClientID].TryGetComponent(out SeekerController _seekerController))
        {
            SetIsSeekerAvailable(true);
            EnableOrDisableSeekerButtonClientRpc();
        }
    }

    private void IfHiderDisconnectsMakeButtonActive(ulong connectedClientID)
    {
        if (_playerControllers[connectedClientID].TryGetComponent(out HiderController _hiderController))
        {
            _netNumberOfHiders.Value -= 1;
            SetIsHiderAvailable(true);
            EnableOrDisableHiderButtonClientRpc();
        }
    }

    private void CheckIfMultipleSeekers()
    {
        var allSeekers = GameObject.FindGameObjectsWithTag("Dragon");
        if (allSeekers.Length > 1)
        {
            for (int i = 0; i < allSeekers.Length; i++)
            {
                if (allSeekers[i].GetComponent<NetworkObject>().OwnerClientId.Equals(_hostClientID))
                    continue;

                NetworkManager.Singleton.DisconnectClient(allSeekers[i]
                    .GetComponent<NetworkObject>()
                    .OwnerClientId);
            }
        }
    }

    private void GameOverIfAllPlayersDie()
    {
        if (_hidersAlive.Count == 0)
        {
            GameContextUI_OnGameOver();
        }
    }

    private void SetIsHiderAvailable(bool value)
    {
        _netIsHiderAvailable.Value = value;
    }

    private void SetIsSeekerAvailable(bool value)
    {
        _netIsSeekerAvailable.Value = value;
    }

    private void EnableStartGameButton()
    {
        _startGameButton.gameObject.SetActive(true);
        _startGameButton.onClick.AddListener(OnGameStarted);
    }

    private void SubscribeToEvents()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Singleton_OnClientDisconnectCallback;
        ChooseCharacter.OnChooseSeeker += ChooseCharacter_OnChooseSeeker;
        ChooseCharacter.OnChooseHider += ChooseCharacter_OnChooseHider;
        HiderController.OnDeath += HiderController_OnDeath;
        HiderController.OnRevive += HiderController_OnRevive;
        _gameContextUI.OnGameOver += GameContextUI_OnGameOver;
    }

    private void TryUnSubscribeToEvents()
    {
        ChooseCharacter.OnChooseSeeker -= ChooseCharacter_OnChooseSeeker;
        ChooseCharacter.OnChooseHider -= ChooseCharacter_OnChooseHider;
        HiderController.OnDeath -= HiderController_OnDeath;
        HiderController.OnRevive -= HiderController_OnRevive;
        _gameContextUI.OnGameOver -= GameContextUI_OnGameOver;
        try
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Singleton_OnClientDisconnectCallback;
        }
        catch (Exception e)
        {
            Debug.Log("no connected clients to unregister");
        }
    }


    private void DisplayGameStartMessageToHiders(PlayerController clientPlayerController,
        KeyValuePair<ulong, NetworkClient> client)
    {
        if (clientPlayerController.transform.TryGetComponent(out HiderController _hiderController))
        {
            _hidersAlive.Add(client.Key, _hiderController);

            ShowTextForClientsServerRpc(client.Key, 1, "Hide from the dragon");
        }
    }

    private void DisplayGameStartMessageToSeeker(PlayerController clientPlayerController,
        KeyValuePair<ulong, NetworkClient> client)
    {
        if (clientPlayerController.transform.TryGetComponent(out SeekerController _seekerController))
        {
            ShowTextForClientsServerRpc(client.Key, 0, "Go find all the hiders");
        }
    }

    #endregion

    #region ServerRpc

    [ServerRpc(RequireOwnership = false)]
    void ShowTextForClientsServerRpc(ulong clientID, int controller, string message)
    {
        if (controller == 0)
        {
            if (_playerControllers[clientID].TryGetComponent(out SeekerController _dragonController))
            {
                _dragonController.SetShowObjectiveTextClientRpc(message, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] {clientID}
                    }
                });
            }
        }
        else
        {
            if (_playerControllers[clientID].TryGetComponent(out HiderController _seekerController))
            {
                _seekerController.SetShowObjectiveTextClientRpc(message, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] {clientID}
                    }
                });
            }
        }
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    private void EnableOrDisableHiderButtonClientRpc(ClientRpcParams rpcParams = default)
    {
        _hiderButton.interactable = _netIsHiderAvailable.Value;
    }

    [ClientRpc]
    private void EnableOrDisableSeekerButtonClientRpc(ClientRpcParams rpcParams = default)
    {
        _seekerButton.interactable = _netIsSeekerAvailable.Value;
    }

    #endregion
}