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
    /// MDG(Mesh Deformation Geometry): Path Creator.
    /// Complete path-creator with node-based editor in Unity Engine at runtime. Apply this script to any gameObject with meshFilter.
    /// Written by Matej Vanco (2020, updated in 2023).
    /// </summary>
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Geometry/Node-Based/Path Creator")]
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class MDG_PathCreator : MonoBehaviour
    {
        public float pathSize = 1.0f;
        public float pathNodeSize = 0.2f;

        public bool pathApplyWorldUvs = true;
        public bool pathReverseFaces = false;
        public bool pathApplyNodeLocalScale = false;

        public bool pathUpdateEveryFrame = true;
        public bool pathUseSmartRotation = true;

        public bool pathEnableGizmos = true;

        public List<Transform> PathCurrentNodes { get => pathCurrentNodes; private set => pathCurrentNodes = value; }
        [SerializeField] private List<Transform> pathCurrentNodes = new List<Transform>();

        [SerializeField] private List<Vector3> pathVertices = new List<Vector3>();
        [SerializeField] private List<int> pathTriangles = new List<int>();
        [SerializeField] private List<Vector2> pathUVs = new List<Vector2>();

        [SerializeField] private Mesh pathCurrentMesh;
        [SerializeField] private MeshFilter pathMeshFilter;

#if UNITY_EDITOR
        [MenuItem("GameObject/3D Object" + MD_Debug.PACKAGENAME + "Node-Based Editors/Path Creator")]
#endif
        public static void CreateGeometry()
        {
            GameObject newPath = new GameObject("Path Creator");
            newPath.AddComponent<MDG_PathCreator>();
#if UNITY_EDITOR
            Selection.activeGameObject = newPath;
#endif
            newPath.transform.position = Vector3.zero;

            Material mat = new Material(Utilities.MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader());
            newPath.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void OnDrawGizmos()
        {
            if (!pathEnableGizmos)
                return;
            if (pathCurrentNodes.Count == 0)
                return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < pathCurrentNodes.Count; i++)
            {
                if (pathCurrentNodes[i] == null)
                    continue;
                if (i != 0)
                    Gizmos.DrawLine(pathCurrentNodes[i].position, pathCurrentNodes[i - 1].position);
            }
        }

        private void Reset()
        {
            if (!GetComponent<MeshRenderer>())
                gameObject.AddComponent<MeshRenderer>();
            pathMeshFilter = GetComponent<MeshFilter>();
            if (!pathMeshFilter) pathMeshFilter = gameObject.AddComponent<MeshFilter>();
        }

        private void OnDestroy()
        {
            Path_RemoveAll();
        }

        private void Update()
        {
            if (pathUpdateEveryFrame)
                Path_RefreshNodes();
        }

        #region Public accessible functions

        /// <summary>
        /// Add node on specific position
        /// </summary>
        public void Path_AddNode(Vector3 positionInWorldSpace, bool groupTogetherOnAdd = true)
        {
            if (pathCurrentNodes.Count == 0)
            {
                PathCreateNodeBlock(positionInWorldSpace, groupTogetherOnAdd);
                PathCreateNodeBlock(positionInWorldSpace == Vector3.zero ? positionInWorldSpace + Vector3.forward : positionInWorldSpace, groupTogetherOnAdd);
            }
            else
                PathCreateNodeBlock((positionInWorldSpace == Vector3.zero) ? pathCurrentNodes[pathCurrentNodes.Count - 1].position + pathCurrentNodes[pathCurrentNodes.Count - 1].forward * 2 : positionInWorldSpace, groupTogetherOnAdd);
        }

        /// <summary>
        /// Remove last node
        /// </summary>
        public void Path_RemoveLastNode()
        {
            PathRemoveLastNodeBlock();
        }

        /// <summary>
        /// Clear all nodes
        /// </summary>
        public void Path_RemoveAll()
        {
            pathVertices.Clear();
            pathTriangles.Clear();
            pathUVs.Clear();

            int c = pathCurrentNodes.Count;
            for (int i = 0; i < c; i++)
            {
                if (pathCurrentNodes[i] == null)
                    continue;
                if (!Application.isPlaying)
                    DestroyImmediate(pathCurrentNodes[i].gameObject);
                else
                    Destroy(pathCurrentNodes[i].gameObject);
            }

            pathCurrentNodes.Clear();
            pathMeshFilter.sharedMesh = null;
        }

        /// <summary>
        /// Refresh current tunnel mesh
        /// </summary>
        public void Path_RefreshNodes()
        {
            if (pathCurrentNodes.Count == 0)
                return;

            int iiindex = 0;
            for (int i = 0; i < pathCurrentNodes.Count; i++)
            {
                if (pathCurrentNodes.Count > 1 && i > 0 && pathUseSmartRotation && (pathCurrentNodes[i].position - pathCurrentNodes[i - 1].position) != Vector3.zero)
                    pathCurrentNodes[i].rotation = Quaternion.LookRotation(pathCurrentNodes[i].position - pathCurrentNodes[i - 1].position);
                PathRefreshMesh(iiindex, i);
                iiindex += 2;
            }

            if (pathReverseFaces)
            {
                pathCurrentMesh.triangles = pathCurrentMesh.triangles.Reverse().ToArray();
                pathCurrentMesh.normals = pathCurrentMesh.normals.Reverse().ToArray();
            }
            else
                pathCurrentMesh.triangles = pathCurrentMesh.triangles.ToArray();

            pathCurrentMesh.RecalculateBounds();
            pathMeshFilter.sharedMesh = pathCurrentMesh;
        }

        /// <summary>
        /// Group all nodes together in hierarchy
        /// </summary>
        public void Path_GroupAllNodesTogether()
        {
            for (int i = 0; i < pathCurrentNodes.Count; i++)
            {
                if (i <= 0)
                    continue;
                pathCurrentNodes[i].parent = pathCurrentNodes[i - 1];
            }
        }

        /// <summary>
        /// Ungroup all nodes to 'empty' or to 'some object'
        /// </summary>
        public void Path_UngroupAllNodes(Transform detachToOtherParent)
        {
            for (int i = 0; i < pathCurrentNodes.Count; i++)
            {
                if (i <= 0)
                    continue;
                pathCurrentNodes[i].parent = detachToOtherParent ? detachToOtherParent : null;
            }
        }

        /// <summary>
        /// Update UV sets list
        /// </summary>
        public void Path_UpdateUVs()
        {
            bool finishUVSet = false;
            for (int v = 0; v < pathVertices.Count; v++)
            {
                if (pathApplyWorldUvs)
                    pathUVs[v] = new Vector2(pathVertices[v].x, pathVertices[v].z);
                else
                {
                    if(finishUVSet)
                    {
                        pathUVs[v] = new Vector2(0, 1);
                        pathUVs[++v] = new Vector2(1, 1);
                    }
                    else
                    {
                        pathUVs[v] = new Vector2(0, 0);
                        pathUVs[++v] = new Vector2(1, 0);
                    }
                    finishUVSet = !finishUVSet;
                }
            }

            if (pathUVs.Count == pathCurrentMesh.vertexCount)
                pathCurrentMesh.uv = pathUVs.ToArray();
        }

        #endregion

        #region Privates

        // Creating path complete blocks
        private void PathCreateNodeBlock(Vector3 positionInWorldSpace, bool groupTogetherOnAdd = true)
        {
            Transform newOrigin = MDG_Octahedron.CreateGeometryAndDispose<MDG_Octahedron>().transform;
            newOrigin.name = "Node" + pathCurrentNodes.Count.ToString();
            newOrigin.localScale = Vector3.one * pathNodeSize;
            DestroyImmediate(newOrigin.GetComponent<SphereCollider>());
            newOrigin.position = positionInWorldSpace;

            if (groupTogetherOnAdd && pathCurrentNodes.Count >= 1)
                newOrigin.transform.parent = pathCurrentNodes[pathCurrentNodes.Count - 1].transform;

            pathCurrentNodes.Add(newOrigin.transform);

            PathCreateVertexChunk(newOrigin);
            PathCreateUVChunk();

            if (pathCurrentNodes.Count <= 1)
                return;

            PathCreateTriangleChunk();

            PathUpdateMeshParams();
        }

        private void PathRemoveLastNodeBlock()
        {
            if (pathCurrentNodes.Count == 0)
                return;
            else if (pathCurrentNodes.Count == 1)
            {
                if(!Application.isPlaying)
                    DestroyImmediate(pathCurrentNodes[pathCurrentNodes.Count - 1].gameObject);
                else
                    Destroy(pathCurrentNodes[pathCurrentNodes.Count - 1].gameObject);

                pathCurrentNodes.RemoveAt(pathCurrentNodes.Count - 1);
                pathVertices.RemoveRange(pathVertices.Count - 2, 2);
                pathUVs.RemoveRange(pathUVs.Count - 2, 2);
                return;
            }

            if (!Application.isPlaying)
                DestroyImmediate(pathCurrentNodes[pathCurrentNodes.Count - 1].gameObject);
            else
                Destroy(pathCurrentNodes[pathCurrentNodes.Count - 1].gameObject);

            pathCurrentNodes.RemoveAt(pathCurrentNodes.Count - 1);

            pathTriangles.RemoveRange(pathTriangles.Count - 6, 6);
            pathUVs.RemoveRange(pathUVs.Count - 2, 2);
            pathVertices.RemoveRange(pathVertices.Count - 2, 2);

            PathUpdateMeshParams();
        }

        private void PathRefreshMesh(int vertexQueue, int originQueue)
        {
            Transform OriginPosition = pathCurrentNodes[originQueue];

            for (int i = vertexQueue; i < vertexQueue + 2; i++)
            {
                Matrix4x4 m = Matrix4x4.TRS(OriginPosition.position, OriginPosition.rotation, pathApplyNodeLocalScale ? OriginPosition.localScale : Vector3.one);
                Vector3 pos = new Vector3(i % 2 == 0 ? -pathSize : pathSize, 0, 0);

                pos = m.MultiplyPoint3x4(pos);
                pathVertices[i] = pos;
            }

            Path_UpdateUVs();
            PathUpdateMeshParams();
        }

        private void PathUpdateMeshParams()
        {
            pathCurrentMesh = new Mesh();
            pathCurrentMesh.name = "PathMesh";
            pathCurrentMesh.vertices = pathVertices.ToArray();
            pathCurrentMesh.triangles = pathTriangles.ToArray();
            pathCurrentMesh.uv = pathUVs.ToArray();

            pathCurrentMesh.RecalculateNormals();
            pathCurrentMesh.RecalculateTangents();
            pathCurrentMesh.RecalculateBounds();

            if (!pathMeshFilter) 
                pathMeshFilter = GetComponent<MeshFilter>();

            pathMeshFilter.sharedMesh = pathCurrentMesh;
        }

        // Creating mesh chunks (vert, tris, uv)
        private void PathCreateVertexChunk(Transform newOrigin)
        {
            pathVertices.Add(new Vector3(-pathSize, 0, newOrigin.position.z));
            pathVertices.Add(new Vector3(pathSize, 0, newOrigin.position.z));
        }

        private void PathCreateTriangleChunk()
        {
            PathCreateFaceChunk(pathVertices.Count - 1);
        }

        private void PathCreateFaceChunk(int i)
        {
            int[] Faces = new int[]
            {
                i - 3, i - 1, i,
                i - 3, i, i - 2,
            };

            pathTriangles.AddRange(Faces);
        }

        private void PathCreateUVChunk()
        {
            pathUVs.Add(Vector2.zero);
            pathUVs.Add(Vector2.zero);
        }

        #endregion
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDG_PathCreator))]
    public sealed class MDG_PathCreator_Editor : MD_EditorUtilities
    {
        private MDG_PathCreator pc;

        private void OnEnable()
        {
            pc = (MDG_PathCreator)target;
        }

        private GameObject obj;

        public override void OnInspectorGUI()
        {
            MDE_s();
            MDE_l("Mesh Settings", true);
            MDE_v();
            MDE_DrawProperty("pathSize", "Path Size");
            MDE_DrawProperty("pathNodeSize", "Node Size");
            MDE_s();
            MDE_DrawProperty("pathApplyWorldUvs", "Apply UVs to World Coordinates", "If enabled, uvs will adapth to the world coordinates");
            MDE_DrawProperty("pathReverseFaces", "Reverse Faces");
            MDE_DrawProperty("pathApplyNodeLocalScale", "Apply Node Local Scale", "If enabled, the path size will change according to nodes local size");
            MDE_ve();
            MDE_s();
            MDE_l("Node Settings", true);
            MDE_v();
            MDE_DrawProperty("pathUseSmartRotation", "Enable Smart Rotation", "If enabled, the nodes will rotate naturally towards one another");
            obj = EditorGUILayout.ObjectField(new GUIContent("Root Node"), (Object)obj, typeof(GameObject), true) as GameObject;
            if (MDE_b("Load nodes from the Root Node to the path", "This will load & refresh all the path nodes from the 'RootNode'MDE_s children") && obj != null)
            {
                if (EditorUtility.DisplayDialog("Question", "Are you sure to load nodes from the assigned root's children? This will clear your current nodes", "Yes", "No"))
                {
                    pc.Path_RemoveAll();
                    foreach (Transform node in obj.GetComponentsInChildren<Transform>(true).Skip(1))
                        pc.Path_AddNode(node.position, false);
                    pc.Path_RefreshNodes();
                }
            }
            MDE_DrawProperty("pathCurrentNodes", "Current Nodes", "Array of available path nodes", true);
            MDE_s();
            MDE_l("Node Editor Manipulation", true);
            MDE_v();
            MDE_h();
            if (MDE_b("Add Node"))
                pc.Path_AddNode(Vector3.zero);
            if (MDE_b("Remove Last Node"))
                pc.Path_RemoveLastNode();
            MDE_he();
            MDE_h();
            if (MDE_b("Ungroup Nodes"))
                pc.Path_UngroupAllNodes(null);
            if (MDE_b("Group All Nodes Together"))
                pc.Path_GroupAllNodesTogether();
            MDE_he();
            MDE_ve();
            MDE_ve();
            MDE_s();
            MDE_l("Update Settings", true);
            MDE_v();
            MDE_DrawProperty("pathUpdateEveryFrame", "Update Every Frame");
            if (!pc.pathUpdateEveryFrame)
            {
                if (MDE_b("Refresh Nodes"))
                    pc.Path_RefreshNodes();
            }
            MDE_ve();

            MDE_s();

            MDE_DrawProperty("pathEnableGizmos", "Enable Debug");

            MDE_s();

            MDE_h();
            if (MDE_b("Show All Nodes"))
            {
                foreach (Transform p in pc.PathCurrentNodes)
                    p.GetComponent<MeshRenderer>().enabled = true;
            }
            if (MDE_b("Hide All Nodes"))
            {
                foreach (Transform p in pc.PathCurrentNodes)
                    p.GetComponent<MeshRenderer>().enabled = false;
            }
            MDE_he();
            if (MDE_b("Clear All Nodes"))
            {
                if (EditorUtility.DisplayDialog("Warning", "You are about to clear all nodes and whole path mesh. Are you sure? There is no way back.", "OK", "Cancel"))
                    pc.Path_RemoveAll();
            }

            MDE_s();
            ColorUtility.TryParseHtmlString("#f2d0d0", out Color c);
            GUI.color = c;
            MDE_s(5);
            if (MDE_b("Back To MeshProEditor"))
            {
                GameObject gm = pc.gameObject;
                DestroyImmediate(pc);
                gm.AddComponent<MD_MeshProEditor>();
            }
        }
    }
}
#endif
