using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameContextUI : NetworkBehaviour
{
    [FormerlySerializedAs("_textMeshProUGUI")] [SerializeField]
    private TextMeshProUGUI _timerText;

    [SerializeField] private TextMeshProUGUI _playerCountText;
    [SerializeField] private float startTime = 300f;

    [SerializeField] private Button _exitGame;
    public bool _gameStarted;
    private NetworkVariable<float> _currentGameTime = new NetworkVariable<float>();

    public static GameContextUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (IsServer)
        {
            _exitGame.onClick.AddListener(() =>
            {
                // NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("Lobby");
            });
        }

        if (IsClient && !IsServer)
        {
            _exitGame.onClick.AddListener(() =>
            {
                // SceneManager.LoadScene("Lobby");
                DisconnectClientServerRpc(NetworkManager.Singleton.LocalClientId);
                SceneManager.LoadScene("Lobby");
                //NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            });
        }

        _timerText.text = startTime.ToString(".0");
    }

    [ServerRpc(RequireOwnership = false)]
    void DisconnectClientServerRpc(ulong clientID)
    {
        NetworkManager.Singleton.DisconnectClient(clientID);
        // GoBackToLobbyClientRpc(new ClientRpcParams
        //     {
        //         Send = new ClientRpcSendParams
        //         {
        //             TargetClientIds = new ulong[] {clientID}
        //         }
        //     }
        //     );
    }

    [ClientRpc]
    void GoBackToLobbyClientRpc(ClientRpcParams rpcParams = default)
    {
        SceneManager.LoadScene("Lobby");
    }

    private void Update()
    {
        if (IsHost && _gameStarted)
        {
            startTime -= Time.deltaTime;
            _currentGameTime.Value = startTime;
        }

        if (IsServer)
        {
            _timerText.text = _currentGameTime.Value.ToString(".0");
            UpdateGameTimeClientRpc(_currentGameTime.Value);
            //game over mechanic here
        }
    }


    [ClientRpc]
    private void UpdateGameTimeClientRpc(float gameTime)
    {
        _timerText.text = gameTime.ToString(".00");
    }

    public void SetPlayerCount(int numberOfPlayers)
    {
        UpdateGamePlayerCountClientRpc(numberOfPlayers);
    }

    [ClientRpc]
    private void UpdateGamePlayerCountClientRpc(int numberOfPlayers)
    {
        _playerCountText.text = $"{numberOfPlayers}/5";
    }
}