using System.Collections;
using System.Collections.Generic;
using Cinemachine;
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

    public CinemachineTargetGroup cinemachineTargetGroup;
    public CinemachineVirtualCamera NoPlayerCinemachine;
    public CinemachineVirtualCamera InGameCinemachine;

    public override void Awake()
    {
        base.Awake();
        if (cinemachineTargetGroup == null)
        {
            cinemachineTargetGroup = FindObjectOfType<CinemachineTargetGroup>();
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        if (NoPlayerCinemachine)
            NoPlayerCinemachine.gameObject.SetActive(false);
        if (InGameCinemachine)
            InGameCinemachine.gameObject.SetActive(true);

        // Debug.Log("OnServerAddPlayer ");
        Debug.Log($"Current Number of Players {numPlayers} ");

        MyNetworkPlayer player = conn.identity.GetComponent<MyNetworkPlayer>();

        player.setDisplayName($"Player {numPlayers}");

        Color displayColor = new Color(Random.Range(0, 1f),
            Random.Range(0f, 1f),
            Random.Range(0f, 1f));

        player.setDisplayColor(displayColor);

        if (cinemachineTargetGroup == null)
        {
            cinemachineTargetGroup = FindObjectOfType<CinemachineTargetGroup>();
        }

        if (cinemachineTargetGroup)
        {
            cinemachineTargetGroup.AddMember(player.GetComponentInChildren<Animator>().transform, 1, 100);
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (NoPlayerCinemachine)
            NoPlayerCinemachine.gameObject.SetActive(true);
        if (InGameCinemachine)
            InGameCinemachine.gameObject.SetActive(false);
    }
}