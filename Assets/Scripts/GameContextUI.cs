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
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _playerCountText;
    [SerializeField] private Button _exitGame;

    private NetworkVariable<float> _currentGameTime = new NetworkVariable<float>();

    private float _startTime = 1f;

    private bool _gameStarted;
    public event Action OnGameOver;

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


        _timerText.text = _startTime.ToString(".0");
    }

    private void Update()
    {
        if (IsHost && _gameStarted)
        {
            _startTime -= Time.deltaTime;
            _currentGameTime.Value = _startTime;
        }

        if (IsServer)
        {
            if (_startTime <= 0f)
            {
                _currentGameTime.Value = 0f;
                OnGameOver?.Invoke();
            }

            _timerText.text = _currentGameTime.Value.ToString(".0");
            UpdateGameTimeClientRpc(_currentGameTime.Value);


            //game over mechanic here
        }
    }


    public void SetPlayerCount(int numberOfPlayers)
    {
        UpdateGamePlayerCountClientRpc(numberOfPlayers);
    }

    public void SetStartTime(float time)
    {
        _startTime = time;
        _currentGameTime.Value = _startTime;
    }

    public void SetHasGameStarted(bool value)
    {
        _gameStarted = value;
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