using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMP_InputField joinCodeInput;

    private UnityTransport _transport;
    [SerializeField] private int MaxPlayers = 5;


    private Lobby _connectedLobby;
    private string _playerID;
    private const string joinKeyCode = "j";

    private static LobbyController _instance;

    private async void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }

        _instance = this;


        foreach (var button in buttons)
        {
            button.gameObject.SetActive(false);
        }

        try
        {
            await Authenticate();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

        foreach (var button in buttons)
        {
            button.gameObject.SetActive(true);
        }
    }


    private async Task Authenticate()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        _playerID = AuthenticationService.Instance.PlayerId;
    }

    private void Start()
    {
        //   _tempCamera.gameObject.SetActive(false);
        _transport = FindObjectOfType<UnityTransport>();
        buttons[0].onClick.AddListener(() => { CreateGame(); });
        buttons[1].onClick.AddListener(() => { });
        buttons[2].onClick.AddListener(() => { JoinGame(); });
        buttons[3].onClick.AddListener(async () =>
        {
            try
            {
                QuickJoinLobbyTest();
                if (_connectedLobby == null)
                    CreateLobbyTest();
            }
            catch (Exception e)
            {
            }


            foreach (var VARIABLE in buttons)
            {
                VARIABLE.gameObject.SetActive(false);
            }

            joinCodeInput.gameObject.SetActive(false);
            // _choosePlayerCanvas.gameObject.SetActive(true);
            DontDestroyOnLoad(gameObject);
        });


        NetworkManager.Singleton.OnServerStarted += () =>
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Multiplayer", LoadSceneMode.Single);
        };
    }


    private async void CreateGame()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log(joinCode);

        _transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort) allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key, allocation.ConnectionData);

        NetworkManager.Singleton.StartHost();
    }

    private async void JoinGame()
    {
        if (!string.IsNullOrEmpty(joinCodeInput.text))
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCodeInput.text);

            _transport.SetClientRelayData(allocation.RelayServer.IpV4, (ushort) allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);
        }

        NetworkManager.Singleton.StartClient();
    }

    private async Task<Lobby> QuickJoinLobby()
    {
        try
        {
            var lobby = await Lobbies.Instance.QuickJoinLobbyAsync();

            var allocation = await RelayService.Instance.JoinAllocationAsync(lobby.Data[joinKeyCode].Value);

            _transport.SetClientRelayData(allocation.RelayServer.IpV4, (ushort) allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
            return lobby;
        }
        catch (Exception e)
        {
            Debug.Log("couldn't find server");
            return null;
        }
    }

    private async void QuickJoinLobbyTest()
    {
        try
        {
            var lobby = await Lobbies.Instance.QuickJoinLobbyAsync();

            var allocation = await RelayService.Instance.JoinAllocationAsync(lobby.Data[joinKeyCode].Value);

            _transport.SetClientRelayData(allocation.RelayServer.IpV4, (ushort) allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
            _connectedLobby = lobby;
        }
        catch (Exception e)
        {
            Debug.Log("couldn't find server");
        }
    }

    private async Task<Lobby> CreateLobby()
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);

            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        joinKeyCode,
                        new DataObject(DataObject.VisibilityOptions.Public, joinCode)
                    }
                }
            };

            var lobby = await Lobbies.Instance.CreateLobbyAsync("Useless Lobby Name", MaxPlayers, options);

            StartCoroutine(HeartBeatLobbyCoroutine(lobby.Id, 15));

            _transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort) allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key, allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();
            return lobby;
        }
        catch (Exception e)
        {
            Debug.Log("couldn't Start Lobby");
            return null;
        }
    }

    private async void CreateLobbyTest()
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);

            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        joinKeyCode,
                        new DataObject(DataObject.VisibilityOptions.Public, joinCode)
                    }
                }
            };

            var lobby = await Lobbies.Instance.CreateLobbyAsync("Useless Lobby Name", MaxPlayers, options);

            StartCoroutine(HeartBeatLobbyCoroutine(lobby.Id, 15));

            _transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort) allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key, allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();
            _connectedLobby = lobby;
        }
        catch (Exception e)
        {
            Debug.Log("couldn't Start Lobby");
        }
    }

    private IEnumerator HeartBeatLobbyCoroutine(string lobbyId, float timeBetweenBeat)
    {
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return new WaitForSeconds(timeBetweenBeat);
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();

        if (_connectedLobby != null)
        {
            if (_connectedLobby.HostId == _playerID)
            {
                Lobbies.Instance.DeleteLobbyAsync(_connectedLobby.Id);
            }
            else
            {
                Lobbies.Instance.RemovePlayerAsync(_connectedLobby.Id, _playerID);
            }
        }
    }
}