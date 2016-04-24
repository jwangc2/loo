using UnityEngine;
using System.Collections;

public class MouseCamClip : MouseCam {

    public LayerMask clipMask = -1;
    public float minClipDistance = 1.5f;
    public bool useDebug = true;

	// Use this for initialization
    protected override void Start()
    {
        base.Start();
	}
	
	// Update is called once per frame
	protected override void LateUpdate()
    {
        base.LateUpdate();

        // Check for clipping
        if (target)
        {
            RaycastHit hit;
            Vector3 dir = (target.position - transform.position).normalized;
            if (Physics.Linecast(transform.position, target.position, out hit, clipMask.value))
            {
                float dist = (target.position - hit.point).magnitude;
                if (useDebug) 
                {
                    Debug.Log ("MouseCamClip::LateUpdate");
                    Debug.Log ("Hit: " + hit.collider.name + " D: " + dist);
                }
                transform.position = hit.point;
                if (dist <= minClipDistance) {
                    if (useDebug)
                        Debug.Log ("Too close!");
                    transform.position = target.position + (hit.point - target.position).normalized * minClipDistance;
                }
            }
        }
	}

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 dir = (target.position - transform.position).normalized;
        Gizmos.DrawLine (transform.position, target.position);
    }
}
