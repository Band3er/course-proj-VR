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
	public float maxDrawDistance = 0.25f;
	public float maxLaunchForce = 40f;

	public enum DrawAxis { NegZ, PosZ, NegX, PosX, NegY, PosY }
	public DrawAxis drawAxis = DrawAxis.NegZ;
	[Header("Draw Axis (change if Draw=0)")]

	[Header("Interaction")]
	public HandGrabInteractable stringInteractable;       // maini
	public GrabInteractable stringGrabInteractable;       // controllere

	[Header("Experiment (optional)")]
	public ArcheryEventBridge eventBridge;

	// ── State ──────────────────────────────────────────────────────────────
	private bool isDrawing = false;
	private bool isStringGrabbed = false;
	private float currentDrawAmount = 0f;
	private float smoothDraw = 0f;
	private float _targetDrawDistance = 0f;

	// Un singur câmp generic pentru ambele tipuri
	private Transform activeInteractorTransform;
	private HandGrabInteractor activeHandInteractor;   // doar pentru Hand API

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
		if (stringInteractable != null)
		{
			stringInteractable.WhenSelectingInteractorAdded.Action += OnHandGrabbed;
			stringInteractable.WhenSelectingInteractorRemoved.Action += OnHandReleased;
		}

		if (stringGrabInteractable != null)
		{
			stringGrabInteractable.WhenSelectingInteractorAdded.Action += OnControllerGrabbed;
			stringGrabInteractable.WhenSelectingInteractorRemoved.Action += OnControllerReleased;
		}
	}

	void OnDisable()
	{
		if (stringInteractable != null)
		{
			stringInteractable.WhenSelectingInteractorAdded.Action -= OnHandGrabbed;
			stringInteractable.WhenSelectingInteractorRemoved.Action -= OnHandReleased;
		}

		if (stringGrabInteractable != null)
		{
			stringGrabInteractable.WhenSelectingInteractorAdded.Action -= OnControllerGrabbed;
			stringGrabInteractable.WhenSelectingInteractorRemoved.Action -= OnControllerReleased;
		}
	}

	// ── Grab callbacks: Maini ──────────────────────────────────────────────
	private void OnHandGrabbed(HandGrabInteractor interactor)
	{
		if (isStringGrabbed) return;
		isStringGrabbed = true;
		activeHandInteractor = interactor;
		activeInteractorTransform = interactor.transform;

		if (stringRb != null)
		{
			stringRb.isKinematic = true;
			stringRb.useGravity = false;
		}

		eventBridge?.OnShotStarted();
		Debug.Log("[BOW] String GRABBED (Hand): " + interactor.name);
	}

	private void OnHandReleased(HandGrabInteractor interactor)
	{
		if (activeHandInteractor != null && interactor != activeHandInteractor) return;
		isStringGrabbed = false;
		activeHandInteractor = null;
		activeInteractorTransform = null;
		Debug.Log("[BOW] String RELEASED (Hand)");
	}

	// ── Grab callbacks: Controllere ────────────────────────────────────────
	private void OnControllerGrabbed(GrabInteractor interactor)
	{
		if (isStringGrabbed) return;
		isStringGrabbed = true;
		activeHandInteractor = null;
		activeInteractorTransform = interactor.transform;

		if (stringRb != null)
		{
			stringRb.isKinematic = true;
			stringRb.useGravity = false;
		}

		eventBridge?.OnShotStarted();
		Debug.Log("[BOW] String GRABBED (Controller): " + interactor.name);
	}

	private void OnControllerReleased(GrabInteractor interactor)
	{
		if (activeInteractorTransform != interactor.transform) return;
		isStringGrabbed = false;
		activeInteractorTransform = null;
		Debug.Log("[BOW] String RELEASED (Controller)");
	}

	// ── Update ─────────────────────────────────────────────────────────────
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

	// ── LateUpdate ─────────────────────────────────────────────────────────
	void LateUpdate()
	{
		if (!isDrawing) return;

		if (stringRestPoint != null)
		{
			// Muta string-ul in world space, inapoi de la rest position
			stringGrabPoint.position = stringRestPoint.position
				+ (-arrowSpawnPoint.forward) * _targetDrawDistance;
		}

		if (currentArrow != null)
		{
			currentArrow.transform.position = stringGrabPoint.position;
			currentArrow.transform.rotation = arrowSpawnPoint.rotation;
		}
	}

	// ── Draw ───────────────────────────────────────────────────────────────
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
		if (activeInteractorTransform == null) return;

		Vector3 handWorldPos;

		if (activeHandInteractor != null && activeHandInteractor.Hand != null)
		{
			Pose rootPose;
			handWorldPos = activeHandInteractor.Hand.GetRootPose(out rootPose)
				? rootPose.position
				: activeInteractorTransform.position;
		}
		else
		{
			handWorldPos = activeInteractorTransform.position;
		}

		// World space — nu depinde de axele bow-ului
		Vector3 restWorldPos = stringRestPoint != null
			? stringRestPoint.position
			: stringGrabPoint.position;

		// Directia de draw = opus directiei sageti, in world space
		Vector3 drawDirection = -arrowSpawnPoint.forward;

		float rawDraw = Vector3.Dot(handWorldPos - restWorldPos, drawDirection);

		Debug.Log("[BOW] rawDraw=" + rawDraw.ToString("F3") +
				  " drawDir=" + drawDirection +
				  " handPos=" + handWorldPos +
				  " restPos=" + restWorldPos);

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
		Debug.Log("[BOW] Launching force = " + force.ToString("F2"));

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