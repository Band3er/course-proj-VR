using UnityEngine;

public class InteractionModeManager : MonoBehaviour
{
	public enum InteractionMode { Controllers, HandTracking }
	public InteractionMode currentMode = InteractionMode.HandTracking;

	public OVRHand leftHand, rightHand;
	public GameObject controllerLeft, controllerRight;

	public void SwitchMode(InteractionMode mode)
	{
		currentMode = mode;
		bool useHands = (mode == InteractionMode.HandTracking);

		// Arata/ascunde controllere vizuale
		controllerLeft.SetActive(!useHands);
		controllerRight.SetActive(!useHands);

		// Activeaza/dezactiveaza hand tracking rendering
		leftHand.gameObject.SetActive(useHands);
		rightHand.gameObject.SetActive(useHands);
	}
}