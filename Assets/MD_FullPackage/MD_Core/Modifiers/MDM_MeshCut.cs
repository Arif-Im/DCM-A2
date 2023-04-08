using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System;

#if UNITY_EDITOR
using UnityEditor;

using MD_Package;
using MD_Package.Modifiers;
#endif

namespace MD_Package.Modifiers
{
    /// <summary>
    /// MDM(Mesh Deformation Modifier): Mesh Cut.
    /// Simple, straight-two-sided mesh cut solution with custom cut settings and events.
    /// Written by Matej Vanco (2022, updated in 2023), mesh cutter source written by hugoscurti (MIT License - Github).
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu(MD_Debug.ORGANISATION + MD_Debug.PACKAGENAME + "Modifiers/Mesh Cut")]
    public sealed class MDM_MeshCut : MD_ModifierBase
    {
        public MDM_MeshCut_Cutter cutterSource;

        public bool addRigidbodyAfterCut = false;
        public float separationForce = 2.0f;
        public float defaultMass = 1.0f;
        public bool addSeparationOffset = false;
        public float separationOffset = 0.05f;

        public bool addMeshColliderAfterCut = false;

        public bool automaticallyCut = false;
        [Range(0.05f, 10.0f)] public float automaticallyCutDelay = 0.5f;

        private MeshCut_Src.MeshCut_TempMesh meshA, meshB;
        private Plane splane;
        private MeshCut_Src mc;
        public MeshCollider MyMeshCollider { get; private set; }
        public readonly List<(MeshFilter f, MeshCollider c)> createdChunks = new List<(MeshFilter f, MeshCollider c)>();

        /// <summary>
        /// Custom OnCut event - when mesh gets cut - list of transforms contain a created chunks from the cut
        /// </summary>
        public event Action<List<Transform>> Event_GotCut;
        private readonly List<Transform> affectedChunks = new List<Transform>();

        /// <summary>
        /// What will happen when the mesh got cut?
        /// </summary>
        public UnityEvent UEvent_OnCut;

        /// <summary>
        /// When the component is added to an object (called once)
        /// </summary>
        private void Reset()
        {
            if (MbIsInitialized)
                return;
            MDModifier_InitializeBase();
        }

        #region Base overrides

        /// <summary>
        /// Base modifier initialization
        /// </summary>
        protected override void MDModifier_InitializeBase(MeshReferenceType meshReferenceType = MeshReferenceType.GetFromPreferences, bool forceInitialization = false, bool affectUpdateEveryFrameField = true)
        {
            base.MDModifier_InitializeBase(meshReferenceType, forceInitialization, affectUpdateEveryFrameField);
            MDModifier_InitializeMeshData(backupMeshData: false);
        }

        /// <summary>
        /// Process the default cut base function (use 'MeshCut_Cut' method for more customized setting)
        /// </summary>
        public override void MDModifier_ProcessModifier()
        {
            MeshCut_Cut();
        }

        #endregion

        private void Start()
        {
            if (!Application.isPlaying) return;

            // Declare & store variables on start

            splane = new Plane();
            mc = new MeshCut_Src(256);

            if (addMeshColliderAfterCut)
                MeshCut_RefreshMeshCollider();

            if (automaticallyCut)
                StartCoroutine(AutoCutProcessor());
        }

        #region Private event subscription

        protected override void OnEnable()
        {
            if (!Application.isPlaying)
                return;
            base.OnEnable();
            Event_GotCut += MDM_MeshCut_Event_GotCut;
        }

        private void MDM_MeshCut_Event_GotCut(List<Transform> chunks)
        {
            MDModifier_InitializeMeshData(false, MbBackupMeshData.MbDataInitialized());
        }

        protected override void OnDisable()
        {
            if (!Application.isPlaying)
                return;
            base.OnDisable();
            Event_GotCut -= MDM_MeshCut_Event_GotCut;
        }

        #endregion

        #region Mesh Cut essentials

        /// <summary>
        /// Automatic cut enumerator with delay feature
        /// </summary>
        private IEnumerator AutoCutProcessor()
        {
            if (!Application.isPlaying)
                yield break;

            yield return new WaitForFixedUpdate();
            while(true)
            {
                if (!enabled) yield break;
                MeshCut_Cut(cutterSource);
                yield return new WaitForSeconds(automaticallyCutDelay);
                if (!automaticallyCut) yield break;
            }
        }

        /// <summary>
        /// Default mesh cutting (the most preferred method) - call this method to process the cut feature with properly assigned cutter source
        /// </summary>
        public void MeshCut_Cut()
        {
            MeshCut_Cut(cutterSource);
        }

        /// <summary>
        /// Process mesh cut with required cutter input
        /// </summary>
        /// <param name="cutterInput">Required MeshCut_Cutter component</param>
        public void MeshCut_Cut(MDM_MeshCut_Cutter cutterInput)
        {
            if(cutterInput == null)
            {
                MD_Debug.Debug(this, "Cutter Input is null. The cut couldn't be proceeded!", MD_Debug.DebugType.Error);
                return;
            }

            List<(MeshFilter f, MeshCollider c)> availableMeshes = new List<(MeshFilter f, MeshCollider c)>();

            // Getting all the created chunks
            for (int i = 0; i < createdChunks.Count; i++)
            {
                if (createdChunks[i].f == null)
                {
                    createdChunks.RemoveAt(i);
                    continue;
                }
                // Checking if the cutter intersects the chunks
                if (cutterInput.HitObject(createdChunks[i].f.gameObject))
                    availableMeshes.Add(createdChunks[i]);
            }

            // Checking if the cutter intersects this object
            if (cutterInput.HitObject(transform.gameObject))
                availableMeshes.Add((MbMeshFilter, MyMeshCollider));

            MeshCut_Cut(cutterInput.transform.position, cutterInput.transform.right, availableMeshes);
        }

        /// <summary>
        /// Process mesh cut with required point-location & normal vector. Related just for this mesh and its chunks
        /// </summary>
        /// <param name="position">Point of the cut (world space)</param>
        /// <param name="normal">Normal vector of the cut</param>
        public void MeshCut_Cut(Vector3 position, Vector3 normal)
        {
            List<(MeshFilter f, MeshCollider c)> availableMeshes = new List<(MeshFilter f, MeshCollider c)>();

            // Getting all the created chunks
            for (int i = 0; i < createdChunks.Count; i++)
            {
                if (createdChunks[i].f == null)
                {
                    createdChunks.RemoveAt(i);
                    continue;
                }
                availableMeshes.Add(createdChunks[i]);
            }

            availableMeshes.Add((MbMeshFilter, MyMeshCollider));
            MeshCut_Cut(position, normal, availableMeshes);
        }

        /// <summary>
        /// Process mesh cut with required point-location, normal vector & available meshes to process. Available meshes can be any meshes that might be related to this cutting object
        /// </summary>
        /// <param name="position">Point of the cut (world space)</param>
        /// <param name="normal">Normal vector of the cut</param>
        /// <param name="availableMeshes">Available meshes to process (any meshes that might be related to this cutting object)</param>
        public void MeshCut_Cut(Vector3 position, Vector3 normal, List<(MeshFilter f, MeshCollider c)> availableMeshes)
        {
            // Return if there's no cut
            if (availableMeshes == null || availableMeshes.Count == 0) return;

            // Declare upper & lower meshes (cut has 2 sides)
            List<Transform> upper = new List<Transform>();
            List<Transform> lower = new List<Transform>();

            // List for all available meshes
            for (int i = 0; i < availableMeshes.Count; ++i)
            {
                var am = availableMeshes[i];
                splane.SetNormalAndPosition(
                    ((Vector3)(am.f.transform.localToWorldMatrix.transpose * normal)).normalized,
                    am.f.transform.InverseTransformPoint(position));

                GameObject obj_A = am.f.gameObject;
                Mesh mesh = obj_A.GetComponent<MeshFilter>().mesh;

                if (!mc.MeshCut_Cut(mesh, ref splane))
                {
                    if (splane.GetDistanceToPoint(mc.GetFirstVertex()) >= 0)
                        upper.Add(obj_A.transform);
                    else
                        lower.Add(obj_A.transform);

                    continue;
                }

                // Dividing the upper and lower mesh (positive/negative meshes [from the original code])
                bool posBigger = mc.UpperMeshTemp.surfaceArea > mc.LowerMeshTemp.surfaceArea;
                if (posBigger)
                {
                    meshA = mc.UpperMeshTemp;
                    meshB = mc.LowerMeshTemp;
                }
                else
                {
                    meshA = mc.LowerMeshTemp;
                    meshB = mc.UpperMeshTemp;
                }

                // Create a copy of the cut mesh

                GameObject obj_B = Instantiate(obj_A);
                obj_B.name = obj_A.name;
                MeshFilter f = obj_B.GetComponent<MeshFilter>();
                MeshCollider c = obj_B.GetComponent<MeshCollider>();
                if (addMeshColliderAfterCut && !c)
                    c = obj_B.AddComponent<MeshCollider>();

                createdChunks.Add((f,c));

                MDM_MeshCut mdmmc = obj_B.GetComponent<MDM_MeshCut>();
                if (mdmmc) Destroy(mdmmc);

                obj_B.transform.SetPositionAndRotation(obj_A.transform.position, obj_A.transform.rotation);
                Mesh newObjMesh = f.mesh;

                mc.ReplaceMesh(mesh, meshA, MbRecalculateNormals, MbRecalculateBounds);
                mc.ReplaceMesh(newObjMesh, meshB, MbRecalculateNormals, MbRecalculateBounds);

                var lA = (posBigger ? upper : lower);
                var lB = (posBigger ? lower : upper);
                Vector3 dA = (posBigger ? normal : -normal);
                Vector3 dB = (posBigger ? -normal : normal);

                // Add separation or physics

                if (addSeparationOffset)
                {
                    obj_A.transform.position += dA * separationOffset;
                    obj_B.transform.position += dB * separationOffset;
                }

                if (addRigidbodyAfterCut)
                {
                    Rigidbody r = obj_A.GetComponent<Rigidbody>();
                    if (!r) r = obj_A.AddComponent<Rigidbody>();
                    r.mass = defaultMass;
                    r.AddForce(dA * separationForce, ForceMode.Impulse);

                    r = obj_B.GetComponent<Rigidbody>();
                    if (!r) r = obj_B.AddComponent<Rigidbody>();
                    r.mass = defaultMass;
                    r.AddForce(dB * separationForce, ForceMode.Impulse);
                }

                lA.Add(obj_A.transform);
                lB.Add(obj_B.transform);
            }

            // Refresh mesh collider if possible

            if (addMeshColliderAfterCut)
                MeshCut_RefreshMeshCollider();

            // Process additional event if possible
            affectedChunks.Clear();
            affectedChunks.AddRange(upper);
            affectedChunks.AddRange(lower);
            Event_GotCut?.Invoke(affectedChunks);
            UEvent_OnCut?.Invoke();
        }

        /// <summary>
        /// Refresh mesh collider to all created chunks (if possible)
        /// </summary>
        public void MeshCut_RefreshMeshCollider()
        {
            if(!MbMeshFilter)
            {
                MD_Debug.Debug(this, "Mesh Filter is missing", MD_Debug.DebugType.Error);
                return;
            }

            if(!MyMeshCollider)
            {
                MyMeshCollider = GetComponent<MeshCollider>();
                if (!MyMeshCollider)
                    MyMeshCollider = gameObject.AddComponent<MeshCollider>();
                MyMeshCollider.convex = true;
            }

            // Sometimes the errors might appear due to very thin mesh
            MyMeshCollider.sharedMesh = MbMeshFilter.mesh;

            if (createdChunks.Count == 0) return;
            foreach((MeshFilter f, MeshCollider c) fc in createdChunks)
            {
                if (!fc.c) continue;
                fc.c.sharedMesh = fc.f.mesh;
            }
        }

        #endregion

        /// <summary>
        /// Source of Mesh Cut solution
        /// </summary>
        private sealed class MeshCut_Src
        {
            /* LICENSE
            ----------------------------------------------------------------------------------------
            This piece of code was used and modified from the solution on github by hugoscurti.
            All the original credits go to him.
            The code optimization, integration and modification by Matej Vanco (18.01.2022 - dd/mm/yyyy)

            Original license: https://github.com/hugoscurti/mesh-cutter/blob/master/LICENSE
            Github: https://github.com/hugoscurti/mesh-cutter

            MIT License

            Copyright (c) 2019 Hugo Scurti

            Permission is hereby granted, free of charge, to any person obtaining a copy
            of this software and associated documentation files (the "Software"), to deal
            in the Software without restriction, including without limitation the rights
            to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
            copies of the Software, and to permit persons to whom the Software is
            furnished to do so, subject to the following conditions:

            The above copyright notice and this permission notice shall be included in all
            copies or substantial portions of the Software.

            THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
            IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
            FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
            AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
            LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
            OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
            SOFTWARE.
            ----------------------------------------------------------------------------------------
            */

            /// <summary>
            /// Temporary mesh structure for Mesh-Cut solution
            /// </summary>
            internal struct MeshCut_TempMesh
            {
                public List<Vector3> vertices;
                public List<Vector3> normals;
                public List<Vector2> uvs;
                public List<int> triangles;

                private readonly Dictionary<int, int> vMapping;

                internal float surfaceArea;

                public MeshCut_TempMesh(int vertexCapacity)
                {
                    vertices = new List<Vector3>(vertexCapacity);
                    normals = new List<Vector3>(vertexCapacity);
                    uvs = new List<Vector2>(vertexCapacity);
                    triangles = new List<int>(vertexCapacity * 3);

                    vMapping = new Dictionary<int, int>(vertexCapacity);

                    surfaceArea = 0;
                }

                public void Clear()
                {
                    vertices.Clear();
                    normals.Clear();
                    uvs.Clear();
                    triangles.Clear();

                    vMapping.Clear();

                    surfaceArea = 0;
                }

                /// <summary>
                /// Add point and normal to arrays if not already present
                /// </summary>
                private void AddPoint(Vector3 point, Vector3 normal, Vector2 uv)
                {
                    triangles.Add(vertices.Count);
                    vertices.Add(point);
                    normals.Add(normal);
                    uvs.Add(uv);
                }

                /// <summary>
                /// Add triangles from the original mesh. Therefore, no new vertices to add 
                /// and no normals to compute
                /// </summary>
                public void AddOgTriangle(int[] indices)
                {
                    for (int i = 0; i < 3; ++i)
                        triangles.Add(vMapping[indices[i]]);

                    //Compute triangle area
                    surfaceArea += GetTriangleArea(triangles.Count - 3);
                }

                public void AddSlicedTriangle(int i1, Vector3 v2, Vector2 uv2, int i3)
                {
                    int v1 = vMapping[i1],
                        v3 = vMapping[i3];
                    Vector3 normal = Vector3.Cross(v2 - vertices[v1], vertices[v3] - v2).normalized;

                    triangles.Add(v1);
                    AddPoint(v2, normal, uv2);
                    triangles.Add(vMapping[i3]);

                    //Compute triangle area
                    surfaceArea += GetTriangleArea(triangles.Count - 3);
                }

                public void AddSlicedTriangle(int i1, Vector3 v2, Vector3 v3, Vector2 uv2, Vector2 uv3)
                {
                    // Compute face normal?
                    int v1 = vMapping[i1];
                    Vector3 normal = Vector3.Cross(v2 - vertices[v1], v3 - v2).normalized;

                    triangles.Add(v1);
                    AddPoint(v2, normal, uv2);
                    AddPoint(v3, normal, uv3);

                    //Compute triangle area
                    surfaceArea += GetTriangleArea(triangles.Count - 3);
                }

                /// <summary>
                /// Add a completely new triangle to the mesh
                /// </summary>
                public void AddTriangle(Vector3[] points)
                {
                    // Compute normal
                    Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[1]).normalized;

                    for (int i = 0; i < 3; ++i)
                    {
                        // TODO: Compute uv values for the new triangle?
                        AddPoint(points[i], normal, Vector2.zero);
                    }

                    //Compute triangle area
                    surfaceArea += GetTriangleArea(triangles.Count - 3);
                }

                public void ContainsKeys(List<int> triangles, int startIdx, bool[] isTrue)
                {
                    for (int i = 0; i < 3; ++i)
                        isTrue[i] = vMapping.ContainsKey(triangles[startIdx + i]);
                }

                /// <summary>
                /// Add a vertex from the original mesh 
                /// while storing its old index in the dictionary of index mappings
                /// </summary>
                public void AddVertex(List<Vector3> ogVertices, List<Vector3> ogNormals, List<Vector2> ogUvs, int index)
                {
                    vMapping[index] = vertices.Count;
                    vertices.Add(ogVertices[index]);
                    normals.Add(ogNormals[index]);
                    uvs.Add(ogUvs[index]);
                }


                private float GetTriangleArea(int i)
                {
                    var va = vertices[triangles[i + 2]] - vertices[triangles[i]];
                    var vb = vertices[triangles[i + 1]] - vertices[triangles[i]];
                    float a = va.magnitude;
                    float b = vb.magnitude;
                    float gamma = Mathf.Deg2Rad * Vector3.Angle(vb, va);

                    return a * b * Mathf.Sin(gamma) / 2;
                }
            }

            internal MeshCut_TempMesh UpperMeshTemp { get; private set; }
            internal MeshCut_TempMesh LowerMeshTemp { get; private set; }

            private readonly List<Vector3> addedPairs;

            private readonly List<Vector3> ogVertices;
            private readonly List<int> ogTriangles;
            private readonly List<Vector3> ogNormals;
            private readonly List<Vector2> ogUvs;

            private readonly Vector3[] intersectPair;
            private readonly Vector3[] tempTriangle;

            private readonly MeshCut_Intersections intersect;

            public MeshCut_Src(int initialArraySize)
            {
                UpperMeshTemp = new MeshCut_TempMesh(initialArraySize);
                LowerMeshTemp = new MeshCut_TempMesh(initialArraySize);

                addedPairs = new List<Vector3>(initialArraySize);
                ogVertices = new List<Vector3>(initialArraySize);
                ogNormals = new List<Vector3>(initialArraySize);
                ogUvs = new List<Vector2>(initialArraySize);
                ogTriangles = new List<int>(initialArraySize * 3);

                intersectPair = new Vector3[2];
                tempTriangle = new Vector3[3];

                intersect = new MeshCut_Intersections();
            }

            /// <summary>
            /// Slice a mesh by the slice plane.
            /// We assume the plane is already in the mesh's local coordinate frame
            /// Returns posMesh and negMesh, which are the resuling meshes on both sides of the plane 
            /// (posMesh on the same side as the plane's normal, negMesh on the opposite side)
            /// </summary>
            public bool MeshCut_Cut(Mesh mesh, ref Plane slice)
            {
                // Let's always fill the vertices array so that we can access it even if the mesh didn't intersect
                mesh.GetVertices(ogVertices);

                // 1. Verify if the bounds intersect first
                if (!MeshCut_Intersections.BoundPlaneIntersect(mesh, ref slice))
                    return false;

                mesh.GetTriangles(ogTriangles, 0);
                mesh.GetNormals(ogNormals);
                mesh.GetUVs(0, ogUvs);

                UpperMeshTemp.Clear();
                LowerMeshTemp.Clear();
                addedPairs.Clear();

                // 2. Separate old vertices in new meshes
                for (int i = 0; i < ogVertices.Count; ++i)
                {
                    if (slice.GetDistanceToPoint(ogVertices[i]) >= 0)
                        UpperMeshTemp.AddVertex(ogVertices, ogNormals, ogUvs, i);
                    else
                        LowerMeshTemp.AddVertex(ogVertices, ogNormals, ogUvs, i);
                }

                // 3. If one of the mesh has no vertices, then it doesn't intersect
                if (LowerMeshTemp.vertices.Count == 0 || UpperMeshTemp.vertices.Count == 0)
                    return false;

                // 4. Separate triangles and cut those that intersect the plane
                for (int i = 0; i < ogTriangles.Count; i += 3)
                {
                    if (intersect.TrianglePlaneIntersect(ogVertices, ogUvs, ogTriangles, i, ref slice, UpperMeshTemp, LowerMeshTemp, intersectPair))
                        addedPairs.AddRange(intersectPair);
                }

                if (addedPairs.Count > 0)
                {
                    //FillBoundaryGeneral(addedPairs);
                    FillBoundaryFace(addedPairs);
                    return true;
                }
                else
                {
                    throw new UnityException("Error: if added pairs is empty, we should have returned false earlier");
                }
            }

            /// <summary>
            /// Replace specific mesh with the temporary mesh
            /// </summary>
            internal void ReplaceMesh(Mesh mesh, MeshCut_TempMesh tempMesh, bool recalcNormals, bool recalcBounds)
            {
                mesh.Clear();
                mesh.SetVertices(tempMesh.vertices);
                mesh.SetTriangles(tempMesh.triangles, 0);
                mesh.SetNormals(tempMesh.normals);
                mesh.SetUVs(0, tempMesh.uvs);
                if (recalcNormals)
                    mesh.RecalculateNormals();
                if (recalcBounds)
                    mesh.RecalculateBounds();
                mesh.RecalculateTangents();
            }

            internal Vector3 GetFirstVertex()
            {
                if (ogVertices.Count == 0)
                    throw new UnityException(
                        "Error: Either the mesh has no vertices or GetFirstVertex was called before SliceMesh.");
                else
                    return ogVertices[0];
            }

            private void FillBoundaryFace(List<Vector3> added)
            {
                // 1. Reorder added so in order ot their occurence along the perimeter.
                MeshCut_MathUtils.ReorderList(added);

                // 2. Find actual face vertices
                var face = FindRealPolygon(added);

                // 3. Create triangle fans
                int t_fwd = 0,
                    t_bwd = face.Count - 1,
                    t_new = 1;
                bool incr_fwd = true;

                while (t_new != t_fwd && t_new != t_bwd)
                {
                    AddTriangle(face, t_bwd, t_fwd, t_new);

                    if (incr_fwd) t_fwd = t_new;
                    else t_bwd = t_new;

                    incr_fwd = !incr_fwd;
                    t_new = incr_fwd ? t_fwd + 1 : t_bwd - 1;
                }
            }

            private List<Vector3> FindRealPolygon(List<Vector3> pairs)
            {
                List<Vector3> vertices = new List<Vector3>();
                Vector3 edge1, edge2;

                // List should be ordered in the correct way
                for (int i = 0; i < pairs.Count; i += 2)
                {
                    edge1 = (pairs[i + 1] - pairs[i]);
                    if (i == pairs.Count - 2)
                        edge2 = pairs[1] - pairs[0];
                    else
                        edge2 = pairs[i + 3] - pairs[i + 2];

                    // Normalize edges
                    edge1.Normalize();
                    edge2.Normalize();

                    if (Vector3.Angle(edge1, edge2) > 1e-6f)
                        // This is a corner
                        vertices.Add(pairs[i + 1]);
                }

                return vertices;
            }

            private void AddTriangle(List<Vector3> face, int t1, int t2, int t3)
            {
                tempTriangle[0] = face[t1];
                tempTriangle[1] = face[t2];
                tempTriangle[2] = face[t3];
                UpperMeshTemp.AddTriangle(tempTriangle);

                tempTriangle[1] = face[t3];
                tempTriangle[2] = face[t2];
                LowerMeshTemp.AddTriangle(tempTriangle);
            }


            /// <summary>
            /// Intersection object class for Mesh-Cut solution
            /// </summary>
            internal class MeshCut_Intersections
            {
                /// <summary>
                /// Based on https://gdbooks.gitbooks.io/3dcollisions/content/Chapter2/static_aabb_plane.html
                /// </summary>
                internal static bool BoundPlaneIntersect(Mesh mesh, ref Plane plane)
                {
                    // Compute projection interval radius
                    float r = mesh.bounds.extents.x * Mathf.Abs(plane.normal.x) +
                        mesh.bounds.extents.y * Mathf.Abs(plane.normal.y) +
                        mesh.bounds.extents.z * Mathf.Abs(plane.normal.z);

                    // Compute distance of box center from plane
                    float s = Vector3.Dot(plane.normal, mesh.bounds.center) - (-plane.distance);

                    // Intersection occurs when distance s falls within [-r,+r] interval
                    return Mathf.Abs(s) <= r;
                }

                // Initialize fixed arrays so that we don't initialize them every time we call TrianglePlaneIntersect
                private readonly Vector3[] v;
                private readonly Vector2[] u;
                private readonly int[] t;
                private readonly bool[] positive;

                // Used in intersect method
                private Ray edgeRay;

                public MeshCut_Intersections()
                {
                    v = new Vector3[3];
                    u = new Vector2[3];
                    t = new int[3];
                    positive = new bool[3];
                }

                public (Vector3, Vector2) Intersect(Plane plane, Vector3 first, Vector3 second, Vector2 uv1, Vector2 uv2)
                {
                    edgeRay.origin = first;
                    edgeRay.direction = (second - first).normalized;
                    float maxDist = Vector3.Distance(first, second);

                    if (!plane.Raycast(edgeRay, out float dist))
                        return (Vector3.zero, Vector2.zero);
                    else if (dist > maxDist)
                        return (Vector3.zero, Vector2.zero);

                    (Vector3, Vector2) returnVal = (edgeRay.GetPoint(dist), Vector2.zero);

                    var relativeDist = dist / maxDist;
                    // Compute new uv by doing Linear interpolation between uv1 and uv2
                    returnVal.Item2.x = Mathf.Lerp(uv1.x, uv2.x, relativeDist);
                    returnVal.Item2.y = Mathf.Lerp(uv1.y, uv2.y, relativeDist);
                    return returnVal;
                }

                public bool TrianglePlaneIntersect(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, int startIdx, ref Plane plane, MeshCut_TempMesh posMesh, MeshCut_TempMesh negMesh, Vector3[] intersectVectors)
                {
                    int i;

                    // Store triangle, vertex and uv from indices
                    for (i = 0; i < 3; ++i)
                    {
                        t[i] = triangles[startIdx + i];
                        v[i] = vertices[t[i]];
                        u[i] = uvs[t[i]];
                    }

                    // Store wether the vertex is on positive mesh
                    posMesh.ContainsKeys(triangles, startIdx, positive);

                    // If they're all on the same side, don't do intersection
                    if (positive[0] == positive[1] && positive[1] == positive[2])
                    {
                        // All points are on the same side. No intersection
                        // Add them to either positive or negative mesh
                        (positive[0] ? posMesh : negMesh).AddOgTriangle(t);
                        return false;
                    }

                    // Find lonely point
                    int lonelyPoint;
                    if (positive[0] != positive[1])
                        lonelyPoint = positive[0] != positive[2] ? 0 : 1;
                    else
                        lonelyPoint = 2;

                    // Set previous point in relation to front face order
                    int prevPoint = lonelyPoint - 1;
                    if (prevPoint == -1) prevPoint = 2;
                    // Set next point in relation to front face order
                    int nextPoint = lonelyPoint + 1;
                    if (nextPoint == 3) nextPoint = 0;

                    // Get the 2 intersection points
                    ValueTuple<Vector3, Vector2> newPointPrev = Intersect(plane, v[lonelyPoint], v[prevPoint], u[lonelyPoint], u[prevPoint]);
                    ValueTuple<Vector3, Vector2> newPointNext = Intersect(plane, v[lonelyPoint], v[nextPoint], u[lonelyPoint], u[nextPoint]);

                    //Set the new triangles and store them in respective tempmeshes
                    (positive[lonelyPoint] ? posMesh : negMesh).AddSlicedTriangle(t[lonelyPoint], newPointNext.Item1, newPointPrev.Item1, newPointNext.Item2, newPointPrev.Item2);

                    (positive[prevPoint] ? posMesh : negMesh).AddSlicedTriangle(t[prevPoint], newPointPrev.Item1, newPointPrev.Item2, t[nextPoint]);

                    (positive[prevPoint] ? posMesh : negMesh).AddSlicedTriangle(t[nextPoint], newPointPrev.Item1, newPointNext.Item1, newPointPrev.Item2, newPointNext.Item2);

                    // We return the edge that will be in the correct orientation for the positive side mesh
                    if (positive[lonelyPoint])
                    {
                        intersectVectors[0] = newPointPrev.Item1;
                        intersectVectors[1] = newPointNext.Item1;
                    }
                    else
                    {
                        intersectVectors[0] = newPointNext.Item1;
                        intersectVectors[1] = newPointPrev.Item1;
                    }
                    return true;
                }
            }

            /// <summary>
            /// Math utilities created for Mesh-Cut solution
            /// </summary>
            private class MeshCut_MathUtils
            {
                /// <summary>
                /// Find center of polygon by averaging vertices
                /// </summary>
                public static Vector3 FindCenter(List<Vector3> pairs)
                {
                    Vector3 center = Vector3.zero;
                    int count = 0;

                    for (int i = 0; i < pairs.Count; i += 2)
                    {
                        center += pairs[i];
                        count++;
                    }

                    return center / count;
                }

                /// <summary>
                /// Reorder a list of pairs of vectors (one dimension list where i and i + 1 defines a line segment)
                /// So that it forms a closed polygon 
                /// </summary>
                public static void ReorderList(List<Vector3> pairs)
                {
                    int nbFaces = 0;
                    int faceStart = 0;
                    int i = 0;

                    while (i < pairs.Count)
                    {
                        //Find next adjacent edge
                        for (int j = i + 2; j < pairs.Count; j += 2)
                        {
                            if (pairs[j] == pairs[i + 1])
                            {
                                //Put j at i+2
                                SwitchPairs(pairs, i + 2, j);
                                break;
                            }
                        }

                        //Triangle can't have more than 3 points
                        if (i + 3 >= pairs.Count) break;
                        //Create face
                        else if (pairs[i + 3] == pairs[faceStart])
                        {
                            nbFaces++;
                            i += 4;
                            faceStart = i;
                        }
                        else i += 2;
                    }
                }

                /// <summary>
                /// Switch pairs order of vectors on pos1 and pos2
                /// </summary>
                private static void SwitchPairs(List<Vector3> pairs, int pos1, int pos2)
                {
                    if (pos1 == pos2) return;

                    Vector3 temp1 = pairs[pos1];
                    Vector3 temp2 = pairs[pos1 + 1];
                    pairs[pos1] = pairs[pos2];
                    pairs[pos1 + 1] = pairs[pos2 + 1];
                    pairs[pos2] = temp1;
                    pairs[pos2 + 1] = temp2;
                }
            }
        }
    }
}

#if UNITY_EDITOR
namespace MD_Package_Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MDM_MeshCut))]
    public sealed class MDM_MeshCutEditor : MD_ModifierBase_Editor
    {
        private MDM_MeshCut md;

        public override void OnEnable()
        {
            mMeshBase = (MD_MeshBase)target;
            mModifierBase = (MD_ModifierBase)target;
            md = (MDM_MeshCut)target;
            showUpdateEveryFrame = false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MDE_l("Mesh Cut Modifier", true);
            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("cutterSource", "Cutter Source","Main cutter source, click the button below if there's none");
            if(md.cutterSource == null)
            {
                if(MDE_b("Create a Cutter Source"))
                {
                    GameObject gm = new GameObject("CutterSource_" + md.name);
                    gm.transform.localScale = new Vector3(0.1f, 1, 1);
                    gm.transform.position = Vector3.zero;
                    md.cutterSource = gm.AddComponent<MDM_MeshCut_Cutter>();
                    EditorUtility.SetDirty(md);
                }
            }    
            MDE_ve();

            MDE_v();
            MDE_v();
            MDE_DrawProperty("addRigidbodyAfterCut", "Add Rigidbody", "If the cut is proceeded, add rigidbody to both cut meshes");
            if (md.addRigidbodyAfterCut)
            {
                MDE_plus();
                MDE_DrawProperty("defaultMass", "Default Mass", "Default mass for the created rigidbodies");
                MDE_DrawProperty("separationForce", "Separation Force", "Physical impulse force");
                MDE_minus();
            }
            MDE_ve();

            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("addSeparationOffset", "Add Default Separation", "If enabled, the both cut meshes will be separated through transform.position only");
            if (md.addSeparationOffset)
            {
                MDE_plus();
                MDE_DrawProperty("separationOffset", "Separation Offset");
                MDE_minus();
            }
            MDE_ve();

            MDE_s(5);
            MDE_v();
            MDE_DrawProperty("addMeshColliderAfterCut", "Add Mesh Collider", "If the cut is proceeded, add convex mesh collider to both cut meshes");
            MDE_ve();

            MDE_ve();

            MDE_v();
            MDE_DrawProperty("automaticallyCut", "Cut Automatically", "If enabled, the script will automatically cut possible meshes (this takes more performance)");
            if (md.automaticallyCut)
            {
                MDE_plus();
                MDE_DrawProperty("automaticallyCutDelay", "Cut delay (every N second)", "Delay for checking the available meshes to cut");
                MDE_minus();
            }
            MDE_ve();
            MDE_DrawProperty("UEvent_OnCut", "Event On Cut");
            MDE_AddMeshCollider(md.gameObject);
            MDE_BackToMeshEditor(md);
        }
    }
}
#endif