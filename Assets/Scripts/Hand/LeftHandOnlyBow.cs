using UnityEngine;
using Oculus.Interaction.HandGrab;

public class LeftHandOnlyBow : MonoBehaviour
{
	private HandGrabInteractable handGrabInteractable;

	void Awake()
	{
		handGrabInteractable = GetComponent<HandGrabInteractable>();
	}

	void OnEnable()
	{
		if (handGrabInteractable == null) return;
		handGrabInteractable.WhenSelectingInteractorAdded.Action += OnGrabbed;
	}

	void OnDisable()
	{
		if (handGrabInteractable == null) return;
		handGrabInteractable.WhenSelectingInteractorAdded.Action -= OnGrabbed;
	}

	private void OnGrabbed(HandGrabInteractor interactor)
	{
		// Daca nu e mana stanga, forteaza release
		if (interactor.Hand.Handedness != Oculus.Interaction.Input.Handedness.Left)
		{
			interactor.ForceRelease();
			Debug.Log("[DEBUG] Rejected right hand on Bow.");
		}
	}
}