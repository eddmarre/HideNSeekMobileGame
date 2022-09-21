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
    [SerializeField] private Button _startGameButton;
    
    private NetworkVariable<bool> _isSeekerAvailable = new NetworkVariable<bool>();
    
    private bool _hasGameStarted;
    private ulong _hostClientID;
    
    private Dictionary<ulong, PlayerController> _playerControllers;
    
    private void Awake()
    {
        _playerControllers = new Dictionary<ulong, PlayerController>();
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
        _isSeekerAvailable.Value = true;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Singleton_OnClientDisconnectCallback;
        ChooseCharacter.OnChooseSeeker += ChooseCharacter_OnChooseSeeker;
        ChooseCharacter.OnChooseHider += ChooseCharacter_OnChooseHider;
        _hostClientID = OwnerClientId;
    }
    
    private void FixedUpdate()
    {
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
        
        if (!_isSeekerAvailable.Value)
        {
            DisableSeekerButtonClientRpc();
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
        // NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_Singleton_OnClientConnectedCallback;
        // NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Singleton_OnClientDisconnectCallback;
    }

    [ClientRpc]
    private void SendPlayerBackToLobbyClientRpc(ClientRpcParams rpcParams = default)
    {
        if (IsHost) return;
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }


    [ClientRpc]
    private void DisableSeekerButtonClientRpc(ClientRpcParams rpcParams = default)
    {
        if (IsHost) return;
        _seekerButton.interactable = _isSeekerAvailable.Value;
    }

    private void ChooseCharacter_OnChooseSeeker(ulong clientID)
    {
        _isSeekerAvailable.Value = false;
        _seekerButton.interactable = _isSeekerAvailable.Value;
        _playerControllers.Add(clientID,
            NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<PlayerController>());
        DisableSeekerButtonClientRpc();
    }

    private void ChooseCharacter_OnChooseHider(ulong clientID)
    {
        _playerControllers.Add(clientID,
            NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<PlayerController>());
    }


    private void NetworkManager_Singleton_OnClientConnectedCallback(ulong connectedClientID)
    {
        _numberOfPlayersInLobby++;
        GameContextUI.Instance.SetPlayerCount(_numberOfPlayersInLobby);
        if (!_isSeekerAvailable.Value)
        {
            DisableSeekerButtonClientRpc(new ClientRpcParams
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
        _numberOfPlayersInLobby--;
        GameContextUI.Instance.SetPlayerCount(_numberOfPlayersInLobby);
        _playerControllers.Remove(connectedClientID);
        SendPlayerBackToLobbyClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] {connectedClientID}
            }
        });
    }

    private void OnGameStarted()
    {
       // if(_numberOfPlayersInLobby!=5) return;
       GameContextUI.Instance._gameStarted = true;
        _hasGameStarted = true;
        InteractSpawner.Instance.SpawnObjects();
        _startGameButton.gameObject.SetActive(false);
        
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var clientPlayerController = client.Value.PlayerObject.GetComponent<PlayerController>();
            clientPlayerController.transform.position = new Vector3(0f, 10f, 0f);

            if (clientPlayerController.transform.TryGetComponent(out DragonController _dragonController))
            {
                if (IsServer)
                    _dragonController.SetShowObjectiveText("Go find all the hiders");
                if (IsClient)
                {
                    ShowTextForClientsServerRpc(client.Key, 0, "Go find all the hiders");
                }
            }

            if (clientPlayerController.transform.TryGetComponent(out SeekerController _seekerController))
            {
                if (IsServer)
                    _seekerController.SetShowObjectiveText("Hide from the dragon");
                if (IsClient)
                {
                    ShowTextForClientsServerRpc(client.Key, 1, "Hide from the dragon");
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ShowTextForClientsServerRpc(ulong clientID, int controller, string message)
    {
        if (controller == 0)
        {
            if (_playerControllers[clientID].TryGetComponent(out DragonController _dragonController))
            {
                _dragonController.SetShowObjectiveTextClientRpc(message,new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]{ clientID}
                    }
                });
            }
        }
        else
        {
            if (_playerControllers[clientID].TryGetComponent(out SeekerController _seekerController))
            {
                _seekerController.SetShowObjectiveTextClientRpc(message,new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]{ clientID}
                    }
                });
            }
        }
    }
}