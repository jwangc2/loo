using UnityEngine;
using System.Collections;

public class MouseCam : MonoBehaviour {

    public Transform target;
    public float distance = 10f;
    public float xSpeed = 250f;
    public float ySpeed = 120f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    float x = 0f;
    float y = 0f;

	// Use this for initialization
	void Start () {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Make the rigid body not change rotation
        Rigidbody rbody = GetComponent<Rigidbody>();
        if (rbody)
            rbody.freezeRotation = true;
	}
	
	// LateUpdate is called once per frame
	void LateUpdate () {
        if (target)
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            y = ClampAngle (y, yMinLimit, yMaxLimit);

            Quaternion newRotation = Quaternion.Euler(y, x, 0);
            Vector3 newPosition = newRotation * new Vector3(0f, 0f, -distance) + target.position;

            transform.rotation = newRotation;
            transform.position = newPosition;
        }
	}

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
