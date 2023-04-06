using Mirror;
using TMPro;
using UnityEngine;

public class MyNetworkPlayer : NetworkBehaviour
{
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private Renderer displayColorRenderer;

    [SyncVar(hook = nameof(HandleDisplayNameUpdate))] 
    [SerializeField]
    private string displayName = "Missing Name";
    
    [SyncVar(hook = nameof(HandleDisplayColourUpdate))] 
    [SerializeField]
    private Color displayColor = Color.black;

    #region Server
    [Server]
    public void setDisplayName(string newDisplayName)
    {
        displayName = newDisplayName;
    }
    [Server]
    public void setDisplayColor(Color newDisplayColor)
    {
        displayColor = newDisplayColor;
    }

    [Command]
    private void CmdSetDisplayName(string newDisplayName)
    { 
        // server authority to limit displayName into 2-20 letter length
        if(newDisplayName.Length<2||newDisplayName.Length>20)
        {
            return;
        }
        RpcDisplayNewName(newDisplayName);
        setDisplayName(newDisplayName);
    }

    #endregion

    #region Client

    private void HandleDisplayColourUpdate(Color oldColor, Color newColor)
    {
        displayColorRenderer.material.SetColor("_BaseColor",newColor);
        // displayColorRenderer.material.color = newColor;
    }
    private void HandleDisplayNameUpdate(string oldName, string newName)
    {
        displayNameText.text = newName;
    }

    [ContextMenu("Set this Name")]
    private void SetThisName()
    {
        CmdSetDisplayName("My New Name");
    }

    [ClientRpc]
    private void RpcDisplayNewName(string newDisplayName)
    {
        Debug.Log(newDisplayName);
    }

    #endregion
    
}