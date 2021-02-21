using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ENetCsharp;
using UnityEditor;
using System;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    public GameObject TextPrefab;
    public Transform ContentParent;

    private IHostServer _hostServer;
    private List<AbstractClient> _clients;

    void Start()
    {
        _clients = new List<AbstractClient>();
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
        ServerOptions serverOptions = new ServerOptions();
        serverOptions.Port = 6005;
        serverOptions.MaxClient = 10;
        _hostServer = HostCreationFactory.CreateENetServerHost(serverOptions);
        _hostServer.Connected += Connected;
        _hostServer.Disconnected += Disconnected;
        _hostServer.ProtocolReceived += ProtocolReceived;

        _hostServer.RegisterProtocolType(typeof(PlayerConnectedProtocol));
        _hostServer.RegisterProtocolType(typeof(PlayerDisconnectedProtocol));
        _hostServer.RegisterProtocolType(typeof(PlayerTransformProtocol));
        CreateTextObject("Initialized enet.");

        _hostServer.StartServer();
        CreateTextObject($"Server started, port : {serverOptions.Port}");
    }

    private void DestroyEnet()
    {
        CreateTextObject("Destroyed enet.");
        if (_hostServer != null)
        {
            _hostServer.Connected -= Connected;
            _hostServer.Disconnected -= Disconnected;
            _hostServer.ProtocolReceived -= ProtocolReceived;
        }
        _hostServer?.Destroy();
    }

    private void ProtocolReceived(AbstractClient client, IProtocol protocol)
    {
        if (protocol is PlayerTransformProtocol playerTransformProtocol)
        {
            _hostServer.SendProtocolToAll(playerTransformProtocol);
        }
    }

    private void Disconnected(AbstractClient client)
    {
        SendDisconnectedProtocol(client);
        _clients.Remove(client);
    }

    private void Connected(AbstractClient client)
    {
        SendConnectedProtocol(client);
        _clients.Add(client);
    }

    private void SendConnectedProtocol(AbstractClient client)
    {
        CreateTextObject($"Player Connected : {client.NetworkId}");
        PlayerConnectedProtocol protocol = new PlayerConnectedProtocol();
        protocol.PlayerIdProtocol = new PlayerIdProtocol();
        protocol.PlayerIdProtocol.PlayerId = client.NetworkId;
        _hostServer.SendProtocolToAll(protocol);
        foreach (var item in _clients)
        {
            PlayerConnectedProtocol p2 = new PlayerConnectedProtocol();
            p2.PlayerIdProtocol = new PlayerIdProtocol();
            p2.PlayerIdProtocol.PlayerId = item.NetworkId;
            _hostServer.SendProtocolToPeer(client, p2);
        }
    }

    private void SendDisconnectedProtocol(AbstractClient client)
    {
        CreateTextObject($"Player Disconnected : {client.NetworkId}");
        PlayerDisconnectedProtocol protocol = new PlayerDisconnectedProtocol();
        protocol.PlayerIdProtocol = new PlayerIdProtocol();
        protocol.PlayerIdProtocol.PlayerId = client.NetworkId;
        _hostServer.SendProtocolToAll(protocol);
    }

    private void CreateTextObject(string message)
    {
        Text text = Instantiate(TextPrefab, ContentParent).GetComponent<Text>();
        text.text = message;
    }

    void Update()
    {
        _hostServer?.Update();
    }
}
