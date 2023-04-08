using UnityEngine;

using MD_Package.Geometry;

/// <summary>
/// Sample geometry handler through the code
/// </summary>
public class MD_Examples_GeometryManager : MonoBehaviour
{
    private MD_GeometryBase currentGeometry;

    private void Start()
    {
        // Create default starting geometry
        CreateSpecificGeometry(0);
    }

    /// <summary>
    /// Create specific geometry type - this is called from the UI button (See scene)
    /// </summary>
    public void CreateSpecificGeometry(int indx)
    {
        switch(indx)
        {
            case 0: CreateNewGeo<MDG_ProceduralPlane>(); break;
            case 1: CreateNewGeo<MDG_Cube>(); break;
            case 2: CreateNewGeo<MDG_Cone>(); break;
            case 3: CreateNewGeo<MDG_Torus>(); break;
            case 4: CreateNewGeo<MDG_Sphere>(); break;
            case 5: CreateNewGeo<MDG_Tube>(); break;
            case 6: CreateNewGeo<MDG_Triangle>(); break;
        }
    }

    /// <summary>
    /// Creating a generic geometry to avoid duplicates
    /// </summary>
    private async void CreateNewGeo<T>() where T : MD_GeometryBase
    {
        // First we need to destroy the current geometry to avoid errors and complications
        if (currentGeometry)
            Destroy(currentGeometry);
        // Wait a few miliseconds until destroy is completed properly... This can be done through the coroutines as well
        await System.Threading.Tasks.Task.Delay(10);
        // Create new geometry...
        currentGeometry = MD_GeometryBase.CreateGeometry<T>(gameObject);
    }

    /// <summary>
    /// Change center value on the geometry - this is changed via UI Toggle element (See scene)
    /// </summary>
    public void ChangeCenterValue(bool isCentered)
    {
        currentGeometry.geometryCenterMesh = isCentered;
        currentGeometry.MDMeshBase_ProcessCompleteMeshUpdate();
    }

    /// <summary>
    /// Change geometry vertex count - this is change via UI Slider element (See scene)
    /// </summary>
    public void ChangeResolution(float res)
    {
        currentGeometry.geometryResolution = (int)res;
        currentGeometry.MDGeometryBase_SyncUnsharedValues();
        currentGeometry.MDMeshBase_ProcessCompleteMeshUpdate();
    }
}
