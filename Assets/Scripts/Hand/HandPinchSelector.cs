using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Hands;

public class HandPinchSelector : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor interactor;
    public Handedness handedness = Handedness.Left;

    private XRHandSubsystem handSubsystem;

    void OnEnable()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];
    }

    void Update()
    {
        if (handSubsystem == null) return;

        var hand = handedness == Handedness.Left ?
            handSubsystem.leftHand : handSubsystem.rightHand;

        if (!hand.isTracked) return;

        hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose);
        hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose);

        float distance = Vector3.Distance(indexPose.position, thumbPose.position);

        // Pinch detectat când degetele sunt < 3cm
        if (distance < 0.03f)
            interactor.allowSelect = true;
        else
            interactor.allowSelect = false;
    }
}