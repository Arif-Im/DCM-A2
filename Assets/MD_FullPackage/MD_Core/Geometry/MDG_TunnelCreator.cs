using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Geometry;
#endif

namespace MD_Package.Geometry
{
    /// <summary>
    /// MDG(Mesh Deformation Geometry): Tunnel Creator.
    /// Complete tunnel-creator with node-based editor in Unity Engine at runtime. Apply this script to any gameObject with meshFilter.
    /// Written by Matej Vanco (2020, updated in 2023).
    /// </summary>
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Node-Based/Tunnel Creator")]
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class MDG_TunnelCreator : MonoBehaviour
    {
        [Range(4, 64)] public int tunnelResolution = 16;
        public int tunnelResolutionBackup = 0;
        public float tunnelRadius = 1.0f;
        public float tunnelNodeSize = 0.2f;

        public bool tunnelHemi = false;
        public bool tunnelHemiVertical = false;
        public bool tunnelReverseFaces = false;
        public bool tunnelApplyNodeLocalScale = false;

        public bool tunnelUpdateEveryFrame = true;
        public bool tunnelUseSmartRotation = true;

        public bool tunnelUseCustomUVData = false;
        public MDG_TunnelNodeUVData.UVType tunnelUVType = MDG_TunnelNodeUVData.UVType.uvZX;

        public bool tunnelEnableGizmos = true;

        public List<Transform> TunnelCurrentNodes { get => tunnelCurrentNodes; private set => tunnelCurrentNodes = value; }
        [SerializeField] private List<Transform> tunnelCurrentNodes = new List<Transform>();

        [SerializeField] private List<Vector3> tunnelVertices = new List<Vector3>();
        [SerializeField] private List<int> tunnelTriangles = new List<int>();
        [SerializeField] private List<Vector2> tunnelUVs = new List<Vector2>();

        [SerializeField] private Mesh tunnelCurrentMesh;
        [SerializeField] private MeshFilter tunnelMeshFilter;
        public Texture2D tunnelEditorIntern_ShowEditorWinIcon;

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Node-Based Editors/Tunnel Creator")]
#endif
        public static void CreateGeometry()
        {
            GameObject newTunnel = new GameObject("TunnelCreator");
            newTunnel.AddComponent<MDG_TunnelCreator>();
#if UNITY_EDITOR
            Selection.activeGameObject = newTunnel;
#endif
            newTunnel.transform.position = Vector3.zero;

            Material mat = new Material(Utilities.MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader());
            newTunnel.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void OnDrawGizmos()
        {
            if (!tunnelEnableGizmos)
                return;
            if (tunnelCurrentNodes.Count == 0)
                return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < tunnelCurrentNodes.Count; i++)
            {
                if (tunnelCurrentNodes[i] == null)
                    continue;
                if (i != 0)
                    Gizmos.DrawLine(tunnelCurrentNodes[i].position, tunnelCurrentNodes[i - 1].position);
            }
        }

        private void Reset()
        {
            if (!GetComponent<MeshRenderer>())
                gameObject.AddComponent<MeshRenderer>();
            tunnelMeshFilter = GetComponent<MeshFilter>();
            if (!tunnelMeshFilter) tunnelMeshFilter = gameObject.AddComponent<MeshFilter>();
            tunnelResolutionBackup = tunnelResolution;
        }

        private void Update()
        {
            if (tunnelUpdateEveryFrame)
                Tunnel_RefreshNodes();
        }

        private void OnDestroy()
        {
            Tunnel_RemoveAll();
        }

        #region Public accessible functions

        /// <summary>
        /// Add node on specific position
        /// </summary>
        public void Tunnel_AddNode(Vector3 positionInWorldSpace, bool groupTogetherOnAdd = true)
        {
            if (tunnelCurrentNodes.Count == 0)
                TunnelCreateSingleBlock((positionInWorldSpace == Vector3.zero) ? Vector3.zero : positionInWorldSpace, groupTogetherOnAdd);
            else
                TunnelCreateSingleBlock((positionInWorldSpace == Vector3.zero) ? tunnelCurrentNodes[tunnelCurrentNodes.Count - 1].position + tunnelCurrentNodes[tunnelCurrentNodes.Count - 1].forward * 2 : positionInWorldSpace, groupTogetherOnAdd);
        }

        /// <summary>
        /// Remove last node
        /// </summary>
        public void Tunnel_RemoveLastNode()
        {
            TunnelRemoveSingleLastBlock();
        }

        /// <summary>
        /// Clear all nodes
        /// </summary>
        public void Tunnel_RemoveAll()
        {
            tunnelVertices.Clear();
            tunnelTriangles.Clear();
            tunnelUVs.Clear();

            int c = tunnelCurrentNodes.Count;
            for (int i = 0; i < c; i++)
            {
                if (tunnelCurrentNodes[i] == null)
                    continue;
                if(!Application.isPlaying)
                    DestroyImmediate(tunnelCurrentNodes[i].gameObject);
                else
                    Destroy(tunnelCurrentNodes[i].gameObject);
            }

            tunnelCurrentNodes.Clear();
            tunnelMeshFilter.sharedMesh = null;
        }

        /// <summary>
        /// Refresh current tunnel mesh
        /// </summary>
        public void Tunnel_RefreshNodes()
        {
            if (tunnelCurrentNodes.Count == 0)
                return;
            if (tunnelResolutionBackup != tunnelResolution)
                return;

            int indx = 0;
            for (int i = 0; i < tunnelCurrentNodes.Count; i++)
            {
                if (tunnelCurrentNodes.Count > 1 && i > 0 && tunnelUseSmartRotation)
                    tunnelCurrentNodes[i].rotation = Quaternion.LookRotation(tunnelCurrentNodes[i].position - tunnelCurrentNodes[i - 1].position);
                TunnelRefreshMesh(indx, i);
                indx += tunnelResolution;
            }

            if (tunnelReverseFaces)
            {
                tunnelCurrentMesh.triangles = tunnelCurrentMesh.triangles.Reverse().ToArray();
                tunnelCurrentMesh.normals = tunnelCurrentMesh.normals.Reverse().ToArray();
            }
            else
                tunnelCurrentMesh.triangles = tunnelCurrentMesh.triangles.ToArray();

            tunnelCurrentMesh.RecalculateBounds();
            tunnelMeshFilter.sharedMesh = tunnelCurrentMesh;
        }

        /// <summary>
        /// Apply changed vertex count and refresh
        /// </summary>
        public void Tunnel_ApplyResolution()
        {
            tunnelResolutionBackup = tunnelResolution;

            if (tunnelCurrentNodes.Count <= 1)
                return;
            List<Vector3> verts = new List<Vector3>();

            foreach (Transform item in tunnelCurrentNodes)
                verts.Add(item.position);

            Tunnel_RemoveAllInternal();
            for (int i = 0; i < verts.Count; i++)
                Tunnel_AddNode(verts[i]);

            Tunnel_UngroupAllNodes(null);

            TunnelUpdateMeshParams();
            Tunnel_RefreshNodes();
        }

        private void Tunnel_RemoveAllInternal()
        {
            for (int i = tunnelCurrentNodes.Count - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(tunnelCurrentNodes[i].gameObject);
                else
                    DestroyImmediate(tunnelCurrentNodes[i].gameObject);
            }

            tunnelCurrentNodes.Clear();

            tunnelVertices.Clear();
            tunnelTriangles.Clear();
            tunnelUVs.Clear();

            tunnelMeshFilter.sharedMesh = null;
        }

        /// <summary>
        /// Group all nodes together in hierarchy
        /// </summary>
        public void Tunnel_GroupAllNodesTogether()
        {
            for (int i = 0; i < tunnelCurrentNodes.Count; i++)
            {
                if (i <= 0)
                    continue;
                tunnelCurrentNodes[i].parent = tunnelCurrentNodes[i - 1];
            }
        }

        /// <summary>
        /// Ungroup all nodes to 'empty' or to 'some object'
        /// </summary>
        public void Tunnel_UngroupAllNodes(Transform detachToOtherParent = null)
        {
            for (int i = 0; i < tunnelCurrentNodes.Count; i++)
            {
                if (i <= 0)
                    continue;
                tunnelCurrentNodes[i].parent = detachToOtherParent ? detachToOtherParent : null;
            }
        }

        /// <summary>
        /// Update UV sets with specific UV mode
        /// </summary>
        public void Tunnel_UpdateUVsWithUVMode(MDG_TunnelNodeUVData.UVType uvMode)
        {
            for (int v = 0; v < tunnelVertices.Count; v++)
            {
                float uvModeX = tunnelVertices[v].x;
                float uvModeY = tunnelVertices[v].y;
                switch (uvMode)
                {
                    case MDG_TunnelNodeUVData.UVType.uvXY:
                        uvModeX = tunnelVertices[v].x;
                        uvModeY = tunnelVertices[v].y;
                        break;

                    case MDG_TunnelNodeUVData.UVType.uvXZ:
                        uvModeX = tunnelVertices[v].x;
                        uvModeY = tunnelVertices[v].z;
                        break;

                    case MDG_TunnelNodeUVData.UVType.uvYX:
                        uvModeX = tunnelVertices[v].y;
                        uvModeY = tunnelVertices[v].x;
                        break;
                    case MDG_TunnelNodeUVData.UVType.uvYZ:
                        uvModeX = tunnelVertices[v].y;
                        uvModeY = tunnelVertices[v].z;
                        break;

                    case MDG_TunnelNodeUVData.UVType.uvZX:
                        uvModeX = tunnelVertices[v].z;
                        uvModeY = tunnelVertices[v].x;
                        break;
                    case MDG_TunnelNodeUVData.UVType.uvZY:
                        uvModeX = tunnelVertices[v].z;
                        uvModeY = tunnelVertices[v].y;
                        break;
                }
                tunnelUVs[v] = new Vector2(uvModeX, uvModeY);
            }
            if (tunnelCurrentMesh && tunnelUVs.Count == tunnelCurrentMesh.vertexCount)
                tunnelCurrentMesh.uv = tunnelUVs.ToArray();
        }

        #endregion

        #region Privates

        // Creating tunnel complete blocks
        private void TunnelCreateSingleBlock(Vector3 OriginPosition, bool GroupOnAdd = true)
        {
            Transform newOrigin = MDG_Octahedron.CreateGeometryAndDispose<MDG_Octahedron>().transform;
            newOrigin.name = "Node" + tunnelCurrentNodes.Count.ToString();
            newOrigin.localScale = Vector3.one * tunnelNodeSize;
            DestroyImmediate(newOrigin.GetComponent<SphereCollider>());
            newOrigin.position = OriginPosition;

            if (GroupOnAdd && tunnelCurrentNodes.Count >= 1)
                newOrigin.transform.parent = tunnelCurrentNodes[tunnelCurrentNodes.Count - 1].transform;

            tunnelCurrentNodes.Add(newOrigin.transform);

            TunnelCreateVertexChunk(newOrigin);
            TunnelCreateUVChunk();

            if (tunnelCurrentNodes.Count <= 1)
                return;

            TunnelCreateTriangleChunk();

            TunnelUpdateMeshParams();
        }

        private void TunnelRemoveSingleLastBlock()
        {
            if (tunnelCurrentNodes.Count == 0)
                return;
            else if (tunnelCurrentNodes.Count == 1)
            {
                DestroyImmediate(tunnelCurrentNodes[tunnelCurrentNodes.Count - 1].gameObject);
                tunnelCurrentNodes.RemoveAt(tunnelCurrentNodes.Count - 1);
                tunnelVertices.RemoveRange(tunnelVertices.Count - tunnelResolution, tunnelResolution);
                tunnelUVs.RemoveRange(tunnelUVs.Count - (tunnelResolution), tunnelResolution);
                return;
            }

            DestroyImmediate(tunnelCurrentNodes[tunnelCurrentNodes.Count - 1].gameObject);
            tunnelCurrentNodes.RemoveAt(tunnelCurrentNodes.Count - 1);

            tunnelTriangles.RemoveRange(tunnelTriangles.Count - (tunnelResolution * 6), tunnelResolution * 6);
            tunnelUVs.RemoveRange(tunnelUVs.Count - (tunnelResolution), tunnelResolution);
            tunnelVertices.RemoveRange(tunnelVertices.Count - tunnelResolution, tunnelResolution);

            TunnelUpdateMeshParams();
        }

        private void TunnelRefreshMesh(int vertexQueue, int originQueue)
        {
            Transform originPos = tunnelCurrentNodes[originQueue];
            float deltaTheta = ((tunnelHemi ? 1 : 2) * Mathf.PI) / tunnelResolution;
            float currentTheta = 0;

            for (int i = vertexQueue; i < vertexQueue + tunnelResolution; i++)
            {
                Matrix4x4 m = Matrix4x4.TRS(originPos.position, originPos.rotation, tunnelApplyNodeLocalScale ? originPos.localScale : Vector3.one);
                Vector3 pos;
                if (tunnelHemiVertical)
                    pos = new Vector3(tunnelRadius * Mathf.Sin(currentTheta), tunnelRadius * Mathf.Cos(currentTheta), 0);
                else
                    pos = new Vector3(tunnelRadius * -Mathf.Cos(currentTheta), tunnelRadius * Mathf.Sin(currentTheta), 0);
                pos = m.MultiplyPoint3x4(pos);
                currentTheta += deltaTheta;
                tunnelVertices[i] = pos;
            }

            if (tunnelUseCustomUVData)
                TunnelUpdateUVAtIndex(originQueue);
            else
                Tunnel_UpdateUVsWithUVMode(tunnelUVType);

            TunnelUpdateMeshParams();
        }

        private void TunnelUpdateMeshParams()
        {
            tunnelCurrentMesh = new Mesh();
            tunnelCurrentMesh.name = "TunnelMesh";
            tunnelCurrentMesh.vertices = tunnelVertices.ToArray();
            tunnelCurrentMesh.triangles = tunnelTriangles.ToArray();
            tunnelCurrentMesh.uv = tunnelUVs.ToArray();

            tunnelCurrentMesh.RecalculateNormals();
            tunnelCurrentMesh.RecalculateTangents();
            tunnelCurrentMesh.RecalculateBounds();

            if (!tunnelMeshFilter) 
                tunnelMeshFilter = GetComponent<MeshFilter>();

            tunnelMeshFilter.sharedMesh = tunnelCurrentMesh;
        }

        // Updating UVs by certain UVType
        private void TunnelUpdateUVAtIndex(int originQueue)
        {
            int atIndex = tunnelResolution * originQueue + tunnelResolution;
            int vCount = tunnelResolution;
            int counter = 0;
            bool applyFirstRound = false;
            for (int i = originQueue + 1; i < tunnelCurrentNodes.Count; i++)
            {
                if (i >= tunnelCurrentNodes.Count)
                    break;
                if (tunnelCurrentNodes[i].GetComponent<MDG_TunnelNodeUVData>())
                    break;
                counter++;
            }
            if (counter != 0)
                vCount *= counter;
            if (originQueue > 0)
                applyFirstRound = true;

            int indexLength = atIndex + vCount;
            MDG_TunnelNodeUVData uvdat;
            if (tunnelCurrentNodes[originQueue].TryGetComponent(out MDG_TunnelNodeUVData mdtnuvd))
                uvdat = mdtnuvd;
            else 
                return;

            bool firstRoundCompleted = false;
            int firstFullVertArray = 0;
            for (int v = atIndex; v < indexLength; v++)
            {
                firstFullVertArray++;
                Vector2 uvcor = TunnelReturnProperUVType(uvdat.uvType, v);
                if (!firstRoundCompleted && applyFirstRound)
                    tunnelUVs[v - tunnelResolution] = uvcor + uvdat.uvTransition;
                if (firstFullVertArray >= tunnelResolution && applyFirstRound && !firstRoundCompleted)
                    firstRoundCompleted = true;

                tunnelUVs[v] = uvcor + uvdat.uvOffset;
            }
            tunnelCurrentMesh.uv = tunnelUVs.ToArray();
        }

        private Vector2 TunnelReturnProperUVType(MDG_TunnelNodeUVData.UVType uvType, int vertexIndex)
        {
            switch (uvType)
            {
                default:
                    return new Vector2(tunnelVertices[vertexIndex].x, tunnelVertices[vertexIndex].y);
                case MDG_TunnelNodeUVData.UVType.uvXZ:
                    return new Vector2(tunnelVertices[vertexIndex].x, tunnelVertices[vertexIndex].z);

                case MDG_TunnelNodeUVData.UVType.uvYX:
                    return new Vector2(tunnelVertices[vertexIndex].y, tunnelVertices[vertexIndex].x);
                case MDG_TunnelNodeUVData.UVType.uvYZ:
                    return new Vector2(tunnelVertices[vertexIndex].y, tunnelVertices[vertexIndex].z);

                case MDG_TunnelNodeUVData.UVType.uvZX:
                    return new Vector2(tunnelVertices[vertexIndex].z, tunnelVertices[vertexIndex].x);
                case MDG_TunnelNodeUVData.UVType.uvZY:
                    return new Vector2(tunnelVertices[vertexIndex].z, tunnelVertices[vertexIndex].y);
            }
        }

        // Creating mesh chunks (vert, tris, uv)
        private void TunnelCreateVertexChunk(Transform newOrigin)
        {
            float deltaTheta = (2 * Mathf.PI) / tunnelResolution;
            float currentTheta = 0;
            for (int i = 0; i < tunnelResolution; i++)
            {
                Vector3 pos = new Vector3(tunnelRadius * Mathf.Sin(currentTheta), tunnelRadius * Mathf.Cos(currentTheta), newOrigin.position.z);
                currentTheta += deltaTheta;
                tunnelVertices.Add(pos);
            }
        }

        private void TunnelCreateTriangleChunk()
        {
            int lastVerticeIndex = tunnelVertices.Count - (tunnelResolution * 2);
            for (int i = lastVerticeIndex; i < tunnelVertices.Count - tunnelResolution; i++)
                TunnelCreateFaceChunk(i, tunnelResolution, tunnelVertices.Count);
        }

        private void TunnelCreateFaceChunk(int index, int maxAdd, int maxCount)
        {
            int i = index;
            int largestAdd = maxAdd;
            bool final = false;

            if (i >= maxCount - maxAdd - 1)
                final = true;

            int[] Faces;

            if (final)
            {
                Faces = new int[]
                {
                i - largestAdd + 1, i + 1, i + largestAdd,
                i - largestAdd + 1, i + largestAdd, i,
                };
            }
            else
            {
                Faces = new int[]
                {
                i + 1, i + largestAdd, i,
                i + 1, i + largestAdd + 1, i + largestAdd,
                };
            }

            tunnelTriangles.AddRange(Faces);
        }

        private void TunnelCreateUVChunk()
        {
            int lastVerticeIndex = tunnelVertices.Count - tunnelResolution;
            for (int i = lastVerticeIndex; i < tunnelVertices.Count; i++)
                tunnelUVs.Add(new Vector2(0, 0));
        }

        #endregion
    }
}

#if UNITY_EDITOR

namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_TunnelCreator))]
    public sealed class MDG_TunnelCreator_Editor : MD_EditorUtilities
    {
        private MDG_TunnelCreator tc;

        private void OnEnable()
        {
            tc = (MDG_TunnelCreator)target;
        }

        private GameObject obj;

        public override void OnInspectorGUI()
        {
            MDE_s();
            MDE_l("Mesh Settings", true);
            MDE_v();
            MDE_DrawProperty("tunnelResolution", "Tunnel Resolution");
            if (tc.tunnelResolutionBackup != tc.tunnelResolution)
                MDE_hb("The tunnel resolution has changed. Press Apply to change the tunnel resolution. [Current: " + tc.tunnelResolutionBackup.ToString() + "]", MessageType.Info);
            if (MDE_b("Apply Tunnel Resolution", 160))
            {
                if (EditorUtility.DisplayDialog("Warning", "You are about to apply a new tunnel resolution. This will reset all the created nodes but the path will remain (All node components will be removed). Are you sure to continue?", "Yes", "No"))
                    tc.Tunnel_ApplyResolution();
            }
            MDE_s();
            MDE_DrawProperty("tunnelRadius", "Tunnel Radius");
            MDE_DrawProperty("tunnelNodeSize", "Node Size");
            MDE_s();
            MDE_DrawProperty("tunnelHemi", "Hemi Tunnel");
            if (tc.tunnelHemi)
                MDE_DrawProperty("tunnelHemiVertical", "Vertical Hemi Tunnel");
            MDE_DrawProperty("tunnelReverseFaces", "Reverse Faces");
            MDE_DrawProperty("tunnelApplyNodeLocalScale", "Apply Node Local Scale", "If enabled, the tunnel radius will change according to nodes local size");
            MDE_ve();

            MDE_s();
            MDE_l("Node Settings", true);
            MDE_v();
            MDE_DrawProperty("tunnelUseSmartRotation", "Enable Smart Rotation", "If enabled, the nodes will rotate naturally towards one another");
            obj = EditorGUILayout.ObjectField(new GUIContent("Root Node"), obj, typeof(GameObject), true) as GameObject;
            if (MDE_b("Load nodes from the Root Node to the tunnel", "This will load & refresh all the tunnel nodes from the 'RootNode's children") && obj != null)
            {
                if (EditorUtility.DisplayDialog("Question", "Are you sure to load nodes from the assigned root's children? This will clear your current nodes", "Yes", "No"))
                {
                    tc.Tunnel_RemoveAll();
                    foreach (Transform node in obj.GetComponentsInChildren<Transform>(true).Skip(1))
                        tc.Tunnel_AddNode(node.position, false);
                }
            }
            MDE_DrawProperty("tunnelCurrentNodes", "Current Nodes", "Array of available tunnel nodes", true);
            MDE_ve();

            MDE_s();
            MDE_l("UV Settings", true);
            MDE_v();
            MDE_DrawProperty("tunnelUseCustomUVData", "Use Custom UV Data", "If enabled, the UV data will be get from the nodes that contain MDM_TunnelNodeUVData behaviour");
            if (!tc.tunnelUseCustomUVData)
            {
                MDE_DrawProperty("tunnelUVType", "UV Mode");
                if (tc.tunnelUpdateEveryFrame == false)
                {
                    if (GUILayout.Button("Refresh UVs"))
                        tc.Tunnel_UpdateUVsWithUVMode(tc.tunnelUVType);
                }
            }
            else MDE_hb("UV will be set from the tunnel nodes", MessageType.Info);
            MDE_ve();

            MDE_s();
            MDE_l("Update Settings", true);
            MDE_v();
            MDE_DrawProperty("tunnelUpdateEveryFrame", "Update Every Frame");
            if (!tc.tunnelUpdateEveryFrame)
            {
                if (MDE_b("Refresh Nodes"))
                    tc.Tunnel_RefreshNodes();
            }
            MDE_ve();

            MDE_s();

            MDE_DrawProperty("tunnelEnableGizmos", "Enable Gizmos");

            MDE_s();

            if (MDE_b(new GUIContent("Show Tunnel Editor", tc.tunnelEditorIntern_ShowEditorWinIcon)))
                MDG_TunnelCreatorEditorWindow.Init(tc);

            MDE_s();

            MDE_h();
            if (MDE_b("Show All Nodes"))
            {
                foreach (Transform p in tc.TunnelCurrentNodes)
                    p.GetComponent<MeshRenderer>().enabled = true;
            }

            if (MDE_b("Hide All Nodes"))
            {
                foreach (Transform p in tc.TunnelCurrentNodes)
                    p.GetComponent<MeshRenderer>().enabled = false;
            }
            MDE_he();
            if (MDE_b("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Warning", "You are about to clear all the nodes and the tunnel mesh. Are you sure? There is no way back.", "OK", "Cancel"))
                    tc.Tunnel_RemoveAll();
            }

            MDE_s();
            ColorUtility.TryParseHtmlString("#f2d0d0", out Color c);
            GUI.color = c;
            MDE_s(5);
            if (MDE_b("Back To MeshProEditor"))
            {
                GameObject gm = tc.gameObject;
                DestroyImmediate(tc);
                gm.AddComponent<MD_MeshProEditor>();
            }
        }
    }
}
#endif
