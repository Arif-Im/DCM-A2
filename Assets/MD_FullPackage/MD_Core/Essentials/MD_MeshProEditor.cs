using System.Collections.Generic;
using UnityEngine;

using MD_Package.Modifiers;
using MD_Package.Geometry;
using MD_Package.Utilities;

namespace MD_Package
{
    /// <summary>
    /// MD(Mesh Deformation): Mesh Pro Editor.
    /// Essential component for general mesh processing and cross-bridge between Mesh Deformation elements.
    /// Written by Matej Vanco (2013, updated in 2023).
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Mesh Pro Editor")]
    public sealed class MD_MeshProEditor : MonoBehaviour
    {
        // Essential Params
        public enum EditorModification { None, Vertices, Collider, Identity, Mesh };
        public EditorModification meshSelectedModification;

        public bool meshNewReferenceAfterCopy = true;
        public bool meshUpdateEveryFrame = true;
        public bool meshAnimationMode = false;
        public bool meshOptimizeMesh = false;
        public bool meshAlternativeNormals = false;
        public float meshAlternativeNormalsAngle = 90.0f;

        #region Vertex editor variables

        public Transform meshVertexEditor_PointsRoot;
        public bool meshVertexEditor_CustomPointPattern = false;
        public bool meshVertexEditor_UseCustomColor = true;
        public Color meshVertexEditor_CustomPointColor = Color.red;
        public GameObject meshVertexEditor_PointPatternObject;
        public float meshVertexEditor_PointSizeMultiplier = 1.0f;

        #endregion

        #region Basic mesh info variables

        public string meshInfoMeshName;
        public int meshInfoVertices = 0;
        public int meshInfoTriangles = 0;
        public int meshInfoNormals = 0;
        public int meshInfoUVs = 0;

        // Stored original mesh
        [SerializeField] private Mesh meshInitialMesh;

        #endregion

        // Vertices & points for editing
        public List<Transform> meshWorkingPoints = new List<Transform>();

        // Default material
        private Material meshDefaultMaterial;

        // Other essentials
        private bool meshDeselectObjectAfterVerticeLimit;

        public bool MeshAlreadyAwake { get => _meshAlreadyAwake; private set => _meshAlreadyAwake = value; }
        [SerializeField] private bool _meshAlreadyAwake;
        public bool MeshBornAsSkinnedMesh { get => _meshBornAsSkinnedMesh; private set => _meshBornAsSkinnedMesh = value; }
        [SerializeField] private bool _meshBornAsSkinnedMesh = false;

        [field:SerializeField] public MeshFilter MyMeshFilter { get; private set; }

        // Zone Generator utility
        public bool meshEnableZoneGenerator = false;
        public Vector3 meshZoneGeneratorPosition;
        public float meshZoneGeneratorRadius = 0.5f;

        public Vector3 meshStartBounds;

        private void Reset()
        {
            MyMeshFilter = GetComponent<MeshFilter>();
        }

        // Base initialization

        private void Awake () 
        {            
            if(!MyMeshFilter)
                MyMeshFilter = GetComponent<MeshFilter>();

            if (!MyMeshFilter.sharedMesh)
            {
                MD_Debug.Debug(this, "Mesh Filter doesn't contain any mesh data. The behaviour will be destroyed", MD_Debug.DebugType.Error);
                if (Application.isPlaying)
                    Destroy(this);
                else
                    DestroyImmediate(this); return;
            }

            if (!MD_Utilities.MD_Specifics.RestrictFromOtherTypes(this.gameObject, this.GetType(), new System.Type[]{typeof(MD_GeometryBase),typeof(MD_ModifierBase)}))
            {
                MD_Debug.Debug(this, "The mesh-editor cannot be applied to this object, because the object already contains other modifiers or components that work with mesh-vertices. Please remove the existing mesh-related-components to access the mesh-editor");
                if (Application.isPlaying)
                    Destroy(this);
                else
                    DestroyImmediate(this); 
                return;
            }

            if(meshDefaultMaterial == null)
                meshDefaultMaterial = new Material(MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader());

            if (!MeshAlreadyAwake)
            {
                if (string.IsNullOrEmpty(meshInfoMeshName))
                    meshInfoMeshName = "NewMesh" + Random.Range(1, 99999).ToString();
                meshAlternativeNormals = MD_GlobalPreferences.AlternateNormalsRecalc;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (MD_GlobalPreferences.PopupEditorWindow)
                    {
                        if (UnityEditor.EditorUtility.DisplayDialog("Create a New Reference?", "Would you like to create a new reference? If yes (recommended), a brand new mesh reference will be created. If no, existing mesh references will share the same data as this mesh reference", "Yes", "No"))
                            Internal_MPE_ResetReference();
                    }
                    else if(MD_GlobalPreferences.CreateNewReference)
                        Internal_MPE_ResetReference();
                }
                else if (!meshAnimationMode && MD_GlobalPreferences.CreateNewReference)
                    Internal_MPE_ResetReference();
#else
                if (!meshAnimationMode && MD_GlobalPreferences.CreateNewReference)
                     Internal_MPE_ResetReference();
#endif
                meshStartBounds = MyMeshFilter.sharedMesh.bounds.max;
                Internal_MPE_ReceiveMeshInfo();

                MeshAlreadyAwake = true;
            }
            else if (meshNewReferenceAfterCopy)
                Internal_MPE_ResetReference();
        }

        #region Internal methods

        /// <summary>
        /// Reset current mesh reference
        /// </summary>
        private void Internal_MPE_ResetReference()
        {
            if (MyMeshFilter == null) 
                return;

            if (meshSelectedModification == EditorModification.Vertices)
                MPE_CreatePointsEditor();
            else
                MPE_ClearPointsEditor();

            Mesh newMesh = MD_Utilities.MD_Specifics.CreateNewMeshReference(MyMeshFilter.sharedMesh);
            newMesh.name = meshInfoMeshName;
            MyMeshFilter.sharedMesh = newMesh;

            Internal_MPE_ReceiveMeshInfo();
        }

       /// <summary>
       /// Refresh current mesh info such as vertex count, triangle count etc
       /// </summary>
        private void Internal_MPE_ReceiveMeshInfo(bool passAlreadyAwake = false)
        {
            if (!MyMeshFilter || !MyMeshFilter.sharedMesh) return;

            Mesh myMesh = MyMeshFilter.sharedMesh;
            meshInfoVertices = myMesh.vertexCount;
            meshInfoTriangles = myMesh.triangles.Length;
            meshInfoNormals = myMesh.normals.Length;
            meshInfoUVs = myMesh.uv.Length;
            if (!MeshAlreadyAwake || passAlreadyAwake)
            {
				meshInitialMesh = Instantiate(myMesh);
                meshInitialMesh.name = myMesh.name;
            }
        }

        #endregion

        private void Update () 
        {
            if (meshUpdateEveryFrame)
                MPE_UpdateMesh();
        }

        #region Public methods

        // Mesh-essentials

        /// <summary>
        /// Update current mesh state (sync generated points with mesh vertices) & recalculate normals+bounds
        /// </summary>
        public void MPE_UpdateMesh()
        {
            if (MyMeshFilter == null || MyMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            if (meshVertexEditor_PointsRoot == null || meshWorkingPoints == null || meshWorkingPoints.Count == 0) 
                return;
            if (MyMeshFilter.sharedMesh.vertexCount != meshWorkingPoints.Count)
                return;

            Vector3[] meshWorkingVertices = MyMeshFilter.sharedMesh.vertices;

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            for (int i = 0; i < meshWorkingVertices.Length; i++)
            {
                if (meshWorkingPoints.Count > i)
                {
                    if (meshWorkingPoints[i] != null)
                        meshWorkingVertices[i] = new Vector3(
                            meshWorkingPoints[i].position.x - (MyMeshFilter.transform.position.x - Vector3.zero.x),
                            meshWorkingPoints[i].position.y - (MyMeshFilter.transform.position.y - Vector3.zero.y),
                            meshWorkingPoints[i].position.z - (MyMeshFilter.transform.position.z - Vector3.zero.z));
                }
            }
            MyMeshFilter.sharedMesh.vertices = meshWorkingVertices;
            MPE_RecalculateMesh();
        }

        /// <summary>
        /// Recalculate current mesh normals and bounds
        /// </summary>
        /// <param name="ignoreOptimizeMesh">Ignore optimization? If yes, the mesh gets always recalculated no matter what</param>
        public void MPE_RecalculateMesh(bool ignoreOptimizeMesh = false)
        {
            if (meshOptimizeMesh && !ignoreOptimizeMesh)
                return;

            if (!meshAlternativeNormals)
                MyMeshFilter.sharedMesh.RecalculateNormals();
            else
                MD_Utilities.AlternativeNormals.RecalculateNormals(MyMeshFilter.sharedMesh, meshAlternativeNormalsAngle);
            MyMeshFilter.sharedMesh.RecalculateBounds();
        }

        // Mesh Editor Vertices

        /// <summary>
        /// Hide/Show generated points on the mesh
        /// </summary>
        public void MPE_ShowHidePoints(bool activeState)
        {
            if (meshWorkingPoints == null || meshWorkingPoints.Count == 0)
                return;
            foreach(var p in meshWorkingPoints)
            {
                if (p == null) continue;
                if (p.TryGetComponent(out Renderer r))
                    r.enabled = activeState;
            }
        }

        /// <summary>
        /// Set 'Ignore Raycast' layer to all generated points
        /// </summary>
        public void MPE_IgnoreRaycastForPoints(bool ignoreRaycast)
        {
            if (meshWorkingPoints == null || meshWorkingPoints.Count == 0)
                return;
            foreach (var p in meshWorkingPoints)
            {
                if (p == null) continue;
                p.gameObject.layer = ignoreRaycast ? 2 : 0;
            }
        }

        /// <summary>
        /// Create points/vertex editor on the current mesh
        /// </summary>
        /// <param name="ignoreVertexLimit">Notification box will popout if the vertices limit is over the constant value</param>
        public void MPE_CreatePointsEditor(bool ignoreVertexLimit = false)
        {
            if (MyMeshFilter == null || MyMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            //Don't continue if animation mode is enabled
            if (meshAnimationMode) 
                return;

            MPE_ClearPointsEditor();

            if (MyMeshFilter.sharedMesh.vertexCount > MD_GlobalPreferences.VertexLimit && !ignoreVertexLimit)
            {
                meshDeselectObjectAfterVerticeLimit = true;
                meshSelectedModification = EditorModification.Vertices;
                meshEnableZoneGenerator = true;
                meshZoneGeneratorPosition = transform.position + Vector3.one;
                return;
            }

            transform.parent = null;

            Vector3 LastScale = transform.localScale;
            Quaternion LastRotation = transform.rotation;

            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            GameObject _VertexRoot = new GameObject(name + "_VertexRoot");
            meshVertexEditor_PointsRoot = _VertexRoot.transform;
            _VertexRoot.transform.position = Vector3.zero;

            meshWorkingPoints.Clear();

            // Generating regular points
            Vector3[] vertices = MyMeshFilter.sharedMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                GameObject gm = null;

                if (meshVertexEditor_CustomPointPattern && meshVertexEditor_PointPatternObject != null)
                {
                    gm = Instantiate(meshVertexEditor_PointPatternObject);
                    if (gm.TryGetComponent(out Renderer rend) && meshVertexEditor_UseCustomColor)
                        rend.sharedMaterial.color = meshVertexEditor_CustomPointColor;
                }
                else
                {
                    Material new_Mat = new Material(MD_Utilities.MD_Specifics.GetProperPipelineDefaultShader(false));
                    new_Mat.color = meshVertexEditor_CustomPointColor;
                    gm = MDG_Octahedron.CreateGeometryAndDispose<MDG_Octahedron>();
                    gm.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                    gm.GetComponentInChildren<Renderer>().material = new_Mat;
                }
				
                if(meshVertexEditor_PointSizeMultiplier != 1)
                    gm.transform.localScale = Vector3.one * meshVertexEditor_PointSizeMultiplier;
					
                gm.transform.parent = _VertexRoot.transform;

                gm.transform.position = vertices[i];
                meshWorkingPoints.Add(gm.transform);

                gm.name = "P" + i.ToString();
            }

            // Fixing point hierarchy & naming
            int counter = 0;
            foreach (Transform onePoint in meshWorkingPoints)
            {
                if (onePoint.gameObject.activeInHierarchy == false) continue;
                foreach (Transform secondPoint in meshWorkingPoints)
                {
                    if (secondPoint.name == onePoint.name) continue;
                    if (secondPoint.transform.position == onePoint.transform.position)
                    {
                        secondPoint.hideFlags = HideFlags.HideInHierarchy;
                        secondPoint.transform.parent = onePoint.transform;
                        secondPoint.gameObject.SetActive(false);
                    }
                }
                counter++;
                onePoint.hideFlags = HideFlags.None;
                onePoint.gameObject.SetActive(true);
                onePoint.gameObject.AddComponent<SphereCollider>();
                onePoint.name = "P" + counter.ToString();
            }

            MyMeshFilter.sharedMesh.vertices = vertices;
            MyMeshFilter.sharedMesh.MarkDynamic();

            _VertexRoot.transform.parent = MyMeshFilter.transform;
            _VertexRoot.transform.localPosition = Vector3.zero;

            if (!MeshBornAsSkinnedMesh)
            {
                _VertexRoot.transform.localScale = LastScale;
                _VertexRoot.transform.rotation = LastRotation;
            }

            Internal_MPE_ReceiveMeshInfo();

#if UNITY_EDITOR
            if (meshDeselectObjectAfterVerticeLimit)
            {
                UnityEditor.Selection.activeObject = null;
                foreach (Transform p in meshWorkingPoints)
                    p.gameObject.SetActive(false);
            }
            meshDeselectObjectAfterVerticeLimit = false;
#endif
        }

        /// <summary>
        /// Clear points/vertex editor if possible
        /// </summary>
        public void MPE_ClearPointsEditor()
        {
            //Don't continue if animation mode is enabled
            if (meshAnimationMode) 
                return;

            if (meshWorkingPoints != null && meshWorkingPoints.Count > 0)
            {
                for (int i = meshWorkingPoints.Count - 1; i >= 0; i--)
                {
                    if (meshWorkingPoints[i] == null) continue;
                    if (!Application.isPlaying)
                        DestroyImmediate(meshWorkingPoints[i].gameObject);
                    else
                        Destroy(meshWorkingPoints[i].gameObject);
                }
                meshWorkingPoints.Clear();
            }

            if (meshVertexEditor_PointsRoot != null)
            {
                if(!Application.isPlaying)
                    DestroyImmediate(meshVertexEditor_PointsRoot.gameObject);
                else
                    Destroy(meshVertexEditor_PointsRoot.gameObject);
            }
        }

        /// <summary>
        /// Generate current points in the current zone-generator's distance
        /// </summary>
        public void MPE_GeneratePointsInTheZone()
        {
            if (MyMeshFilter.sharedMesh.vertexCount > 10000)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayDialog("I'm Sorry", "The mesh has too many vertices [" + MyMeshFilter.sharedMesh.vertexCount + "]. You won't be able to process this function due to possible endless freeze. [This message can be disabled in the code on your own risk & responsibility]", "OK");
#else
                MD_Debug.Debug(this, "The mesh has too many vertices [" + MyMeshFilter.sharedMesh.vertexCount + "]. You won't be able to process this function due to possible endless freeze. [This message can be disabled in the code on your own risk & responsibility]", MD_Debug.DebugType.Error);
#endif
                return;
            }

            if(meshWorkingPoints == null || meshWorkingPoints.Count == 0)
                MPE_CreatePointsEditor(true);
            for (int i = 0; i < meshWorkingPoints.Count; i++)
            {
                if (Vector3.Distance(meshWorkingPoints[i].transform.position, meshZoneGeneratorPosition) > meshZoneGeneratorRadius)
                    meshWorkingPoints[i].gameObject.SetActive(false);
                else
                    meshWorkingPoints[i].gameObject.SetActive(true);
            }
        }

        // Mesh Combine

        /// <summary>
        /// Combine all sub-meshes with the current mesh. This will create a brand new gameObject & notification will show up
        /// </summary>
        public void MPE_CombineMesh()
        {
            if (MyMeshFilter == null || MyMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

#if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                if(!UnityEditor.EditorUtility.DisplayDialog("Are you sure to combine meshes?", "If you combine the mesh with it's sub-meshes, materials and all the components will be lost. Are you sure to combine meshes? Undo won't record this process.", "Yes, proceed", "No, cancel"))
                    return;
            }
#endif
            transform.parent = null;
            Vector3 Last_POS = transform.position;
            transform.position = Vector3.zero;

            MeshFilter[] meshes_ = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combiners_ = new CombineInstance[meshes_.Length];

            int counter_ = 0;
            while (counter_ < meshes_.Length)
            {
                combiners_[counter_].mesh = meshes_[counter_].sharedMesh;
                combiners_[counter_].transform = meshes_[counter_].transform.localToWorldMatrix;
                if (meshes_[counter_].gameObject != this.gameObject)
                {
                    if (!Application.isPlaying)
                        DestroyImmediate(meshes_[counter_].gameObject);
                    else
                        Destroy(meshes_[counter_].gameObject);
                }
                counter_++;
            }

            GameObject newgm = new GameObject();
            MeshFilter f = newgm.AddComponent<MeshFilter>();
            newgm.AddComponent<MeshRenderer>();
            newgm.name = name;

            Mesh newMesh = new Mesh();
            newMesh.CombineMeshes(combiners_);

            f.sharedMesh = newMesh;
            f.sharedMesh.name = meshInfoMeshName;
            newgm.GetComponent<Renderer>().material = meshDefaultMaterial;
            newgm.AddComponent<MD_MeshProEditor>().meshInfoMeshName = meshInfoMeshName;

            newgm.transform.position = Last_POS;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Selection.activeGameObject = newgm;
                UnityEditor.EditorUtility.DisplayDialog("Successfully combined. Notice please...", "If your mesh has been successfully combined, please notice that the prefab of the 'old' mesh in the Assets Folder is no more valid for the new one. " +
                    "If you want to store the new mesh, you have to save your mesh prefab again.", "OK");
            }
#endif
            if(!Application.isPlaying)
                DestroyImmediate(this.gameObject);
            else
                Destroy(this.gameObject);
        }

        /// <summary>
        /// Combine all sub-meshes with current the mesh. This will NOT create a new gameObject & notification will not show up
        /// </summary>
        public void MPE_CombineMeshQuick()
        {
            if (MyMeshFilter == null || MyMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

            transform.parent = null;

            Vector3 Last_POS = transform.position;
            transform.position = Vector3.zero;

            MeshFilter[] meshes_ = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combiners_ = new CombineInstance[meshes_.Length];

            long counter_ = 0;
            while (counter_ < meshes_.Length)
            {
                combiners_[counter_].mesh = meshes_[counter_].sharedMesh;
                combiners_[counter_].transform = meshes_[counter_].transform.localToWorldMatrix;
                if (meshes_[counter_].gameObject != this.gameObject)
                {
                    if (!Application.isPlaying)
                        DestroyImmediate(meshes_[counter_].gameObject);
                    else
                        Destroy(meshes_[counter_].gameObject);
                }
                counter_++;
            }

            Mesh newMesh = new Mesh();
            newMesh.CombineMeshes(combiners_);

            MyMeshFilter.sharedMesh = newMesh;
            MyMeshFilter.sharedMesh.name = meshInfoMeshName;
            meshSelectedModification = EditorModification.None;

            transform.position = Last_POS;
            Internal_MPE_ReceiveMeshInfo();
        }

        // Mesh References

        /// <summary>
        /// Create a brand new object with a new mesh reference. All your components on the object will be lost!
        /// </summary>
        public void MPE_CreateNewReference()
        {
            if (MyMeshFilter == null || MyMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

            GameObject newgm = new GameObject();
            MeshFilter f = newgm.AddComponent<MeshFilter>();
            newgm.AddComponent<MeshRenderer>();
            newgm.name = name;

            Material[] Materials = GetComponent<Renderer>().sharedMaterials;

            Vector3 Last_POS = transform.position;
            transform.position = Vector3.zero;

            CombineInstance[] combine = new CombineInstance[1];
            combine[0].mesh = MyMeshFilter.sharedMesh;
            combine[0].transform = MyMeshFilter.transform.localToWorldMatrix;

            Mesh newMesh = new Mesh();
            newMesh.CombineMeshes(combine);

            f.sharedMesh = newMesh;
            newgm.SetActive(false);
            newgm.AddComponent<MD_MeshProEditor>().MeshAlreadyAwake = true;
            newgm.GetComponent<MD_MeshProEditor>().meshInfoMeshName = meshInfoMeshName;
            f.sharedMesh.name = meshInfoMeshName;

            if (Materials.Length > 0) newgm.GetComponent<Renderer>().sharedMaterials = Materials;
            newgm.transform.position = Last_POS;
            newgm.SetActive(true);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Selection.activeGameObject = newgm;
                if(MD_GlobalPreferences.PopupEditorWindow)
                     UnityEditor.EditorUtility.DisplayDialog("Notice please...", "If you change the reference of your mesh, please notice that the prefab of the 'old' mesh in Assets Folder is no more valid for the new one. " +
                    "If you would like to store a new mesh, you have to save your mesh prefab again.", "OK");
            }
#endif
            newgm.GetComponent<MD_MeshProEditor>().MyMeshFilter = f;
            newgm.GetComponent<MD_MeshProEditor>().Internal_MPE_ReceiveMeshInfo(true);

            if (!Application.isPlaying)
                DestroyImmediate(this.gameObject);
            else
                Destroy(this.gameObject);
        }

        /// <summary>
        /// Restore current mesh to its initial state
        /// </summary>
        public void MPE_RestoreMeshToOriginal()
        {
            if (MyMeshFilter == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter", MD_Debug.DebugType.Error);
                return;
            }

            if(meshInitialMesh == null)
            {
                MD_Debug.Debug(this, "Couldn't restore the original mesh data, because the initial mesh is for some reason null!", MD_Debug.DebugType.Error);
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if(meshAnimationMode)
                {
                    UnityEditor.EditorUtility.DisplayDialog("Can't continue", "Couldn't restore the original mesh data, because the Animation Mode is enabled.", "OK");
                    return;
                }
                if(!UnityEditor.EditorUtility.DisplayDialog("Are you sure?", "Are you sure to restore the original mesh data?", "Restore", "Cancel"))
                    return;
            }
#endif
            if (meshAnimationMode)
            {
                MD_Debug.Debug(this, "Couldn't restore the original mesh data, because the Animation Mode is enabled", MD_Debug.DebugType.Warning);
                return;
            }

            MPE_ClearPointsEditor();

            MyMeshFilter.sharedMesh = meshInitialMesh;
            MPE_RecalculateMesh(true);

            Internal_MPE_ReceiveMeshInfo();

            meshSelectedModification = EditorModification.None;
        }

        /// <summary>
        /// Convert skinned mesh renderer to a mesh renderer & mesh filter. This will create a new object, so none of the components will remain
        /// </summary>
        public void MPE_ConvertFromSkinnedToFilter()
        {
            SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
            if (!smr) return;
            if (smr.sharedMesh == null) return;

            GameObject newgm = new GameObject();
            MeshFilter f = newgm.AddComponent<MeshFilter>();
            newgm.AddComponent<MeshRenderer>();
            newgm.name = name + "_ConvertedMesh";

            Material[] mater = null;

            if (smr.sharedMaterials.Length > 0)
                mater = GetComponent<Renderer>().sharedMaterials;

            Vector3 Last_POS = transform.root.transform.position;
            Vector3 Last_SCA = transform.localScale;
            Quaternion Last_ROT = transform.rotation;

            transform.position = Vector3.zero;

            Mesh newMesh = smr.sharedMesh;

            f.sharedMesh = newMesh;
            f.sharedMesh.name = meshInfoMeshName;
            if (mater.Length != 0)
                newgm.GetComponent<Renderer>().sharedMaterials = mater;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Selection.activeGameObject = newgm;
                if(MD_GlobalPreferences.PopupEditorWindow)
                    UnityEditor.EditorUtility.DisplayDialog("Successfully Converted!", "Your skinned mesh renderer has been successfully converted to the Mesh Filter and Mesh Renderer.", "OK");
            }
#endif

            newgm.AddComponent<MD_MeshProEditor>().MeshBornAsSkinnedMesh = true;

            newgm.transform.position = Last_POS;
            newgm.transform.rotation = Last_ROT;
            newgm.transform.localScale = Last_SCA;

            if (!Application.isPlaying)
                DestroyImmediate(this.gameObject);
            else
                Destroy(this.gameObject);
        }

        // Internal mesh-features

        private Mesh intern_modif_sourceMesh;
        private Mesh intern_modif_workingMesh;

        /// <summary>
        /// Internal modifier - mesh smooth
        /// </summary>
        public void MPE_SmoothMesh(float intensity = 0.5f)
        {
            if (MyMeshFilter == null || MyMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

            //Mesh Smooth will not pass if the recommended vertex count is passed
            if (!MD_Utilities.MD_Specifics.CheckVertexCountLimit(MyMeshFilter.sharedMesh.vertexCount))
                return;

            intern_modif_sourceMesh = new Mesh();
            intern_modif_sourceMesh = MyMeshFilter.sharedMesh;

            Mesh clone = new Mesh();

            clone.vertices = intern_modif_sourceMesh.vertices;
            clone.normals = intern_modif_sourceMesh.normals;
            clone.tangents = intern_modif_sourceMesh.tangents;
            clone.triangles = intern_modif_sourceMesh.triangles;

            clone.uv = intern_modif_sourceMesh.uv;
            clone.uv2 = intern_modif_sourceMesh.uv2;
            clone.uv2 = intern_modif_sourceMesh.uv2;

            clone.bindposes = intern_modif_sourceMesh.bindposes;
            clone.boneWeights = intern_modif_sourceMesh.boneWeights;
            clone.bounds = intern_modif_sourceMesh.bounds;

            clone.colors = intern_modif_sourceMesh.colors;
            clone.name = intern_modif_sourceMesh.name;

            intern_modif_workingMesh = clone;
            MyMeshFilter.mesh = intern_modif_workingMesh;

            intern_modif_workingMesh.vertices = MD_Utilities.Smoothing_HCFilter.HCFilter(intern_modif_sourceMesh.vertices, intern_modif_workingMesh.triangles, 0.0f, intensity);

            Mesh m = new Mesh();

            m.name = meshInfoMeshName;
            m.vertices = MyMeshFilter.sharedMesh.vertices;
            m.triangles = MyMeshFilter.sharedMesh.triangles;
            m.uv = MyMeshFilter.sharedMesh.uv;
            m.normals = MyMeshFilter.sharedMesh.normals;

            meshWorkingPoints.Clear();

            m = intern_modif_workingMesh;

            MyMeshFilter.sharedMesh = m;

            Internal_MPE_ReceiveMeshInfo();

#if UNITY_EDITOR
            if (meshDeselectObjectAfterVerticeLimit)
                UnityEditor.Selection.activeObject = null;
            meshDeselectObjectAfterVerticeLimit = false;
#endif
        }

        /// <summary>
        /// Internal modifier - mesh subdivision
        /// </summary>
        public void MPE_SubdivideMesh(int Level)
        {
            if (MyMeshFilter == null || MyMeshFilter.sharedMesh == null)
            {
                MD_Debug.Debug(this, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                return;
            }

            MPE_ClearPointsEditor();

            //Mesh Subdivision will not pass if the recommended vertex count is passed
            if (!MD_Utilities.MD_Specifics.CheckVertexCountLimit(MyMeshFilter.sharedMesh.vertexCount))
                return;

            intern_modif_sourceMesh = new Mesh();
            intern_modif_sourceMesh = MyMeshFilter.sharedMesh;
            MD_Utilities.Mesh_Subdivision.Subdivide(intern_modif_sourceMesh, Level);
            MyMeshFilter.sharedMesh = intern_modif_sourceMesh;

            Mesh m = new Mesh();

            m.name = meshInfoMeshName;
            m.vertices = MyMeshFilter.sharedMesh.vertices;
            m.triangles = MyMeshFilter.sharedMesh.triangles;
            m.uv = MyMeshFilter.sharedMesh.uv;
            m.normals = MyMeshFilter.sharedMesh.normals;

            meshWorkingPoints.Clear();

            m = intern_modif_sourceMesh;

            MyMeshFilter.sharedMesh = m;

            Internal_MPE_ReceiveMeshInfo();

#if UNITY_EDITOR
            if (meshDeselectObjectAfterVerticeLimit)
                UnityEditor.Selection.activeObject = null;
            meshDeselectObjectAfterVerticeLimit = false;
#endif
        }

        #endregion
    }
}