using UnityEngine;
using ENetCsharp;

public struct PlayerTransformProtocol : IProtocol
{
    public PlayerIdProtocol PlayerIdProtocol;
    public Vector3 Position;
    public void Read(PacketOrganizer packet)
    {
        PlayerIdProtocol = new PlayerIdProtocol();
        PlayerIdProtocol.Read(packet);

        Position = Vector3.zero;
        Position.x = packet.ReadFloat();
        Position.y = packet.ReadFloat();
    }

    public void Write(PacketOrganizer packet)
    {
        PlayerIdProtocol.Write(packet);
        packet.Write(Position.x);
        packet.Write(Position.y);
    }
}

public struct PlayerIdProtocol : IProtocol
{
    public uint PlayerId;

    public void Read(PacketOrganizer packet)
    {
        PlayerId = (uint)packet.ReadLong();
    }

    public void Write(PacketOrganizer packet)
    {
        packet.Write((long)PlayerId);
    }
}

public struct PlayerConnectedProtocol : IProtocol
{
    public PlayerIdProtocol PlayerIdProtocol;

    public void Read(PacketOrganizer packet)
    {
        PlayerIdProtocol = new PlayerIdProtocol();
        PlayerIdProtocol.Read(packet);
    }

    public void Write(PacketOrganizer packet)
    {
        PlayerIdProtocol.Write(packet);
    }
}

public struct PlayerDisconnectedProtocol : IProtocol
{
    public PlayerIdProtocol PlayerIdProtocol;

    public void Read(PacketOrganizer packet)
    {
        PlayerIdProtocol = new PlayerIdProtocol();
        PlayerIdProtocol.Read(packet);
    }

    public void Write(PacketOrganizer packet)
    {
        PlayerIdProtocol.Write(packet);
    }
}