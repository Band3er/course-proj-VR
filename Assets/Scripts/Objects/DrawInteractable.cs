using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Debug = UnityEngine.Debug;

public class DrawInteractable : XRBaseInteractable
{
    private BowController bowController;

    protected override void Awake()
    {
        base.Awake();

        bowController = GetComponentInParent<BowController>();

        Debug.Log("[DrawInteractable] Awake. Bow found: " + (bowController != null));
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        Debug.Log("[DrawInteractable] String grabbed by: " + args.interactorObject.transform.name);

        if (bowController == null)
        {
            Debug.LogWarning("[DrawInteractable] Cannot start draw: BowController is missing.");
            return;
        }

        bowController.StartDraw(args.interactorObject.transform);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        Debug.Log("[DrawInteractable] String released.");

        if (bowController == null)
        {
            Debug.LogWarning("[DrawInteractable] Cannot release: BowController is missing.");
            return;
        }

        bowController.Release();
    }
}