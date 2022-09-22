using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HideNSeekGameManager : NetworkBehaviour
{
    [SerializeField] private int _numberOfPlayersInLobby = 1;
    [SerializeField] private Button _seekerButton;
    [SerializeField] private Button _hiderButton;
    [SerializeField] private Button _startGameButton;
    [SerializeField] private GameContextUI _gameContextUI;
    [SerializeField] private float _gamePlayTime = 100f;

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
            _startGameButton.gameObject.SetActive(true);
            //  _startGameButton.interactable = false;
            _startGameButton.onClick.AddListener(OnGameStarted);
        }

        if (!IsServer) return;
        _netIsSeekerAvailable.Value = true;
        _netIsHiderAvailable.Value = true;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Singleton_OnClientDisconnectCallback;
        ChooseCharacter.OnChooseSeeker += ChooseCharacter_OnChooseSeeker;
        ChooseCharacter.OnChooseHider += ChooseCharacter_OnChooseHider;
        HiderController.OnDeath += HiderController_OnDeath;
        HiderController.OnRevive += HiderController_OnRevive;
        _gameContextUI.OnGameOver += GameContextUI_OnGameOver;
        _hostClientID = OwnerClientId;
    }

    private void FixedUpdate()
    {

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
        
        
        if (IsServer && _hasGameStarted)
        {
            if (_hidersAlive.Count == 0)
            {
                GameContextUI_OnGameOver();
            }
        }


        if (!IsServer || _hasGameStarted) return;
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

    #endregion

    #region Events

    private void ChooseCharacter_OnChooseSeeker(ulong clientID)
    {
        _netIsSeekerAvailable.Value = false;
        // _seekerButton.interactable = _netIsSeekerAvailable.Value;
        _playerControllers.Add(clientID,
            NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<PlayerController>());
        EnableOrDisableSeekerButtonClientRpc();
    }

    private void ChooseCharacter_OnChooseHider(ulong clientID)
    {
        _netNumberOfHiders.Value += 1;

        if (_netNumberOfHiders.Value == 4)
        {
            _netIsHiderAvailable.Value = false;
        }
        else
        {
            _netIsHiderAvailable.Value = true;
        }

        // _hiderButton.interactable = _netIsHiderAvailable.Value;

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
            if (_playerControllers[connectedClientID].TryGetComponent(out HiderController _hiderController))
            {
                _netNumberOfHiders.Value -= 1;
                _netIsHiderAvailable.Value = true;
                EnableOrDisableHiderButtonClientRpc();
            }

            if (_playerControllers[connectedClientID].TryGetComponent(out SeekerController _seekerController))
            {
                _netIsSeekerAvailable.Value = true;
                EnableOrDisableSeekerButtonClientRpc();
            }

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
        _hidersDead.Add(clientID,_hidersAlive[clientID]);
        _hidersAlive.Remove(clientID);
    }

    private void HiderController_OnRevive(ulong clientID)
    {
        _hidersAlive.Add(clientID,_hidersDead[clientID]);
        _hidersDead.Remove(clientID);
    }

    #endregion

    #region Method

    private void OnGameStarted()
    {
        // if(_numberOfPlayersInLobby!=5) return;
        _gameContextUI.SetStartTime(_gamePlayTime);
        _gameContextUI.SetHasGameStarted(true);
        _hasGameStarted = true;
        InteractSpawner.Instance.SpawnObjects();
        _startGameButton.gameObject.SetActive(false);

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var clientPlayerController = client.Value.PlayerObject.GetComponent<PlayerController>();
            clientPlayerController.transform.position = new Vector3(0f, 10f, 0f);

            if (clientPlayerController.transform.TryGetComponent(out SeekerController _seekerController))
            {
                if (IsServer)
                    _seekerController.SetShowObjectiveText("Go find all the hiders");
                if (IsClient)
                {
                    ShowTextForClientsServerRpc(client.Key, 0, "Go find all the hiders");
                }
            }

            if (clientPlayerController.transform.TryGetComponent(out HiderController _hiderController))
            {
                _hidersAlive.Add(client.Key, _hiderController);
                if (IsServer)
                    _hiderController.SetShowObjectiveText("Hide from the dragon");
                if (IsClient)
                {
                    ShowTextForClientsServerRpc(client.Key, 1, "Hide from the dragon");
                }
            }
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
    private void SendPlayerBackToLobbyClientRpc(ClientRpcParams rpcParams = default)
    {
        if (IsHost) return;
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }


    [ClientRpc]
    private void EnableOrDisableHiderButtonClientRpc(ClientRpcParams rpcParams = default)
    {
        // if (IsHost) return;
        _hiderButton.interactable = _netIsHiderAvailable.Value;
    }

    [ClientRpc]
    private void EnableOrDisableSeekerButtonClientRpc(ClientRpcParams rpcParams = default)
    {
        // if (IsHost) return;
        _seekerButton.interactable = _netIsSeekerAvailable.Value;
    }

    #endregion
}