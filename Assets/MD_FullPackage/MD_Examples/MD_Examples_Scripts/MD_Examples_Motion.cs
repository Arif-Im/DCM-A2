using UnityEngine;

public class MD_Examples_Motion : MonoBehaviour
{
    public float rotationSpeed = 12.0f;

    private void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
    }
}
