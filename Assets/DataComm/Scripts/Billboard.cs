using UnityEngine;


public class Billboard : MonoBehaviour
{
    private void LateUpdate()
    {
        transform.forward = new Vector3(Camera.main.transform.forward.x, transform.forward.y,
            Camera.main.transform.forward.z);
    }
}