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
	public float maxDrawDistance = 0.35f;
	public float maxLaunchForce = 40f;

	[Header("Interaction")]
	public HandGrabInteractable stringInteractable;

	[Header("Experiment (optional)")]
	public ArcheryEventBridge eventBridge;

	private bool isDrawing = false;
	private bool isStringGrabbed = false;
	private float currentDrawAmount = 0f;

	private GameObject currentArrow;
	private ArrowController currentArrowController;
	private Rigidbody arrowRb;

	private HandGrabInteractor activeInteractor;
	public Transform stringRestPoint;

	private float smoothDraw;

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

	private void OnStringGrabbed(HandGrabInteractor interactor)
	{
		if (isStringGrabbed)
		{
			Debug.Log("[DEBUG] Already grabbed -> ignoring");
			return;
		}

		Debug.Log("[DEBUG] String GRABBED!");

		isStringGrabbed = true;
		activeInteractor = interactor;

		eventBridge?.OnShotStarted();
	}

	private void OnStringReleased(HandGrabInteractor interactor)
	{
		Debug.Log("[DEBUG] String RELEASED!");

		isStringGrabbed = false;
		activeInteractor = null;
	}

	void Update()
	{
		if (isStringGrabbed)
		{
			if (!isDrawing)
			{
				Debug.Log("[DEBUG] Starting draw...");
				StartDraw();
			}

			UpdateDraw();
		}
		else if (isDrawing)
		{
			Debug.Log("[DEBUG] Releasing arrow...");
			ReleaseArrow();
		}
	}

	void StartDraw()
	{
		isDrawing = true;

		Debug.Log("[DEBUG] Instantiating arrow");

		currentArrow = Instantiate(
			arrowPrefab,
			arrowSpawnPoint.position,
			arrowSpawnPoint.rotation
		);

		if (currentArrow == null)
		{
			Debug.LogWarning("[DEBUG] Failed to instantiate arrow!");
			return;
		}

		arrowRb = currentArrow.GetComponent<Rigidbody>();
		currentArrowController = currentArrow.GetComponent<ArrowController>();

		if (arrowRb == null)
		{
			Debug.LogError("[DEBUG] Arrow Rigidbody missing!");
		}

		if (currentArrowController != null)
		{
			Debug.Log("[DEBUG] ArrowController found -> SetNocked()");
			currentArrowController.SetNocked(arrowSpawnPoint);
		}
		else
		{
			Debug.LogWarning("[DEBUG] ArrowController missing, fallback mode");

			arrowRb.isKinematic = true;
		}
	}

	void UpdateDraw()
	{
		if (activeInteractor == null)
			return;

		Vector3 handPos =
			activeInteractor.transform.position;

		Vector3 localHandPos =
			bowHoldPoint.InverseTransformPoint(handPos);

		// presupunem că arcul trage pe axa Z
		float rawDraw =
			-localHandPos.z;

		float drawDistance =
			Mathf.Clamp(rawDraw, 0f, maxDrawDistance);

		smoothDraw = Mathf.Lerp(smoothDraw, drawDistance, Time.deltaTime * 20f);

		currentDrawAmount = smoothDraw / maxDrawDistance;

		// mută coarda
		stringGrabPoint.localPosition =
			new Vector3(
				stringGrabPoint.localPosition.x,
				stringGrabPoint.localPosition.y,
				-drawDistance
			);



		// mută săgeata împreună cu coarda
		if (currentArrow != null)
		{
			currentArrow.transform.position = stringGrabPoint.position;
			currentArrow.transform.rotation = arrowSpawnPoint.rotation;
		}


		Debug.Log(
			"[DEBUG] Draw Distance = " +
			drawDistance +
			" Draw Amount = " +
			currentDrawAmount
		);
	}

	void ReleaseArrow()
	{
		isDrawing = false;

		if (currentArrow == null)
		{
			Debug.LogError("[DEBUG] currentArrow NULL on release!");
			return;
		}

		eventBridge?.OnArrowReleased();

		float force = currentDrawAmount * maxLaunchForce;

		Debug.Log("[DEBUG] Launch Force = " + force);

		if (force < 0.1f)
		{
			Debug.LogWarning("[DEBUG] Force too small!");
		}

		if (currentArrowController != null)
		{
			Debug.Log("[DEBUG] Launching with ArrowController");

			currentArrowController.Launch(
				arrowSpawnPoint.forward,
				force
			);
		}
		else
		{
			Debug.Log("[DEBUG] Launch fallback mode");

			arrowRb.isKinematic = false;
			arrowRb.useGravity = true;

			arrowRb.AddForce(
				arrowSpawnPoint.forward * force,
				ForceMode.VelocityChange
			);

		}

		Debug.DrawRay(
			arrowSpawnPoint.position,
			arrowSpawnPoint.forward * 2f,
			Color.red,
			5f
		);

		stringGrabPoint.localPosition =
			stringRestPoint.localPosition;

		currentArrow = null;
		currentArrowController = null;
		currentDrawAmount = 0f;
	}
}