using UnityEngine;

public class ArrowController : MonoBehaviour
{
	private bool isNocked = false;
	private Rigidbody rb;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();

		Debug.Log("[ARROW] Awake");

		if (rb == null)
		{
			Debug.LogError("[ARROW] Rigidbody missing!");
		}
	}

	public void SetNocked(Transform nockPoint)
	{
		isNocked = true;

		rb.isKinematic = true;
		rb.useGravity = false;

		transform.SetParent(nockPoint);

		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
	}

	public void Launch(Vector3 direction, float force)
	{
		Debug.Log("[ARROW] LAUNCH START");

		isNocked = false;

		transform.SetParent(null);

		rb.isKinematic = false;
		rb.useGravity = true;

		rb.linearVelocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;

		Debug.Log("[ARROW] BEFORE FORCE velocity = " + rb.linearVelocity);

		rb.AddForce(direction.normalized * force, ForceMode.VelocityChange);

		Debug.Log("[ARROW] AFTER FORCE velocity = " + rb.linearVelocity);

		Debug.Log("[ARROW] position = " + transform.position);

		Destroy(gameObject, 10f);
	}

	void Update()
	{
		if (!isNocked)
		{
			Debug.Log(
				"[ARROW] Flying | Pos = " + transform.position +
				" | Vel = " + rb.linearVelocity.magnitude
			);

			if (rb.linearVelocity.sqrMagnitude > 0.01f)
			{
				transform.rotation =
					Quaternion.LookRotation(rb.linearVelocity);
			}
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		Debug.Log(
			"[ARROW] COLLIDED WITH -> " +
			collision.gameObject.name
		);
	}
}