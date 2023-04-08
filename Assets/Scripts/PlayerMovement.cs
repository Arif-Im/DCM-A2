using System;
using Mirror;
using Unity.VisualScripting;
using Cinemachine;
using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    private Camera mainCamera;

    #region Server

    [Command]
    private void CmdMove(Vector3 position)
    {
        
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            return;
        }
    
        agent.SetDestination(position);
        Debug.Log($"Click "  + position);

    }

    #endregion

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        CinemachineTargetGroup targetGroup = GameObject.Find("TargetGroup").GetComponent<CinemachineTargetGroup>();
        Cinemachine.CinemachineTargetGroup.Target target;
        target.target = this.transform.Find("LookAtPos").transform;
        target.weight = 1;
        target.radius = 2;

        for (int i = 0; i < targetGroup.m_Targets.Length; i++)
        {
            if (targetGroup.m_Targets[i].target == null)
            {
                targetGroup.m_Targets.SetValue(target, i);
                return;
            }
        }
    }

    private void Update()
    {
        if (!isOwned)
            return;
        if (!Input.GetMouseButton(0))
            return;
        Ray ray =  mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            return;
        }

        CmdMove(hit.point);
        

    }
    
    
}