using ExitGames.Client.Photon;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Login Panel")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private InputField _playerNameInput;


    [Header("Selection Panel")]
    [SerializeField] private GameObject _selectionPanel;


    [Header("Create Room Panel")]
    [SerializeField] private GameObject _createRoomPanel;

    [SerializeField] private InputField _roomNameInputField;


    [Header("Join Random Room Panel")]
    [SerializeField] private GameObject _joinRandomRoomPanel;


    [Header("Room List Panel")]
    [SerializeField] private GameObject _roomListPanel;

    [SerializeField] private GameObject _roomListContent;
    [SerializeField] private GameObject _roomListEntryPrefab;


    [Header("Inside Room Panel")]
    [SerializeField] private GameObject _insideRoomPanel;

    [SerializeField] private Button _startGameButton;
    [SerializeField] private GameObject _playerListEntryPrefab;

    [Header("Settings")]
    [SerializeField] private string _gameSceneName;
    [SerializeField] private int _maxPlayerCount;

    private Dictionary<string, RoomInfo> _cachedRoomsList;
    private Dictionary<string, GameObject> _roomListEntries;
    private Dictionary<int, GameObject> _playerListEntries;

    public void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        _cachedRoomsList = new Dictionary<string, RoomInfo>();
        _roomListEntries = new Dictionary<string, GameObject>();
        
        _playerNameInput.text = "Player " + Random.Range(0, 10000);
    }

    public override void OnConnectedToMaster()
    {
        this.SetActivePanel(_selectionPanel.name);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        ClearRoomListView();

        UpdateCachedRoomList(roomList);
        UpdateRoomListView();
    }

    public override void OnJoinedLobby()
    {
        // whenever this joins a new lobby, clear any previous room lists
        _cachedRoomsList.Clear();
        ClearRoomListView();
    }

    // note: when a client joins / creates a room, OnLeftLobby does not get called, even if the client was in a lobby before
    public override void OnLeftLobby()
    {
        _cachedRoomsList.Clear();
        ClearRoomListView();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetActivePanel(_selectionPanel.name);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetActivePanel(_selectionPanel.name);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        string roomName = "Room " + Random.Range(1000, 10000);

        RoomOptions options = new RoomOptions {MaxPlayers = 2};

        PhotonNetwork.CreateRoom(roomName, options, null);
    }

    public override void OnJoinedRoom()
    {
        // joining (or entering) a room invalidates any cached lobby room list (even if LeaveLobby was not called due to just joining a room)
        _cachedRoomsList.Clear();


        SetActivePanel(_insideRoomPanel.name);

        if (_playerListEntries == null)
        {
            _playerListEntries = new Dictionary<int, GameObject>();
        }

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            GameObject entry = Instantiate(_playerListEntryPrefab);
            entry.transform.SetParent(_insideRoomPanel.transform);
            entry.transform.localScale = Vector3.one;
            
            entry.GetComponent<LobbyPlayerEntry>().Initialize(p.ActorNumber, p.NickName);

            object isPlayerReady;
            if (p.CustomProperties.TryGetValue(Constants.PLAYER_READY, out isPlayerReady))
            {
                entry.GetComponent<LobbyPlayerEntry>().SetPlayerReady((bool) isPlayerReady);
            }

            _playerListEntries.Add(p.ActorNumber, entry);
        }

        _startGameButton.gameObject.SetActive(CheckPlayersReady());

        Hashtable props = new Hashtable
        {
            {Constants.PLAYER_LOADED_LEVEL, false}
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnLeftRoom()
    {
        SetActivePanel(_selectionPanel.name);

        foreach (GameObject entry in _playerListEntries.Values)
        {
            Destroy(entry.gameObject);
        }

        _playerListEntries.Clear();
        _playerListEntries = null;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        GameObject entry = Instantiate(_playerListEntryPrefab);
        entry.transform.SetParent(_insideRoomPanel.transform);
        entry.transform.localScale = Vector3.one;

        entry.GetComponent<LobbyPlayerEntry>().Initialize(newPlayer.ActorNumber, newPlayer.NickName);

        _playerListEntries.Add(newPlayer.ActorNumber, entry);

        _startGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Destroy(_playerListEntries[otherPlayer.ActorNumber].gameObject);
        _playerListEntries.Remove(otherPlayer.ActorNumber);

        _startGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        {
            _startGameButton.gameObject.SetActive(CheckPlayersReady());
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (_playerListEntries == null)
        {
            _playerListEntries = new Dictionary<int, GameObject>();
        }

        GameObject entry;
        if (_playerListEntries.TryGetValue(targetPlayer.ActorNumber, out entry))
        {
            object isPlayerReady;

            if (changedProps.TryGetValue(Constants.PLAYER_READY, out isPlayerReady))
            {
                entry.GetComponent<LobbyPlayerEntry>().SetPlayerReady((bool) isPlayerReady);
            }
        }

        _startGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    public void OnBackButtonClicked()
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        SetActivePanel(_selectionPanel.name);
    }

    public void OnCreateRoomViewButtonClicked(string view)
    {
        SetActivePanel(view);
    }

    public void OnCreateRoomButtonClicked()
    {
        string roomName = _roomNameInputField.text;
        roomName = (roomName.Equals(string.Empty)) ? "Room " + Random.Range(1000, 10000) : roomName;

        byte maxPlayers;
        maxPlayers = (byte) Mathf.Clamp(_maxPlayerCount, 2, 8);

        RoomOptions options = new RoomOptions {MaxPlayers = maxPlayers, PlayerTtl = 10000 };

        PhotonNetwork.CreateRoom(roomName, options, null);
    }

    public void OnJoinRandomRoomButtonClicked()
    {
        SetActivePanel(_joinRandomRoomPanel.name);

        PhotonNetwork.JoinRandomRoom();
    }

    public void OnLeaveGameButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void OnLoginButtonClicked()
    {
        string playerName = _playerNameInput.text;

        if (!playerName.Equals(""))
        {
            PhotonNetwork.LocalPlayer.NickName = playerName;
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.LogError("Player Name is invalid.");
        }
    }

    public void OnRoomListButtonClicked()
    {
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        SetActivePanel(_roomListPanel.name);
    }

    public void OnStartGameButtonClicked()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        PhotonNetwork.LoadLevel(_gameSceneName);
    }

    private bool CheckPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isPlayerReady;
            
            if (p.CustomProperties.TryGetValue(Constants.PLAYER_READY, out isPlayerReady))
            {
                if (!(bool) isPlayerReady)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }
    
    private void ClearRoomListView()
    {
        foreach (GameObject entry in _roomListEntries.Values)
        {
            Destroy(entry.gameObject);
        }

        _roomListEntries.Clear();
    }

    public void LocalPlayerPropertiesUpdated()
    {
        _startGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    private void SetActivePanel(string activePanel)
    {
        _loginPanel.SetActive(activePanel.Equals(_loginPanel.name));
        _selectionPanel.SetActive(activePanel.Equals(_selectionPanel.name));
        _createRoomPanel.SetActive(activePanel.Equals(_createRoomPanel.name));
        _joinRandomRoomPanel.SetActive(activePanel.Equals(_joinRandomRoomPanel.name));
        _roomListPanel.SetActive(activePanel.Equals(_roomListPanel.name));
        _insideRoomPanel.SetActive(activePanel.Equals(_insideRoomPanel.name));
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // Remove room from cached room list if it got closed, became invisible or was marked as removed
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                if (_cachedRoomsList.ContainsKey(info.Name))
                {
                    _cachedRoomsList.Remove(info.Name);
                }

                continue;
            }

            // Update cached room info
            if (_cachedRoomsList.ContainsKey(info.Name))
            {
                _cachedRoomsList[info.Name] = info;
            }
            // Add new room info to cache
            else
            {
                _cachedRoomsList.Add(info.Name, info);
            }
        }
    }

    private void UpdateRoomListView()
    {
        foreach (RoomInfo info in _cachedRoomsList.Values)
        {
            GameObject entry = Instantiate(_roomListEntryPrefab);
            entry.transform.SetParent(_roomListContent.transform);
            entry.transform.localScale = Vector3.one;

            entry.GetComponent<LobbyRoomEntry>().Initialize(info.Name, (byte)info.PlayerCount, info.MaxPlayers);

            _roomListEntries.Add(info.Name, entry);
        }
    }
}
