using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENetCsharp;
using System;
using UnityEditor;

public class Client : MonoBehaviour
{
    public GameObject PlayerPrefab;
    public GameObject EnemyPrefab;

    private Dictionary<uint, Transform> _players;
    private Transform _playerTransform;
    private IHostClient _hostClient;
    private PlayerTransformProtocol _playerTransformProtocol;

    void Start()
    {
        _players = new Dictionary<uint, Transform>();
        Application.targetFrameRate = 30;

        InitEnet();
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += (obj) =>
        {
            if (obj == PlayModeStateChange.ExitingPlayMode)
            {
                DestroyEnet();
            }
        };
#endif
    }

    private void InitEnet()
    {
        _playerTransformProtocol = new PlayerTransformProtocol();
        _playerTransformProtocol.Position = Vector3.zero;

        ClientOptions clientOptions = new ClientOptions();
        clientOptions.IP = "127.0.0.1";
        clientOptions.Port = 6005;
        _hostClient = HostCreationFactory.CreateENetClientHost(clientOptions);
        _hostClient.Connected += Connected;
        _hostClient.Disconnected += Disconnected;
        _hostClient.ProtocolReceived += ProtocolReceived;

        _hostClient.RegisterProtocolType(typeof(PlayerConnectedProtocol));
        _hostClient.RegisterProtocolType(typeof(PlayerDisconnectedProtocol));
        _hostClient.RegisterProtocolType(typeof(PlayerTransformProtocol));
        Debug.Log("Enet initialized");

        _hostClient.StartClient();
        Debug.Log($"connecting {clientOptions.IP}:{clientOptions.Port}..");
    }

    private void DestroyEnet()
    {
        Debug.Log("Destroy client..");
        if (_hostClient != null)
        {
            _hostClient.Disconnect(0);
            _hostClient.Connected -= Connected;
            _hostClient.Disconnected -= Disconnected;
            _hostClient.ProtocolReceived -= ProtocolReceived;
        }
        _hostClient?.Destroy();
    }

    private void ProtocolReceived(IProtocol protocol)
    {
        if (protocol is PlayerConnectedProtocol playerConnectedProtocol)
        {
            OnConnectedPlayer(playerConnectedProtocol.PlayerIdProtocol);
        }
        else if (protocol is PlayerDisconnectedProtocol playerDisconnectedProtocol)
        {
            OnDisconnectedPlayer(playerDisconnectedProtocol.PlayerIdProtocol);
        }
        else if (protocol is PlayerTransformProtocol playerTransformProtocol)
        {
            UpdatePosition(playerTransformProtocol.PlayerIdProtocol.PlayerId, playerTransformProtocol.Position);
        }
    }

    private void UpdatePosition(uint playerId, Vector3 position)
    {
        if (_players.ContainsKey(playerId))
        {
            _players[playerId].position = position;
        }
    }

    private void OnDisconnectedPlayer(PlayerIdProtocol playerIdProtocol)
    {
        Debug.Log($"Disconnected player {playerIdProtocol.PlayerId}..");
        if (_players.ContainsKey(playerIdProtocol.PlayerId))
        {
            Transform transform = _players[playerIdProtocol.PlayerId];
            _players.Remove(playerIdProtocol.PlayerId);
            Destroy(transform.gameObject);
        }
    }

    private void OnConnectedPlayer(PlayerIdProtocol playerIdProtocol)
    {
        Debug.Log($"Connected player {playerIdProtocol.PlayerId}..");
        if (_hostClient.GetClient().NetworkId == playerIdProtocol.PlayerId)
        {
            return;
        }

        Transform transform = Instantiate(EnemyPrefab).transform;
        transform.position = Vector3.zero;
        _players.Add(playerIdProtocol.PlayerId, transform);
    }

    private void Disconnected()
    {
        Debug.Log("Disconnected..");
        foreach (var item in _players)
        {
            GameObject.Destroy(item.Value.gameObject);
        }
        GameObject.Destroy(_playerTransform.gameObject);
        _players.Clear();
    }

    private void Connected()
    {
        Debug.Log("Connected..");
        _playerTransform = GameObject.Instantiate(PlayerPrefab).transform;
        _playerTransform.position = Vector3.zero;
        _playerTransformProtocol.PlayerIdProtocol.PlayerId = _hostClient.GetClient().NetworkId;
    }

    void Update()
    {
        //networking update
        _hostClient.Update();

        if (_playerTransform)
            SendPosition();
    }

    private void SendPosition()
    {
        _playerTransformProtocol.Position = _playerTransform.position;
        _hostClient?.SendProtocol(_playerTransformProtocol);
    }
}
