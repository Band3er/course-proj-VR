using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class BowDrawController : MonoBehaviour
{
	[Header("References")]
	public Transform stringGrabPoint;
	public Transform bowHoldPoint;
	public Transform arrowSpawnPoint;
	public GameObject arrowPrefab;

	[Header("Draw Settings")]
	public float maxDrawDistance = 0.5f;
	public float maxLaunchForce = 30f;

	[Header("Interaction")]
	public HandGrabInteractable stringInteractable; // ← schimbat din HandGrabInteractable

	[Header("Experiment (optional)")]
	public ArcheryEventBridge eventBridge;

	private bool isDrawing = false;
	private bool isStringGrabbed = false;
	private float currentDrawAmount = 0f;
	private GameObject currentArrow;
	private ArrowController currentArrowController;
	private Rigidbody arrowRb;

	void OnEnable()
	{
		if (stringInteractable == null)
		{
			Debug.LogError("[DEBUG] stringInteractable is NULL!");
			return;
		}
		Debug.Log("[DEBUG] Subscribed to string events OK");
		stringInteractable.WhenSelectingInteractorAdded.Action += OnStringGrabbed;
		stringInteractable.WhenSelectingInteractorRemoved.Action += OnStringReleased;
	}

	void OnDisable()
	{
		if (stringInteractable == null) return;
		stringInteractable.WhenSelectingInteractorAdded.Action -= OnStringGrabbed;
		stringInteractable.WhenSelectingInteractorRemoved.Action -= OnStringReleased;
	}

	private void OnStringGrabbed(HandGrabInteractor interactor) // Modificat tipul parametrului
	{
		Debug.Log("[DEBUG] String GRABBED!");
		isStringGrabbed = true;
		eventBridge?.OnShotStarted();
	}

	private void OnStringReleased(HandGrabInteractor interactor) // Modificat tipul parametrului
	{
		isStringGrabbed = false;
	}

	void Update()
	{
		if (isStringGrabbed)
		{
			if (!isDrawing) StartDraw();
			UpdateDraw();
		}
		else if (isDrawing)
		{
			ReleaseArrow();
		}
	}

	void StartDraw()
	{
		isDrawing = true;
		currentArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position,
								   arrowSpawnPoint.rotation);
		arrowRb = currentArrow.GetComponent<Rigidbody>();
		currentArrowController = currentArrow.GetComponent<ArrowController>();

		if (currentArrowController != null)
			currentArrowController.SetNocked(arrowSpawnPoint);
		else
			arrowRb.isKinematic = true;
	}

	void UpdateDraw()
	{
		float drawDistance = Vector3.Distance(
			stringGrabPoint.position,
			bowHoldPoint.position
		);
		currentDrawAmount = Mathf.Clamp01(drawDistance / maxDrawDistance);
	}

	void ReleaseArrow()
	{
		isDrawing = false;
		if (currentArrow == null) return;

		eventBridge?.OnArrowReleased();

		float force = currentDrawAmount * maxLaunchForce;

		if (currentArrowController != null)
			currentArrowController.Launch(arrowSpawnPoint.forward, force);
		else
		{
			arrowRb.isKinematic = false;
			arrowRb.useGravity = true;
			arrowRb.AddForce(arrowSpawnPoint.forward * force, ForceMode.Impulse);
		}

		currentArrow = null;
		currentArrowController = null;
		currentDrawAmount = 0f;
	}
}