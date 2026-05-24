using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class BowDrawController : MonoBehaviour
{
	[Header("References")]
	public Transform stringGrabPoint;
	public Transform stringRestPoint;
	public Transform bowHoldPoint;
	public Transform arrowSpawnPoint;
	public GameObject arrowPrefab;

	[Header("Draw Settings")]
	public float maxDrawDistance = 0.25f;   // reduce this for shorter draw
	public float maxLaunchForce = 40f;

	public enum DrawAxis { NegZ, PosZ, NegX, PosX, NegY, PosY }
	public DrawAxis drawAxis = DrawAxis.NegZ;
	[Header("Draw Axis (change if Draw=0)")]

	[Header("Interaction")]
	public HandGrabInteractable stringInteractable;

	[Header("Experiment (optional)")]
	public ArcheryEventBridge eventBridge;

	// ── State ──────────────────────────────────────────────────────────────
	private bool isDrawing = false;
	private bool isStringGrabbed = false;
	private float currentDrawAmount = 0f;
	private float smoothDraw = 0f;

	// desired draw distance this frame — set in Update, applied in LateUpdate
	private float _targetDrawDistance = 0f;

	private HandGrabInteractor activeInteractor;
	private Rigidbody stringRb;

	private GameObject currentArrow;
	private ArrowController currentArrowController;
	private Rigidbody arrowRb;

	// ── Lifecycle ──────────────────────────────────────────────────────────
	void Awake()
	{
		stringRb = stringGrabPoint != null
			? stringGrabPoint.GetComponent<Rigidbody>()
			: null;
	}

	void OnEnable()
	{
		if (stringInteractable == null) { Debug.LogError("[BOW] stringInteractable is NULL!"); return; }
		stringInteractable.WhenSelectingInteractorAdded.Action += OnStringGrabbed;
		stringInteractable.WhenSelectingInteractorRemoved.Action += OnStringReleased;
	}

	void OnDisable()
	{
		if (stringInteractable == null) return;
		stringInteractable.WhenSelectingInteractorAdded.Action -= OnStringGrabbed;
		stringInteractable.WhenSelectingInteractorRemoved.Action -= OnStringReleased;
	}

	// ── Grab callbacks ─────────────────────────────────────────────────────
	private void OnStringGrabbed(HandGrabInteractor interactor)
	{
		if (isStringGrabbed) return;
		Debug.Log("[BOW] String GRABBED by: " + interactor.name);
		isStringGrabbed = true;
		activeInteractor = interactor;

		// Take over the string rigidbody so the SDK can't drag the bow
		if (stringRb != null)
		{
			stringRb.isKinematic = true;
			stringRb.useGravity = false;
		}

		eventBridge?.OnShotStarted();
	}

	private void OnStringReleased(HandGrabInteractor interactor)
	{
		if (activeInteractor != null && interactor != activeInteractor) return;
		Debug.Log("[BOW] String RELEASED.");
		isStringGrabbed = false;
		activeInteractor = null;
	}

	// ── Update: compute draw amount only ──────────────────────────────────
	void Update()
	{
		if (isStringGrabbed)
		{
			if (!isDrawing) StartDraw();
			ComputeDraw();
		}
		else if (isDrawing)
		{
			ReleaseArrow();
		}
	}

	// ── LateUpdate: apply position AFTER the Oculus SDK has run ───────────
	void LateUpdate()
	{
		if (!isDrawing) return;

		// Override string position — runs after SDK, so we always win
		stringGrabPoint.localPosition = new Vector3(
			stringGrabPoint.localPosition.x,
			stringGrabPoint.localPosition.y,
			-_targetDrawDistance
		);

		// Keep the arrow at the string, pointing forward
		if (currentArrow != null)
		{
			currentArrow.transform.position = stringGrabPoint.position;
			currentArrow.transform.rotation = arrowSpawnPoint.rotation;
		}
	}

	// ── Draw phase ─────────────────────────────────────────────────────────
	void StartDraw()
	{
		isDrawing = true;
		smoothDraw = 0f;
		_targetDrawDistance = 0f;

		currentArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, arrowSpawnPoint.rotation);
		if (currentArrow == null) { Debug.LogError("[BOW] Arrow instantiation FAILED."); return; }

		arrowRb = currentArrow.GetComponent<Rigidbody>();
		currentArrowController = currentArrow.GetComponent<ArrowController>();

		if (currentArrowController != null)
			currentArrowController.SetNocked(arrowSpawnPoint);
		else if (arrowRb != null)
			arrowRb.isKinematic = true;
	}

	void ComputeDraw()
	{
		if (activeInteractor == null) return;

		// Încearcă să ia poziția reală a mâinii din Oculus API
		Vector3 handWorldPos;
		Pose rootPose;

		if (activeInteractor.Hand != null &&
			activeInteractor.Hand.GetRootPose(out rootPose))
		{
			handWorldPos = rootPose.position;
		}
		else
		{
			// fallback dacă Hand API nu e disponibil
			handWorldPos = activeInteractor.transform.position;
		}

		Vector3 localHand = bowHoldPoint.InverseTransformPoint(handWorldPos);

		Debug.Log("[BOW] localHand X=" + localHand.x.ToString("F3") +
				  " Y=" + localHand.y.ToString("F3") +
				  " Z=" + localHand.z.ToString("F3"));

		float rawDraw = GetRawDraw(localHand);
		float drawDistance = Mathf.Clamp(rawDraw, 0f, maxDrawDistance);

		smoothDraw = Mathf.Lerp(smoothDraw, drawDistance, Time.deltaTime * 20f);
		currentDrawAmount = smoothDraw / maxDrawDistance;
		_targetDrawDistance = smoothDraw;

		Debug.Log("[BOW] Draw=" + drawDistance.ToString("F3") +
				  " Amount=" + currentDrawAmount.ToString("F3"));
	}

	private float GetRawDraw(Vector3 localHand)
	{
		switch (drawAxis)
		{
			case DrawAxis.NegZ: return -localHand.z;
			case DrawAxis.PosZ: return localHand.z;
			case DrawAxis.NegX: return -localHand.x;
			case DrawAxis.PosX: return localHand.x;
			case DrawAxis.NegY: return -localHand.y;
			case DrawAxis.PosY: return localHand.y;
			default: return -localHand.z;
		}
	}

	// ── Release ────────────────────────────────────────────────────────────
	void ReleaseArrow()
	{
		isDrawing = false;
		_targetDrawDistance = 0f;

		if (currentArrow == null) { ResetString(); return; }

		eventBridge?.OnArrowReleased();

		float force = currentDrawAmount * maxLaunchForce;
		Debug.Log("[BOW] Launching with force = " + force.ToString("F2"));

		if (force < 0.5f)
		{
			Destroy(currentArrow);
		}
		else if (currentArrowController != null)
		{
			currentArrowController.Launch(arrowSpawnPoint.forward, force);
		}
		else if (arrowRb != null)
		{
			arrowRb.isKinematic = false;
			arrowRb.useGravity = true;
			arrowRb.linearVelocity = arrowSpawnPoint.forward * force;
		}

		Debug.DrawRay(arrowSpawnPoint.position, arrowSpawnPoint.forward * 3f, Color.red, 3f);
		ResetString();
	}

	private void ResetString()
	{
		if (stringRestPoint != null)
			stringGrabPoint.localPosition = stringRestPoint.localPosition;
		else
			stringGrabPoint.localPosition = Vector3.zero;

		currentArrow = null;
		currentArrowController = null;
		arrowRb = null;
		currentDrawAmount = 0f;
		smoothDraw = 0f;
		_targetDrawDistance = 0f;
	}
}