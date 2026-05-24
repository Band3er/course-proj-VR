using Oculus.Interaction.Input;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>
/// Add this component to the Bow GameObject.
/// Then drag it into the Bow's HandGrabInteractable → Interactor Filters list.
/// Set Allowed Handedness = Left so only the left hand can grab the bow.
/// </summary>
public class HandednessFilter : MonoBehaviour, IGameObjectFilter
{
	[SerializeField]
	private Handedness _allowedHandedness = Handedness.Left;

	public bool Filter(GameObject go)
	{
		HandGrabInteractor interactor = go.GetComponent<HandGrabInteractor>();

		// If it's not a HandGrabInteractor, allow it through
		if (interactor == null) return true;

		return interactor.Hand.Handedness == _allowedHandedness;
	}
}