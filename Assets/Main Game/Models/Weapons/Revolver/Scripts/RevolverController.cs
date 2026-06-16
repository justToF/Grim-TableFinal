using UnityEngine;
using System.Collections;

public class RevolverController : MonoBehaviour {
	public float revRotSpeed;
	public Transform cylTr;

	private bool rotatingRev = false;
	private bool rotatingCyl = false;
	private float endAngle = 60F;

	void Start () {
		if (cylTr == null) {

			cylTr = transform.Find("Body/Cylinder");
			if (cylTr == null) cylTr = transform.Find("Cylinder");
		}
	}

	void Update () {
		if (rotatingRev)
			transform.Rotate (Vector3.up * revRotSpeed * Time.deltaTime);

		if (rotatingCyl) {
			RotateCyl ();
		}
	}

	public void FlipRevState () {
		rotatingRev = !rotatingRev;
	}

	public void RotateCyl () {
		if (endAngle == 360F && cylTr.localRotation.eulerAngles.y < 60F) {
			endAngle = 0F;
		}
		if (cylTr.localRotation.eulerAngles.y < endAngle ) {
			rotatingCyl = true;
			Quaternion target = Quaternion.Euler (0, endAngle, 0); 
			cylTr.localRotation = Quaternion.RotateTowards (cylTr.localRotation, target, Time.deltaTime * 100F);
		} else {
			rotatingCyl = false;
			endAngle += 60F;
		}
	}
}
