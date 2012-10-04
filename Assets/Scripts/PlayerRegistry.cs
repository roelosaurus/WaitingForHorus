﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class PlayerRegistry : MonoBehaviour
{
    static PlayerRegistry instance;
    public static PlayerRegistry Instance
    {
        get { return instance; }
    }

    void OnNetworkInstantiate(NetworkMessageInfo info)
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static Dictionary<NetworkPlayer, PlayerInfo> For = new Dictionary<NetworkPlayer, PlayerInfo>();

    public static void RegisterCurrentPlayer(string username)
    {
        Instance.networkView.RPC("RegisterPlayer", RPCMode.All, Network.player, username);
    }

    [RPC]
    public void RegisterPlayer(NetworkPlayer player, string username)
    {
        //Debug.Log(player + " = " + username);
        var color = Color.white;
        if (For.ContainsKey(player)) For.Remove(player);
        For.Add(player, new PlayerInfo { Username = username, Color = color });
    }
    [RPC]
    public void RegisterPlayerFull(NetworkPlayer player, string username, float r, float g, float b, bool isSpectating)
    {
        //Debug.Log(player + " = " + username);
        try
        {
            For.Add(player, new PlayerInfo { Username = username, Color = new Color(r, g, b), Spectating = isSpectating });
        }
        catch (Exception ex)
        {
            Debug.Log("Tried to register player " + player + " but was already registered. Current username : " + For[player].Username + " | wanted username : " + username);
        }
    }

    [RPC]
    public void UnregisterPlayer(NetworkPlayer player)
    {
        For.Remove(player);
    }

    public void OnPlayerConnected(NetworkPlayer player)
    {
        foreach (var otherPlayer in For.Keys)
            if (otherPlayer != player)
            {
                networkView.RPC("RegisterPlayerFull", player, otherPlayer, For[otherPlayer].Username,
                                For[otherPlayer].Color.r, For[otherPlayer].Color.g, For[otherPlayer].Color.b,
                                For[otherPlayer].Spectating);

                if (For[otherPlayer].Spectating)
                    foreach (var p in FindObjectsOfType(typeof(PlayerScript)).Cast<PlayerScript>())
                        if (p.networkView.owner == otherPlayer)
                            p.GetComponent<HealthScript>().networkView.RPC("ToggleSpectate", player, true);
            }
    }
    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        //networkView.RPC("UnregisterPlayer", RPCMode.All, player);
    }
    public void OnDisconnectedFromServer(NetworkDisconnection info)
    {
        For.Clear();
    }

    public class PlayerInfo
    {
        public string Username;
        public Color Color;
        public bool Spectating;
    }
}
