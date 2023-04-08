using UnityEngine;

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Tunnel Node UV Data.
    /// Tunnel Node UV Data holds information for a chunk in Tunnel Creator. Apply this script to any tunnel-creator node.
    /// Written by Matej Vanco (2020, updated in 2023).
    /// </summary>
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Node-Based/Tunnel Creator Node UVData")]
    public sealed class MDG_TunnelNodeUVData : MonoBehaviour
    {
        public enum UVType { uvXY, uvXZ, uvYX, uvYZ, uvZX, uvZY };
        public UVType uvType;
        [Space]
        public Vector2 uvOffset;
        public Vector2 uvTransition;
        [Space]
        public float debugSize = 0.5f;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, debugSize);
        }
    }
}