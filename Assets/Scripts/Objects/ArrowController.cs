using System.Collections;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
	private bool isNocked = false;
	private bool isFlying = false;
	private bool isStuck = false;

	private Rigidbody rb;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		Debug.Log("[DEBUG] ArrowController Awake");
		if (rb == null) Debug.LogError("[DEBUG] Rigidbody missing on Arrow!");
	}

	// ─── Nock ──────────────────────────────────────────────────────────────
	public void SetNocked(Transform nockPoint)
	{
		isNocked = true;
		isFlying = false;
		isStuck = false;

		rb.isKinematic = true;
		rb.useGravity = false;

		transform.SetParent(nockPoint);
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
	}

	// ─── Launch ────────────────────────────────────────────────────────────
	public void Launch(Vector3 direction, float force)
	{
		Debug.Log("[DEBUG] LAUNCH START | dir=" + direction + " | force=" + force);

		isNocked = false;
		isFlying = true;

		transform.SetParent(null);

		StartCoroutine(ApplyLaunchForce(direction, force));

		Destroy(gameObject, 15f);
	}

	private IEnumerator ApplyLaunchForce(Vector3 direction, float force)
	{
		yield return new WaitForFixedUpdate();

		rb.isKinematic = false;
		rb.useGravity = true;

		rb.linearVelocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;

		rb.linearVelocity = direction.normalized * force;

		Debug.Log("[DEBUG] AFTER FORCE velocity = " + rb.linearVelocity);
		Debug.Log("[DEBUG] Arrow position        = " + transform.position);

		IgnorePlayerCollision();
	}

	// ─── Player collision ignore ───────────────────────────────────────────
	private void IgnorePlayerCollision()
	{
		Collider arrowCol = GetComponent<Collider>();
		if (arrowCol == null)
			arrowCol = GetComponentInChildren<Collider>();
		if (arrowCol == null) return;

		// 1. Try by "Player" tag
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

		// 2. Fallback: scan every collider in the "Player" layer
		if (players.Length == 0)
		{
			int playerLayer = LayerMask.NameToLayer("Player");
			if (playerLayer >= 0)
			{
				var allCols = FindObjectsByType<Collider>(FindObjectsSortMode.None);
				var list = new System.Collections.Generic.List<GameObject>();
				foreach (var c in allCols)
					if (c.gameObject.layer == playerLayer)
						list.Add(c.gameObject);
				players = list.ToArray();
			}
		}

		foreach (var player in players)
		{
			foreach (var col in player.GetComponentsInChildren<Collider>(true))
			{
				Physics.IgnoreCollision(arrowCol, col, true);
				Debug.Log("[DEBUG] Ignoring collision with: " + col.gameObject.name);
			}
		}
	}

	// ─── Update ────────────────────────────────────────────────────────────
	void Update()
	{
		if (!isFlying || isStuck) return;

		if (rb.linearVelocity.sqrMagnitude > 0.05f)
		{
			transform.rotation = Quaternion.Slerp(
				transform.rotation,
				Quaternion.LookRotation(rb.linearVelocity),
				Time.deltaTime * 15f
			);
		}
	}

	// ─── Collision ─────────────────────────────────────────────────────────
	void OnCollisionEnter(Collision collision)
	{
		if (isStuck) return;

		Debug.Log("[DEBUG] COLLIDED WITH -> " + collision.gameObject.name);

		isFlying = false;
		isStuck = true;

		rb.isKinematic = true;
		rb.useGravity = false;

		transform.SetParent(collision.transform);

		CancelInvoke();
		Destroy(gameObject, 30f);
	}
}