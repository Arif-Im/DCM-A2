using UnityEngine;

public class MD_Examples_OrbitalCamera : MonoBehaviour {

    public Transform target;
    [Space]
    public float distance = 5.0f;
    public float xSpeed = 15;
    public float ySpeed = 100;
    [Space]
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
    [Space]
    public float distanceMin = .5f;
    public float distanceMax = 15f;
    [Space]
    public float SmoothSpeed = 0.2f;
    [Space]
    public bool UseInput = true;
    public KeyCode MoveInput = KeyCode.Mouse1;
    [Space]
    public bool UsingRigidbody = false;
    public bool AllowShooting = false;
    [Space]
    public bool SmartZoom = false;
    public float SmartZoomSpeedMultiplier = 0.2f;

    float x = 0.0f;
    float y = 0.0f;

    private void Start()
    {
        if (target == null)
        {
            if (GameObject.Find("Center_Camera"))
                target = GameObject.Find("Center_Camera").transform;
            else
                target = new GameObject("CamCenter").transform;
        }
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    private void LateUpdate()
    {
        if(AllowShooting)
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                GameObject newSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                newSphere.transform.position = transform.position + transform.forward;
                newSphere.transform.localScale /= 6;
                newSphere.AddComponent<Rigidbody>().AddForce(transform.forward * 1000);
                Destroy(newSphere, 5);
            }
        }
        if (UsingRigidbody)
            return;
        if (target)
        {
            if (UseInput && Input.GetKey(MoveInput))
            {
                x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            }
            else if(!UseInput)
            {
                x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            }

            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            if (!SmartZoom)
                distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);
            else
            {
                float dist = Vector3.Distance(transform.position, target.position);
                if (Input.GetAxis("Mouse ScrollWheel") != 0)
                {
                    distance = distance - Input.GetAxis("Mouse ScrollWheel")  * ((dist * dist * dist) * SmartZoomSpeedMultiplier);
                    distance = Mathf.Clamp(distance, 1.1f, 5f);
                }
            }

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            transform.rotation = Quaternion.Lerp(transform.rotation, rotation,SmoothSpeed);
            transform.position = Vector3.Lerp(transform.position, position, SmoothSpeed);
        }
    }

    private void FixedUpdate()
    {
        if (!UsingRigidbody)
            return;

        if (target)
        {
            if (UseInput && Input.GetKey(MoveInput))
            {
                x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            }
            else if (!UseInput)
            {
                x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            }

            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, SmoothSpeed);
            transform.position = Vector3.Lerp(transform.position, position, SmoothSpeed);
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    public void SwitchTarget(Transform t)
    {
        target = t;
    }
}