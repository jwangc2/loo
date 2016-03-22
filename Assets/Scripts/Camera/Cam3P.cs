using UnityEngine;
using System.Collections;

public class Cam3P : MonoBehaviour {

	public Transform target;
	public float dist = 5f;
	public float height = 3f;

	//public CharMovement pcc;

	// Use this for initialization
	void Start () {
		Follow ();
	}
	
	// Update is called once per frame
	void Update () {
		Follow ();
	}

	void Follow() {
		if (target)
		{
			Vector3 fwd = target.forward;
			Vector3 up = target.up;
			/*if (pcc)
			{
				Vector3 vel = pcc.GetVelocity();
				if (Mathf.Abs(vel.x) > 0.1f || Mathf.Abs(vel.z) > 0.1f)
				{
					fwd = vel;
					fwd.y = 0;
					fwd = fwd.normalized;
					up = Vector3.up;
				}
			}*/

			Vector3 tpos = target.position - (fwd * dist) + (up * height);
			this.transform.position = Vector3.Lerp(this.transform.position, tpos, 7f * Time.deltaTime);
			this.transform.LookAt(target.position);
		}
	}
}