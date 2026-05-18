using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Ataseaza asta pe un mic collider invizibil pe coarda arcului
public class DrawInteractable : XRBaseInteractable
{
    private BowController bow;

    void Awake()
    {
        bow = GetComponentInParent<BowController>();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        bow.StartDraw(args.interactorObject as XRBaseInteractor);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        bow.Release();
    }
}