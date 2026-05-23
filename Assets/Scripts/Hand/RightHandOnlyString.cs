using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class RightHandOnlyString : MonoBehaviour
{
	private GrabInteractable grabInteractable;

	void Awake()
	{
		grabInteractable = GetComponent<GrabInteractable>();
	}

	void OnEnable()
	{
		if (grabInteractable == null) return;
		grabInteractable.WhenSelectingInteractorAdded.Action += OnGrabbed;
	}

	void OnDisable()
	{
		if (grabInteractable == null) return;
		grabInteractable.WhenSelectingInteractorAdded.Action -= OnGrabbed;
	}

	private void OnGrabbed(GrabInteractor interactor)
	{
		// Verifica daca interactorul vine de la mana dreapta
		var handGrab = interactor.GetComponentInParent<HandGrabInteractor>();
		if (handGrab != null &&
			handGrab.Hand.Handedness != Oculus.Interaction.Input.Handedness.Right)
		{
			interactor.ForceRelease();
			Debug.Log("[DEBUG] Rejected left hand on String.");
		}
	}
}