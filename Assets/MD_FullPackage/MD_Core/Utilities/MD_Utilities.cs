using System;
using System.Collections.Generic;
using UnityEngine;

using MD_Package.Modifiers;

namespace MD_Package.Utilities
{
    /// <summary>
    /// MD(MeshDeformation) utilities - extended library of 3D-math related to meshes and 3D-objects
    /// </summary>
    public static class MD_Utilities
    {
        /// <summary>
        /// Specific utilities that extends the MD Package
        /// </summary>
        public static class MD_Specifics
        {
            /// <summary>
            /// Restrict gameObject from other types. This will prevent the gameObject from adding specific types
            /// </summary>
            /// <param name="sender">Sender gameObject</param>
            /// <param name="focusedType">Sender's focused type that should be kept</param>
            /// <param name="restrictedTypes">Specific types that won't be allowed to add</param>
            /// <param name="includeSelfType">Include self type? This will prevent the gameObject from adding the same type as its focused on</param>
            /// <param name="checkForBaseClass">Check for base classes?</param>
            /// <returns>Returns true if none of the types were found on the gameObject</returns>
            public static bool RestrictFromOtherTypes(GameObject sender, Type focusedType, Type[] restrictedTypes, bool includeSelfType = true, bool checkForBaseClass = true)
            {
                int selfCounter = 0;
                foreach(MonoBehaviour monos in sender.GetComponents<MonoBehaviour>())
                {
                    string t = monos.GetType().Name;
                    string tb = monos.GetType().BaseType.Name;
                    if (tb == "MonoBehaviour") tb = t;
                    if (t == focusedType.Name)
                        selfCounter++;

                    if (selfCounter >= 2 && includeSelfType)
                        return false;
                    foreach(Type rest in restrictedTypes)
                    {
                        if (rest.Name == (checkForBaseClass ? tb : t))
                        {
                            if (t == focusedType.Name)
                            {
                                if(selfCounter >= 2)
                                    return false;
                            }
                            else return false;
                        }
                    }
                }
                return true;
            }

            /// <summary>
            /// Prepare specific modifier for further use
            /// </summary>
            /// <param name="sender">Sender modifier base</param>
            /// <param name="senderFilter">Sender MeshFilter</param>
            /// <param name="restrictedTypes">Restricted monotypes - this will prevent the gameObject from adding specified types</param>
            /// <param name="checkVertCount">Check for vertex count limit?</param>
            /// <param name="meshReferenceType">Additional mesh reference type</param>
            /// <returns>Returns true if the modifier was completely and successfully setup</returns>
            public static bool PrepareMeshDeformationModifier(MD_ModifierBase sender, MeshFilter senderFilter, Type[] restrictedTypes, bool checkVertCount = true, MD_ModifierBase.MeshReferenceType meshReferenceType = MD_ModifierBase.MeshReferenceType.GetFromPreferences)
            {
                if (!RestrictFromOtherTypes(sender.gameObject, sender.GetType(), restrictedTypes))
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying && MD_GlobalPreferences.PopupEditorWindow)
                        UnityEditor.EditorUtility.DisplayDialog("Warning", "The modifier cannot be applied to this object, because the object already contains other modifiers or components that work with mesh-vertices. Please remove the existing modifiers to access the selected modifier.", "OK");
                    else
                        MD_Debug.Debug(sender, "The modifier cannot be applied to this object, because the object already contains other modifiers or components that work with mesh-vertices. Please remove the existing modifiers to access the selected modifier");
#else
                    MD_Debug.Debug(sender, "The object contains another modifier or component that work with mesh-vertices, which is prohibited. The modifier will be destroyed");
#endif
                    return false;
                }

                if (!senderFilter.sharedMesh)
                {
                    MD_Debug.Debug(sender, "Mesh Filter doesn't contain any mesh data. The modifier will be destroyed", MD_Debug.DebugType.Error);
                    return false;
                }

                if (!senderFilter.sharedMesh.isReadable)
                    MD_Debug.Debug(sender, "It's original mesh reference is not set to 'Writable/Readable'. Please stop the application, select the mesh source in the Project window and make sure the 'Readable/Writable' option is enabled", MD_Debug.DebugType.Error);

                string omName = senderFilter.sharedMesh.name;
                // This is by default set to true - it's very recommended to create a new mesh reference
                if (meshReferenceType == MD_ModifierBase.MeshReferenceType.CreateNewReference ||
                    (meshReferenceType == MD_ModifierBase.MeshReferenceType.GetFromPreferences && MD_GlobalPreferences.CreateNewReference))
                    CreateNewMeshReference(senderFilter);

                senderFilter.sharedMesh.name = omName;
                senderFilter.sharedMesh.MarkDynamic();

                if (!checkVertCount)
                    return true;

                return CheckVertexCountLimit(senderFilter.sharedMesh.vertexCount, senderFilter.gameObject);
            }

            /// <summary>
            /// Check vertex count limit. If the limitation is over the required level, the window/ debug will popout
            /// </summary>
            /// <returns>Returns true if everything is ok, returns false if vertex count is over the limitation</returns>
            public static bool CheckVertexCountLimit(int inputVertCount, GameObject sender = null)
            {
                if (inputVertCount <= MD_GlobalPreferences.VertexLimit) return true;

#if UNITY_EDITOR
                if (!Application.isPlaying && MD_GlobalPreferences.PopupEditorWindow)
                    return UnityEditor.EditorUtility.DisplayDialog("Mesh has more than " + MD_GlobalPreferences.VertexLimit.ToString() + " vertices", "Your selected mesh has more than " + MD_GlobalPreferences.VertexLimit.ToString() + " vertices [" + inputVertCount.ToString() + "]. This may slow the editor performance. Would you like to continue?", "Yes", "No");
                else
                {
                    Debug.Log($"Mesh '{(sender != null ? sender.name : null)}' has more than recommended vertices count. This may slow down the application performance");
                    return true;
                }
#else
                    Debug.Log($"Mesh '{(sender != null ? sender.name : null)}' has more than recommended vertices count. This may slow down the application performance");
                    return true;
#endif
            }

            /// <summary>
            /// Create a brand new mesh reference - instantiates an entry mesh
            /// </summary>
            /// <returns>Returns a brand new mesh reference</returns>
            public static Mesh CreateNewMeshReference(Mesh entryMesh)
            {
                return UnityEngine.Object.Instantiate(entryMesh);
            }

            /// <summary>
            /// Create a brand new mesh reference, does some additional work (if shared mesh exists) - Safer way
            /// </summary>
            public static void CreateNewMeshReference(MeshFilter entryMeshFilter)
            {
                if (entryMeshFilter == null)
                {
                    MD_Debug.Debug(null, "Creating a new mesh reference was unsuccessful. The object entry was empty!");
                    return;
                }
                if (entryMeshFilter.sharedMesh == null)
                {
                    MD_Debug.Debug(null, "Creating a new mesh reference of object " + entryMeshFilter.name + " was unsuccessful. The shared mesh was empty!");
                    return;
                }
                entryMeshFilter.sharedMesh = CreateNewMeshReference(entryMeshFilter.sharedMesh);
            }

            /// <summary>
            /// Returns a proper pipeline default shader - URP returns URP/Unlit etc
            /// </summary>
            public static Shader GetProperPipelineDefaultShader(bool lit = true, string standardUnlitCustomKeyword = "")
            {
                string shader = lit ? "Standard" : (string.IsNullOrEmpty(standardUnlitCustomKeyword) ? "Unlit/Color" : standardUnlitCustomKeyword);
                string fallback = shader;

                var pn = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                if (pn == null)
                    return Shader.Find(shader);
                string pname = pn.name;
                shader = "";
                if (pname.Contains("Universal") || pname.Contains("URP"))
                    shader = "Universal Render Pipeline/" + (lit ? "Lit" : "Unlit");
                else if (pname.Contains("HDRP") || pname.Contains("HDRender"))
                    shader = "HDRP/" + (lit ? "Lit" : "Unlit");
                Shader s = Shader.Find(shader);
                if (s) return s;
                MD_Debug.Debug(null, "The scriptable render pipeline couldn't be recognized. Default built-in Standard shader has been returned");
                return Shader.Find(fallback);
            }

#if UNITY_EDITOR
            /// <summary>
            /// Save mesh to assets (Editor only)
            /// </summary>
            public static void SaveMeshToTheAssetsDatabase(MeshFilter meshFilter)
            {
                if (meshFilter == null || (meshFilter && meshFilter.sharedMesh == null))
                {
                    MD_Debug.Debug(null, "The object doesn't contain Mesh Filter or shared mesh is empty", MD_Debug.DebugType.Error);
                    return;
                }

                string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Please enter the path to save your Mesh to the Assets as Prefab", meshFilter.sharedMesh.name, "asset", "Please enter path");

                if (string.IsNullOrEmpty(path))
                    return;

                string UniquePath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(path);
                try
                {
                    UnityEditor.AssetDatabase.CreateAsset(meshFilter.sharedMesh, UniquePath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();

                    MD_Debug.Debug(null, "Mesh has been successfully saved to: " + path, MD_Debug.DebugType.Information);
                }
                catch (UnityException e)
                {
                    Debug.LogError(e.Message + ", (Error occured while saving the asset...) Unique Path: " + UniquePath);
                }
            }
#endif
        }

        /// <summary>
        /// Mesh smoothing - Laplacian filter method
        /// </summary>
        public static class Smoothing_LaplacianFilter
        {
            /// <summary>
            /// Returns smooth calculation of Laplacian method [Calculates entire mesh]
            /// </summary>
            /// <param name="mesh">Input mesh</param>
            /// <returns>Returns calculated mesh</returns>
            public static Mesh LaplacianFilter(Mesh mesh, float intensity, bool recalculateNormals = true)
            {
                mesh.vertices = LaplacianFilter(mesh.vertices, mesh.triangles, intensity);
                if (recalculateNormals) mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }

            /// <summary>
            /// Returns smooth calculation of Laplacian method [Calculates specific vertices]
            /// </summary>
            /// <param name="vertices">Input vertices</param>
            /// <param name="triangles">Input triangles</param>
            /// <param name="times">Smoothing multiplier</param>
            /// <returns>Returns array of calculated vertices</returns>
            public static Vector3[] LaplacianFilter(Vector3[] vertices, int[] triangles, float intensity)
            {
                var network = VertBranch.BuildNetwork(triangles);
                vertices = LaplacianFilter(network, vertices, intensity, new MDM_SculptingLite.SculptingCrossData() { NotInitialized = true });
                return vertices;
            }

            /// <summary>
            /// Returns smooth calculation of Laplacian method [Calculates specific vertices] specifically for sculpting
            /// </summary>
            /// <returns>Returns array of calculated vertices</returns>
            public static Vector3[] LaplacianFilter(Vector3[] vertices, int[] triangles, float intensity, MDM_SculptingLite.SculptingCrossData atr)
            {
                var network = VertBranch.BuildNetwork(triangles);
                vertices = LaplacianFilter(network, vertices, intensity, atr);
                return vertices;
            }

            private static Vector3[] LaplacianFilter(Dictionary<int, VertBranch> network, Vector3[] origin, float intensity, MDM_SculptingLite.SculptingCrossData atr)
            {
                intensity = Mathf.Clamp01(intensity);
                Vector3[] vertices = new Vector3[origin.Length];
                for (int i = 0, n = origin.Length; i < n; i++)
                {
                    if (!atr.NotInitialized)
                    {
                        Vector3 v0 = Transformations.TransformPoint(atr.transPos, atr.transRot, atr.transScale, origin[i]);
                        Vector3 v1 = atr.worldPoint;
                        if (Vector3.Distance(v0, v1) > atr.radius)
                        {
                            vertices[i] = origin[i];
                            continue;
                        }
                    }

                    HashSet<int> connection = network[i].Connection;
                    Vector3 v = vertices[i];
                    foreach (int adj in connection) v += origin[adj];
                    vertices[i] = Vector3.Lerp(origin[i], (v / connection.Count), intensity); //Finalize weight calculation by dividing the total count of connections
                }
                return vertices;
            }

            public sealed class VertBranch
            {
                public HashSet<int> Connection { get; }
                public VertBranch()
                {
                    this.Connection = new HashSet<int>();
                }

                public void Connect(int to)
                {
                    Connection.Add(to);
                }
                public static Dictionary<int, VertBranch> BuildNetwork(int[] triangles)
                {
                    var table = new Dictionary<int, VertBranch>();
                    for (int i = 0, n = triangles.Length; i < n; i += 3)
                    {
                        int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                        if (!table.ContainsKey(a)) table.Add(a, new VertBranch());
                        if (!table.ContainsKey(b)) table.Add(b, new VertBranch());
                        if (!table.ContainsKey(c)) table.Add(c, new VertBranch());
                        table[a].Connect(b); table[a].Connect(c);
                        table[b].Connect(a); table[b].Connect(c);
                        table[c].Connect(a); table[c].Connect(b);
                    }
                    return table;
                }
            }
        }

        /// <summary>
        /// Mesh smoothing - Humphrey's [HC] filter method
        /// </summary>
        public static class Smoothing_HCFilter
        {
            /// <summary>
            /// Returns smooth calculation of HC Filter method [Calculates vertices] specifically for sculpting
            /// </summary>
            /// <returns>Returns calculated vertices</returns>
            public static Vector3[] HCFilter(Vector3[] inVerts, int[] inTris, MDM_SculptingLite.SculptingCrossData atr, float alpha = 0.8f, float beta = 0.94f)
            {
                Vector3[] originalVerts = new Vector3[inVerts.Length];
                Vector3[] workingVerts = GetWeightedVertices(inVerts, inTris, atr);

                for (int i = 0; i < workingVerts.Length; i++)
                {
                    originalVerts[i].x = workingVerts[i].x - (alpha * inVerts[i].x + (1 - alpha) * inVerts[i].x);
                    originalVerts[i].y = workingVerts[i].y - (alpha * inVerts[i].y + (1 - alpha) * inVerts[i].y);
                    originalVerts[i].z = workingVerts[i].z - (alpha * inVerts[i].z + (1 - alpha) * inVerts[i].z);
                }

                float dx;
                float dy;
                float dz;
                for (int j = 0; j < originalVerts.Length; j++)
                {
                    Vector3 v0 = Transformations.TransformPoint(atr.transPos, atr.transRot, atr.transScale, originalVerts[j]);
                    Vector3 v1 = atr.worldPoint;
                    if (Vector3.Distance(v0, v1) > atr.radius)
                        continue;

                    List<int> AdjIndex = FindAdjTris(inVerts, inTris, inVerts[j], atr);

                    dx = 0.0f;
                    dy = 0.0f;
                    dz = 0.0f;

                    for (int k = 0; k < AdjIndex.Count; k++)
                    {
                        dx += originalVerts[AdjIndex[k]].x;
                        dy += originalVerts[AdjIndex[k]].y;
                        dz += originalVerts[AdjIndex[k]].z;
                    }

                    workingVerts[j].x -= beta * originalVerts[j].x + ((1 - beta) / AdjIndex.Count) * dx;
                    workingVerts[j].y -= beta * originalVerts[j].y + ((1 - beta) / AdjIndex.Count) * dy;
                    workingVerts[j].z -= beta * originalVerts[j].z + ((1 - beta) / AdjIndex.Count) * dz;
                }
                return workingVerts;
            }

            /// <summary>
            /// Returns smooth calculation of HC Filter method [Calculates vertices]
            /// </summary>
            /// <returns>Returns calculated vertices</returns>
            public static Vector3[] HCFilter(Vector3[] inVerts, int[] inTris, float alpha = 0.8f, float beta = 0.94f)
            {
                Vector3[] originalVerts = new Vector3[inVerts.Length];
                Vector3[] workingVerts = GetWeightedVertices(inVerts, inTris, new MDM_SculptingLite.SculptingCrossData() { NotInitialized = true });

                for (int i = 0; i < workingVerts.Length; i++)
                {
                    originalVerts[i].x = workingVerts[i].x - (alpha * inVerts[i].x + (1 - alpha) * inVerts[i].x);
                    originalVerts[i].y = workingVerts[i].y - (alpha * inVerts[i].y + (1 - alpha) * inVerts[i].y);
                    originalVerts[i].z = workingVerts[i].z - (alpha * inVerts[i].z + (1 - alpha) * inVerts[i].z);
                }

                float dx;
                float dy;
                float dz;
                for (int j = 0; j < originalVerts.Length; j++)
                {
                    List<int> AdjIndex = FindAdjTris(inVerts, inTris, inVerts[j], new MDM_SculptingLite.SculptingCrossData() { NotInitialized = true });

                    dx = 0.0f;
                    dy = 0.0f;
                    dz = 0.0f;

                    for (int k = 0; k < AdjIndex.Count; k++)
                    {
                        dx += originalVerts[AdjIndex[k]].x;
                        dy += originalVerts[AdjIndex[k]].y;
                        dz += originalVerts[AdjIndex[k]].z;
                    }

                    workingVerts[j].x -= beta * originalVerts[j].x + ((1 - beta) / AdjIndex.Count) * dx;
                    workingVerts[j].y -= beta * originalVerts[j].y + ((1 - beta) / AdjIndex.Count) * dy;
                    workingVerts[j].z -= beta * originalVerts[j].z + ((1 - beta) / AdjIndex.Count) * dz;
                }
                return workingVerts;
            }

            private static Vector3[] GetWeightedVertices(Vector3[] sv, int[] t, MDM_SculptingLite.SculptingCrossData atr)
            {
                Vector3[] verts = new Vector3[sv.Length];

                float dx;
                float dy;
                float dz;

                for (int vi = 0; vi < sv.Length; vi++)
                {
                    if (!atr.NotInitialized)
                    {
                        Vector3 v0 = Transformations.TransformPoint(atr.transPos, atr.transRot, atr.transScale, sv[vi]);
                        Vector3 v1 = atr.worldPoint;
                        if (Vector3.Distance(v0, v1) > atr.radius)
                        { 
                            verts[vi] = sv[vi]; 
                            continue; 
                        }
                    }

                    List<Vector3> adjVerts = FindAdjVerts(sv, t, sv[vi], atr);

                    if (adjVerts.Count == 0) continue;

                    dx = 0.0f;
                    dy = 0.0f;
                    dz = 0.0f;

                    for (int j = 0; j < adjVerts.Count; j++)
                    {
                        dx += adjVerts[j].x;
                        dy += adjVerts[j].y;
                        dz += adjVerts[j].z;
                    }

                    verts[vi].x = dx / adjVerts.Count;
                    verts[vi].y = dy / adjVerts.Count;
                    verts[vi].z = dz / adjVerts.Count;
                }

                return verts;
            }

            private static List<Vector3> FindAdjVerts(Vector3[] v, int[] t, Vector3 vertex, MDM_SculptingLite.SculptingCrossData atr)
            {
                List<Vector3> Vertex = new List<Vector3>();
                HashSet<int> FaceCreator = new HashSet<int>();
                int FaceLength = 0;

                for (int i = 0; i < v.Length; i++)
                {
                    if (!atr.NotInitialized)
                    {
                        Vector3 v0 = Transformations.TransformPoint(atr.transPos, atr.transRot, atr.transScale, v[i]);
                        Vector3 v1 = atr.worldPoint;
                        if (Vector3.Distance(v0, v1) > atr.radius)
                            continue;
                    }

                    if (Mathf.Approximately(vertex.x, v[i].x) &&
                        Mathf.Approximately(vertex.y, v[i].y) &&
                        Mathf.Approximately(vertex.z, v[i].z))
                    {
                        int v1;
                        int v2;
                        bool marker;

                        for (int k = 0; k < t.Length; k = k + 3)
                        {
                            if (FaceCreator.Contains(k) == false)
                            {
                                v1 = 0;
                                v2 = 0;
                                marker = false;

                                if (i == t[k])
                                {
                                    v1 = t[k + 1];
                                    v2 = t[k + 2];
                                    marker = true;
                                }

                                if (i == t[k + 1])
                                {
                                    v1 = t[k];
                                    v2 = t[k + 2];
                                    marker = true;
                                }

                                if (i == t[k + 2])
                                {
                                    v1 = t[k];
                                    v2 = t[k + 1];
                                    marker = true;
                                }

                                FaceLength++;
                                if (marker)
                                {
                                    FaceCreator.Add(k);
                                    if (VertExist(Vertex, v[v1]) == false) Vertex.Add(v[v1]);
                                    if (VertExist(Vertex, v[v2]) == false) Vertex.Add(v[v2]);
                                }
                            }
                        }
                    }
                }

                return Vertex;
            }

            private static List<int> FindAdjTris(Vector3[] v, int[] t, Vector3 vertex, MDM_SculptingLite.SculptingCrossData atr)
            {
                List<int> AdjIndex = new List<int>();
                List<Vector3> AdjVertex = new List<Vector3>();
                HashSet<int> AdjFace = new HashSet<int>();
                int FaceLength = 0;

                for (int i = 0; i < v.Length; i++)
                {
                    if (!atr.NotInitialized)
                    {
                        Vector3 vv0 = Transformations.TransformPoint(atr.transPos, atr.transRot, atr.transScale, v[i]);
                        Vector3 vv1 = atr.worldPoint;
                        if (Vector3.Distance(vv0, vv1) > atr.radius)
                            continue;
                    }

                    if (Mathf.Approximately(vertex.x, v[i].x) &&
                        Mathf.Approximately(vertex.y, v[i].y) &&
                        Mathf.Approximately(vertex.z, v[i].z))
                    {
                        int v1;
                        int v2;
                        bool marker;

                        for (int k = 0; k < t.Length; k = k + 3)
                            if (AdjFace.Contains(k) == false)
                            {
                                v1 = 0;
                                v2 = 0;
                                marker = false;

                                if (i == t[k])
                                {
                                    v1 = t[k + 1];
                                    v2 = t[k + 2];
                                    marker = true;
                                }

                                if (i == t[k + 1])
                                {
                                    v1 = t[k];
                                    v2 = t[k + 2];
                                    marker = true;
                                }

                                if (i == t[k + 2])
                                {
                                    v1 = t[k];
                                    v2 = t[k + 1];
                                    marker = true;
                                }

                                FaceLength++;
                                if (marker)
                                {
                                    AdjFace.Add(k);

                                    if (VertExist(AdjVertex, v[v1]) == false)
                                    {
                                        AdjVertex.Add(v[v1]);
                                        AdjIndex.Add(v1);
                                    }

                                    if (VertExist(AdjVertex, v[v2]) == false)
                                    {
                                        AdjVertex.Add(v[v2]);
                                        AdjIndex.Add(v2);
                                    }
                                }
                            }
                    }
                }

                return AdjIndex;
            }

            private static bool VertExist(List<Vector3> adjVert, Vector3 v)
            {
                for (int i = 0; i < adjVert.Count; i++)
                {
                    if (Mathf.Approximately(adjVert[i].x, v.x) && Mathf.Approximately(adjVert[i].y, v.y) && Mathf.Approximately(adjVert[i].z, v.z)) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Mesh subdivision - populate mesh with vertices
        /// </summary>
        public static class Mesh_Subdivision
        {
            private static List<Vector3> vertices;
            private static List<Vector3> normals;
            private static List<Color> colors;
            private static List<Vector2> uv;
            private static List<Vector2> uv2;
            private static List<Vector2> uv3;

            private static List<int> indices;
            private static Dictionary<uint, int> newVectices;

            /// <summary>
            /// Main subdivision; Subdivision levels = 2, 3, 4, 6, 8, 9, 12, 16, 18, 24.
            /// </summary>
            public static void Subdivide(Mesh mesh, int level)
            {
                level = Mathf.Max(2, Mathf.Min(24, level));

                while (level > 1)
                {
                    while (level % 3 == 0)
                    {
                        Mode_Subdivide2(mesh);
                        level /= 3;
                    }
                    while (level % 2 == 0)
                    {
                        Mode_Subdivide(mesh);
                        level /= 2;
                    }
                    if (level > 3) level++;
                }
            }

            private static void Clean()
            {
                vertices = null;
                normals = null;
                colors = null;
                uv = null;
                uv2 = null;
                uv3 = null;
                indices = null;
            }

            private static void InitArrays(Mesh mesh)
            {
                vertices = new List<Vector3>(mesh.vertices);
                normals = new List<Vector3>(mesh.normals);
                colors = new List<Color>(mesh.colors);
                uv = new List<Vector2>(mesh.uv);
                uv2 = new List<Vector2>(mesh.uv2);
                uv3 = new List<Vector2>(mesh.uv3);
                indices = new List<int>();
            }

            private static int GetNewVertex4(int i1, int i2)
            {
                int newIndex = vertices.Count;
                uint t1 = ((uint)i1 << 16) | (uint)i2;
                uint t2 = ((uint)i2 << 16) | (uint)i1;
                if (newVectices.ContainsKey(t2))
                    return newVectices[t2];
                if (newVectices.ContainsKey(t1))
                    return newVectices[t1];

                newVectices.Add(t1, newIndex);

                vertices.Add((vertices[i1] + vertices[i2]) * 0.5f);
                if (normals.Count > 0)
                    normals.Add((normals[i1] + normals[i2]).normalized);
                if (colors.Count > 0)
                    colors.Add((colors[i1] + colors[i2]) * 0.5f);
                if (uv.Count > 0)
                    uv.Add((uv[i1] + uv[i2]) * 0.5f);
                if (uv2.Count > 0)
                    uv2.Add((uv2[i1] + uv2[i2]) * 0.5f);
                if (uv3.Count > 0)
                    uv3.Add((uv3[i1] + uv3[i2]) * 0.5f);

                return newIndex;
            }

            private static void Mode_Subdivide(Mesh mesh)
            {
                newVectices = new Dictionary<uint, int>();

                InitArrays(mesh);

                int[] triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i1 = triangles[i + 0];
                    int i2 = triangles[i + 1];
                    int i3 = triangles[i + 2];

                    int a = GetNewVertex4(i1, i2);
                    int b = GetNewVertex4(i2, i3);
                    int c = GetNewVertex4(i3, i1);
                    indices.Add(i1); indices.Add(a); indices.Add(c);
                    indices.Add(i2); indices.Add(b); indices.Add(a);
                    indices.Add(i3); indices.Add(c); indices.Add(b);
                    indices.Add(a); indices.Add(b); indices.Add(c); // center triangle
                }
                mesh.vertices = vertices.ToArray();
                if (normals.Count > 0)
                    mesh.normals = normals.ToArray();
                if (colors.Count > 0)
                    mesh.colors = colors.ToArray();
                if (uv.Count > 0)
                    mesh.uv = uv.ToArray();
                if (uv2.Count > 0)
                    mesh.uv2 = uv2.ToArray();
                if (uv3.Count > 0)
                    mesh.uv3 = uv3.ToArray();

                mesh.triangles = indices.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                Clean();
            }

            private static int GetVert(int i1, int i2, int i3)
            {
                int newIndex = vertices.Count;

                if (i3 == i1 || i3 == i2)
                {
                    uint t1 = ((uint)i1 << 16) | (uint)i2;
                    if (newVectices.ContainsKey(t1))
                        return newVectices[t1];
                    newVectices.Add(t1, newIndex);
                }

                vertices.Add((vertices[i1] + vertices[i2] + vertices[i3]) / 3.0f);
                if (normals.Count > 0)
                    normals.Add((normals[i1] + normals[i2] + normals[i3]).normalized);
                if (colors.Count > 0)
                    colors.Add((colors[i1] + colors[i2] + colors[i3]) / 3.0f);
                if (uv.Count > 0)
                    uv.Add((uv[i1] + uv[i2] + uv[i3]) / 3.0f);
                if (uv2.Count > 0)
                    uv2.Add((uv2[i1] + uv2[i2] + uv2[i3]) / 3.0f);
                if (uv3.Count > 0)
                    uv3.Add((uv3[i1] + uv3[i2] + uv3[i3]) / 3.0f);
                return newIndex;
            }

            private static void Mode_Subdivide2(Mesh mesh)
            {
                newVectices = new Dictionary<uint, int>();

                InitArrays(mesh);

                int[] triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i1 = triangles[i + 0];
                    int i2 = triangles[i + 1];
                    int i3 = triangles[i + 2];

                    int a1 = GetVert(i1, i2, i1);
                    int a2 = GetVert(i2, i1, i2);
                    int b1 = GetVert(i2, i3, i2);
                    int b2 = GetVert(i3, i2, i3);
                    int c1 = GetVert(i3, i1, i3);
                    int c2 = GetVert(i1, i3, i1);

                    int d = GetVert(i1, i2, i3);

                    indices.Add(i1); indices.Add(a1); indices.Add(c2);
                    indices.Add(i2); indices.Add(b1); indices.Add(a2);
                    indices.Add(i3); indices.Add(c1); indices.Add(b2);
                    indices.Add(d); indices.Add(a1); indices.Add(a2);
                    indices.Add(d); indices.Add(b1); indices.Add(b2);
                    indices.Add(d); indices.Add(c1); indices.Add(c2);
                    indices.Add(d); indices.Add(c2); indices.Add(a1);
                    indices.Add(d); indices.Add(a2); indices.Add(b1);
                    indices.Add(d); indices.Add(b2); indices.Add(c1);
                }

                mesh.vertices = vertices.ToArray();
                if (normals.Count > 0)
                    mesh.normals = normals.ToArray();
                if (colors.Count > 0)
                    mesh.colors = colors.ToArray();
                if (uv.Count > 0)
                    mesh.uv = uv.ToArray();
                if (uv2.Count > 0)
                    mesh.uv2 = uv2.ToArray();
                if (uv3.Count > 0)
                    mesh.uv3 = uv3.ToArray();

                mesh.triangles = indices.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();

                Clean();
            }
        }

        /// <summary>
        /// Mesh normals recalculation - "Alternative technique" based on angles
        /// </summary>
        public static class AlternativeNormals
        {
            public struct NormalData
            {
                public Vector3[] vertices;
                public int[] triangles;
                public Vector3[] normals;
            }

            /// <summary>
            /// Recalculate normals in "an alternative way" - requires specific angle. 90 degrees is a default value. Use 0 degrees for flat smoothing
            /// </summary>
            public static void RecalculateNormals(Mesh mesh, float angle = 90)
            {
                NormalData nd = new NormalData() { normals = mesh.normals, triangles = mesh.triangles, vertices = mesh.vertices };
                mesh.normals = RecalculateNormals(nd, angle);
            }

            /// <summary>
            /// Recalculate normals in "an alternative way" - requires specific angle. 90 degrees is a default value. Use 0 degrees for flat smoothing. Returns recalculated normals array
            /// </summary>
            public static Vector3[] RecalculateNormals(NormalData nd, float angle = 90)
            {
                float cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

                Vector3[] vertices = nd.vertices;
                Vector3[] normals = new Vector3[vertices.Length];
                int[] triangles = nd.triangles;

                Vector3[][] triNormals = new Vector3[1][];
                Dictionary<VertexKey, List<VertexEntry>> dictionary = new Dictionary<VertexKey, List<VertexEntry>>(vertices.Length);

                triNormals[0] = new Vector3[triangles.Length / 3];

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i1 = triangles[i];
                    int i2 = triangles[i + 1];
                    int i3 = triangles[i + 2];

                    Vector3 p1 = vertices[i2] - vertices[i1];
                    Vector3 p2 = vertices[i3] - vertices[i1];
                    Vector3 normal = Vector3.Cross(p1, p2).normalized;
                    int triIndex = i / 3;
                    triNormals[0][triIndex] = normal;

                    VertexKey key;

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i1]), out List<VertexEntry> entry))
                    {
                        entry = new List<VertexEntry>(4);
                        dictionary.Add(key, entry);
                    }
                    entry.Add(new VertexEntry(0, triIndex, i1));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i2]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }
                    entry.Add(new VertexEntry(0, triIndex, i2));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i3]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }
                    entry.Add(new VertexEntry(0, triIndex, i3));
                }

                foreach (var vertList in dictionary.Values)
                {
                    for (int i = 0; i < vertList.Count; ++i)
                    {
                        Vector3 sum = new Vector3();
                        VertexEntry lhsEntry = vertList[i];

                        for (int j = 0; j < vertList.Count; ++j)
                        {
                            VertexEntry rhsEntry = vertList[j];
                            if (lhsEntry.VertexIndex == rhsEntry.VertexIndex)
                                sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                            else
                            {
                                var dot = Vector3.Dot(
                                    triNormals[lhsEntry.MeshIndex][lhsEntry.TriangleIndex],
                                    triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                                if (dot >= cosineThreshold)
                                    sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                            }
                        }
                        normals[lhsEntry.VertexIndex] = sum.normalized;
                    }
                }

                return normals;
            }

            private struct VertexKey
            {
                private readonly long _x;
                private readonly long _y;
                private readonly long _z;

                // Change this if you require a different precision.
                private const int Tolerance = 100000;

                // Magic FNV values. Do not change these.
                private const long FNV32Init = 0x811c9dc5;
                private const long FNV32Prime = 0x01000193;

                public VertexKey(Vector3 position)
                {
                    _x = (long)(Mathf.Round(position.x * Tolerance));
                    _y = (long)(Mathf.Round(position.y * Tolerance));
                    _z = (long)(Mathf.Round(position.z * Tolerance));
                }

                public override bool Equals(object obj)
                {
                    var key = (VertexKey)obj;
                    return _x == key._x && _y == key._y && _z == key._z;
                }

                public override int GetHashCode()
                {
                    long rv = FNV32Init;
                    rv ^= _x;
                    rv *= FNV32Prime;
                    rv ^= _y;
                    rv *= FNV32Prime;
                    rv ^= _z;
                    rv *= FNV32Prime;
                    return rv.GetHashCode();
                }
            }

            private struct VertexEntry
            {
                public int MeshIndex;
                public int TriangleIndex;
                public int VertexIndex;

                public VertexEntry(int meshIndex, int triIndex, int vertIndex)
                {
                    MeshIndex = meshIndex;
                    TriangleIndex = triIndex;
                    VertexIndex = vertIndex;
                }
            }
        }

        /// <summary>
        /// Various global transformation helpers from world to local space and vice-versa
        /// </summary>
        public static class Transformations
        {
            /// <summary>
            /// Convert point from local space to world space
            /// </summary>
            public static Vector3 TransformPoint(Vector3 WorldPos, Quaternion WorldRot, Vector3 WorldScale, Vector3 Point)
            {
                var localToWorldMatrix = Matrix4x4.TRS(WorldPos, WorldRot, WorldScale);
                return localToWorldMatrix.MultiplyPoint3x4(Point);
            }

            /// <summary>
            /// Convert point from world space to local space
            /// </summary>
            public static Vector3 TransformPointInverse(Vector3 WorldPos, Quaternion WorldRot, Vector3 WorldScale, Vector3 Point)
            {
                var localToWorldMatrix = Matrix4x4.TRS(WorldPos, WorldRot, WorldScale).inverse;
                return localToWorldMatrix.MultiplyPoint3x4(Point);
            }

            /// <summary>
            /// Convert point direction from local space to world space
            /// </summary>
            public static Vector3 TransformDirection(Vector3 WorldPos, Quaternion WorldRot, Vector3 WorldScale, Vector3 Point)
            {
                var localToWorldMatrix = Matrix4x4.TRS(WorldPos, WorldRot, WorldScale);
                return localToWorldMatrix.MultiplyVector(Point);
            }
        }

        /// <summary>
        /// Custom 3D math library focused on linear algebra and interpolations
        /// </summary>
        public static class Math3D
        {
            /// <summary>
            /// Custom linear interpolation between A to B
            /// </summary>
            public static Vector3 CustomLerp(Vector3 a, Vector3 b, float t)
            {
                return ((1 - t) * a + t * b);
            }

            /// <summary>
            /// Procedural perlin noise - create an instance
            /// </summary>
            public sealed class Perlin
            {
                private const int B = 0x100;
                private const int BM = 0xff;
                private const int N = 0x1000;

                private readonly int[] p = new int[B + B + 2];
                private readonly float[,] g3 = new float[B + B + 2, 3];
                private readonly float[,] g2 = new float[B + B + 2, 2];
                private readonly float[] g1 = new float[B + B + 2];

                private float Scurve(float t)
                {
                    return t * t * (3.0F - 2.0F * t);
                }

                private float Lerp(float t, float a, float b)
                {
                    return a + t * (b - a);
                }

                private void Setup(float value, out int b0, out int b1, out float r0, out float r1)
                {
                    float t = value + N;
                    b0 = ((int)t) & BM;
                    b1 = (b0 + 1) & BM;
                    r0 = t - (int)t;
                    r1 = r0 - 1.0F;
                }

                private float At3(float rx, float ry, float rz, float x, float y, float z) { return rx * x + ry * y + rz * z; }

                public float Noise(float x, float y, float z)
                {
                    int bx0, bx1, by0, by1, bz0, bz1, b00, b10, b01, b11;
                    float rx0, rx1, ry0, ry1, rz0, rz1, sy, sz, a, b, c, d, t, u, v;
                    int i, j;

                    Setup(x, out bx0, out bx1, out rx0, out rx1);
                    Setup(y, out by0, out by1, out ry0, out ry1);
                    Setup(z, out bz0, out bz1, out rz0, out rz1);

                    i = p[bx0];
                    j = p[bx1];

                    b00 = p[i + by0];
                    b10 = p[j + by0];
                    b01 = p[i + by1];
                    b11 = p[j + by1];

                    t = Scurve(rx0);
                    sy = Scurve(ry0);
                    sz = Scurve(rz0);

                    u = At3(rx0, ry0, rz0, g3[b00 + bz0, 0], g3[b00 + bz0, 1], g3[b00 + bz0, 2]);
                    v = At3(rx1, ry0, rz0, g3[b10 + bz0, 0], g3[b10 + bz0, 1], g3[b10 + bz0, 2]);
                    a = Lerp(t, u, v);

                    u = At3(rx0, ry1, rz0, g3[b01 + bz0, 0], g3[b01 + bz0, 1], g3[b01 + bz0, 2]);
                    v = At3(rx1, ry1, rz0, g3[b11 + bz0, 0], g3[b11 + bz0, 1], g3[b11 + bz0, 2]);
                    b = Lerp(t, u, v);

                    c = Lerp(sy, a, b);

                    u = At3(rx0, ry0, rz1, g3[b00 + bz1, 0], g3[b00 + bz1, 2], g3[b00 + bz1, 2]);
                    v = At3(rx1, ry0, rz1, g3[b10 + bz1, 0], g3[b10 + bz1, 1], g3[b10 + bz1, 2]);
                    a = Lerp(t, u, v);

                    u = At3(rx0, ry1, rz1, g3[b01 + bz1, 0], g3[b01 + bz1, 1], g3[b01 + bz1, 2]);
                    v = At3(rx1, ry1, rz1, g3[b11 + bz1, 0], g3[b11 + bz1, 1], g3[b11 + bz1, 2]);
                    b = Lerp(t, u, v);

                    d = Lerp(sy, a, b);

                    return Lerp(sz, c, d);
                }

                private void Normalize2(ref float x, ref float y)
                {
                    float s;
                    s = (float)Math.Sqrt(x * x + y * y);
                    x = y / s;
                    y = y / s;
                }

                private void Normalize3(ref float x, ref float y, ref float z)
                {
                    float s;
                    s = (float)Math.Sqrt(x * x + y * y + z * z);
                    x = y / s;
                    y = y / s;
                    z = z / s;
                }

                public Perlin()
                {
                    int i, j, k;
                    System.Random rnd = new System.Random();

                    for (i = 0; i < B; i++)
                    {
                        p[i] = i;
                        g1[i] = (float)(rnd.Next(B + B) - B) / B;

                        for (j = 0; j < 2; j++)
                            g2[i, j] = (float)(rnd.Next(B + B) - B) / B;
                        Normalize2(ref g2[i, 0], ref g2[i, 1]);

                        for (j = 0; j < 3; j++)
                            g3[i, j] = (float)(rnd.Next(B + B) - B) / B;

                        Normalize3(ref g3[i, 0], ref g3[i, 1], ref g3[i, 2]);
                    }

                    while (--i != 0)
                    {
                        k = p[i];
                        p[i] = p[j = rnd.Next(B)];
                        p[j] = k;
                    }

                    for (i = 0; i < B + 2; i++)
                    {
                        p[B + i] = p[i];
                        g1[B + i] = g1[i];
                        for (j = 0; j < 2; j++)
                            g2[B + i, j] = g2[i, j];
                        for (j = 0; j < 3; j++)
                            g3[B + i, j] = g3[i, j];
                    }
                }
            }
        }
	}
}