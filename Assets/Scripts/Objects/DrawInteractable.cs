using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DrawInteractable : XRBaseInteractable
{
    private BowController bow;

    void Awake()
    {
        bow = GetComponentInParent<BowController>();
        Debug.Log("DrawInteractable Awake - Bow found: " + (bow != null));
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        Debug.Log("STRING GRABBED! Interactor: " + args.interactorObject.GetType().Name);
        bow.StartDraw(args.interactorObject as XRBaseInteractor);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        Debug.Log("STRING RELEASED!");
        bow.Release();
    }
}