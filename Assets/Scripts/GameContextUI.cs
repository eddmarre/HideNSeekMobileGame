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


    private void Start()
    {
        _exitGame.onClick.AddListener(() =>
        {
            // NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            NetworkManager.Singleton.Shutdown();
            try
            {
                Destroy(FindObjectOfType<LobbyController>().gameObject);
                Destroy(FindObjectOfType<NetworkManager>().gameObject);
            }
            catch (Exception e)
            {
            }

            SceneManager.LoadScene("Lobby");
        });


        _timerText.text = startTime.ToString(".0");
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


    public void SetPlayerCount(int numberOfPlayers)
    {
        UpdateGamePlayerCountClientRpc(numberOfPlayers);
    }

    #region ClientRpc

    [ClientRpc]
    private void UpdateGamePlayerCountClientRpc(int numberOfPlayers)
    {
        _playerCountText.text = $"{numberOfPlayers}/5";
    }

    [ClientRpc]
    private void UpdateGameTimeClientRpc(float gameTime)
    {
        _timerText.text = gameTime.ToString(".00");
    }

    #endregion
}