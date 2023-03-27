//using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Photon.Pun;

public class LobbyPlayerEntry : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text _playerNameText;

    [SerializeField] private Image _playerColorImage;
    [SerializeField] private Button _playerReadyButton;
    [SerializeField] private Image _playerReadyImage;

    private int _ownerId;
    private bool _isPlayerReady;

    public void OnEnable()
    {
        PlayerNumbering.OnPlayerNumberingChanged += OnPlayerNumberingChanged;
    }

    public void Start()
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != _ownerId)
        {
            _playerReadyButton.gameObject.SetActive(false);
        }
        else
        {
            Hashtable initialProps = new Hashtable() {{Constants.PLAYER_READY, _isPlayerReady}};
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
            PhotonNetwork.LocalPlayer.SetScore(0);

            _playerReadyButton.onClick.AddListener(() =>
            {
                _isPlayerReady = !_isPlayerReady;
                SetPlayerReady(_isPlayerReady);

                Hashtable props = new Hashtable() {{Constants.PLAYER_READY, _isPlayerReady}};
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                if (PhotonNetwork.IsMasterClient)
                {
                    FindObjectOfType<LobbyManager>().LocalPlayerPropertiesUpdated();
                }
            });
        }
    }

    public void OnDisable()
    {
        PlayerNumbering.OnPlayerNumberingChanged -= OnPlayerNumberingChanged;
    }

    public void Initialize(int playerId, string playerName)
    {
        _ownerId = playerId;
        _playerNameText.text = playerName;
    }

    private void OnPlayerNumberingChanged()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == _ownerId)
            {
                _playerColorImage.color = Constants.GetColor(p.GetPlayerNumber());
            }
        }
    }

    public void SetPlayerReady(bool playerReady)
    {
        _playerReadyButton.GetComponentInChildren<Text>().text = playerReady ? "Ready!" : "Ready?";
        _playerReadyImage.enabled = playerReady;
    }
}
