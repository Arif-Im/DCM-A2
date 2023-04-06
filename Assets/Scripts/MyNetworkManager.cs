using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        // Debug.Log("OnClientConnect");
        Debug.Log("You have connected to the server");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        // Debug.Log("OnServerAddPlayer ");
        Debug.Log($"Current Number of Players {numPlayers} ");

        MyNetworkPlayer player = conn.identity.GetComponent<MyNetworkPlayer>();

        player.setDisplayName($"Player {numPlayers}");

        Color displayColor = new Color(Random.Range(0, 1f), 
            Random.Range(0f, 1f),
            Random.Range(0f, 1f));
        
        player.setDisplayColor(displayColor);
        
        
    }
}
