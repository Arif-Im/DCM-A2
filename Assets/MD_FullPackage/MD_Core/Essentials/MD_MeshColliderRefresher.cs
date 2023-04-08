using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
#endif

namespace MD_Package
{
    /// <summary>
    /// MD(Mesh Deformation): Mesh Collider Refresher.
    /// Essential component for general mesh-collider refreshing.
    /// Written by Matej Vanco (2014, updated in 2023).
    /// </summary>
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Mesh Collider Refresher")]
    public sealed class MD_MeshColliderRefresher : MonoBehaviour
    {
        public enum RefreshType { Once, PerFrame, Interval, Never };
        public RefreshType refreshType = RefreshType.Once;

        public float intervalInSeconds = 1f;
        public bool convexMeshCollider = false;
        public MeshColliderCookingOptions cookingOptions = ~MeshColliderCookingOptions.None;

        public bool ignoreRaycast = false;

        public Vector3 colliderOffset = Vector3.zero;

        [field:SerializeField] public MeshCollider MCcache { get; private set; }
        [SerializeField] private MeshFilter selfRenderCache;

        private void Awake()
        {
            if (!selfRenderCache) selfRenderCache = GetComponent<MeshFilter>();
            if (!MCcache) MCcache = GetComponent<MeshCollider>();

            if (refreshType == RefreshType.Never) return;
            MeshCollider_UpdateMeshCollider();
        }

        private float intervalTimer = 0;
        private void Update()
        {
            if (refreshType == RefreshType.PerFrame)
                MeshCollider_UpdateMeshCollider();
            else if (refreshType == RefreshType.Interval)
            {
                intervalTimer += Time.deltaTime;
                if (intervalTimer > intervalInSeconds)
                {
                    MeshCollider_UpdateMeshCollider();
                    intervalTimer = 0;
                }
            }
        }

        /// <summary>
        /// Update current mesh collider
        /// </summary>
        public void MeshCollider_UpdateMeshCollider()
        {
            if (refreshType == RefreshType.Never) return;

            if (ignoreRaycast) gameObject.layer = 2;

            if (!selfRenderCache) selfRenderCache = GetComponent<MeshFilter>();

            if (!selfRenderCache || (selfRenderCache && !selfRenderCache.sharedMesh))
            {
                MD_Debug.Debug(this, "Object " + this.name + " doesn't contain any Mesh Renderer Component. Mesh Collider Refresher could not be proceeded", MD_Debug.DebugType.Error);
                return;
            }

            if (!MCcache) MCcache = GetComponent<MeshCollider>(); 
            if (!MCcache) MCcache = gameObject.AddComponent<MeshCollider>();

            MCcache.sharedMesh = selfRenderCache.sharedMesh;
            MCcache.convex = convexMeshCollider;
            MCcache.cookingOptions = cookingOptions;

            if (refreshType != RefreshType.Once)
                return;
            if (colliderOffset == Vector3.zero)
                return;

            Mesh newMeshCol = new Mesh();
            newMeshCol.vertices = MCcache.sharedMesh.vertices;
            newMeshCol.triangles = MCcache.sharedMesh.triangles;
            newMeshCol.normals = MCcache.sharedMesh.normals;
            Vector3[] verts = newMeshCol.vertices;
            for (int i = 0; i < verts.Length; i++)
                verts[i] += colliderOffset;
            newMeshCol.vertices = verts;
            MCcache.sharedMesh = newMeshCol;
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CustomEditor(typeof(MD_MeshColliderRefresher))]
    [CanEditMultipleObjects]
    public class MD_MeshColliderRefresher_Editor : MD_EditorUtilities
    {
        private MD_MeshColliderRefresher m;

        private void OnEnable()
        {
            m = (MD_MeshColliderRefresher)target;
        }

        public override void OnInspectorGUI()
        {
            if(!m)
            {
                DrawDefaultInspector();
                return;
            }

            MDE_s();
            ColorUtility.TryParseHtmlString("#9fe6b2", out Color c);
            GUI.color = c;
            MDE_v();
            MDE_v();
            MDE_DrawProperty("refreshType", "Collider Refresh Type","Once - Refreshes the collider just once in the startup. Per Frame - Refreshes the collider every frame after start. Interval - Refreshes the collider in the specific interval after start. Never - Never refreshes the collider.");
            if (m.refreshType == MD_MeshColliderRefresher.RefreshType.Interval)
                MDE_DrawProperty("intervalInSeconds", "Interval (every N second)", "Set the interval value for mesh collider refreshing in seconds");
            else if (m.refreshType == MD_MeshColliderRefresher.RefreshType.Once)
                MDE_DrawProperty("colliderOffset", "Collider Offset", "Specific offset of the mesh collider generated after start");
            MDE_ve();
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("convexMeshCollider", "Convex Mesh Collider");
            MDE_DrawProperty("cookingOptions", "Cooking Options", "Specify the mesh collider in higher details by choosing proper cooking options");
            MDE_ve();
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("ignoreRaycast", "Ignore Raycast", "If enabled, the objects layer mask will be set to 2 [Ignore raycast]. Otherwise the masks will be untouched");
            MDE_ve();
            if (!m.gameObject.GetComponent<MeshCollider>() && MDE_b("Add Mesh Collider Now"))
                m.gameObject.AddComponent<MeshCollider>();
            MDE_ve();
            serializedObject.Update();
        }
    }
}
#endif
