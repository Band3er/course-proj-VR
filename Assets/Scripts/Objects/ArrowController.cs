using UnityEngine;

public class ArrowController : MonoBehaviour
{
	private bool isNocked = false;
	private Rigidbody rb;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	// Apelat de BowDrawController la StartDraw
	public void SetNocked(Transform nockPoint)
	{
		isNocked = true;
		rb.isKinematic = true;
		rb.useGravity = false;
		transform.SetParent(nockPoint);
	}

	// Apelat de BowDrawController la Release
	public void Launch(Vector3 direction, float force)
	{
		isNocked = false;
		transform.SetParent(null);
		rb.isKinematic = false;
		rb.useGravity = true;
		rb.AddForce(direction.normalized * force, ForceMode.Impulse);
	}

	void Update()
	{
		if (!isNocked && rb.linearVelocity.sqrMagnitude > 0.01f)
		{
			transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
		}
	}
}