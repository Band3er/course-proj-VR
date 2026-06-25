using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestArchery.Finalization
{
    [DefaultExecutionOrder(32700)]
    [DisallowMultipleComponent]
    public sealed class GameplayHUDAndBowSessionFix :
        MonoBehaviour
    {
        [Header("Core references")]
        [SerializeField]
        private Transform eyeTransform;

        [SerializeField]
        private Transform bowRoot;

        [SerializeField]
        private BowEyeLevelStartPlacement initialBowPlacement;

        [Header("HUD references")]
        [SerializeField]
        private RectTransform trajectoryButtonRect;

        [SerializeField]
        private RectTransform scoreHudRootRect;

        [Header("Runtime")]
        [SerializeField]
        private bool enforceHudAlignmentEveryFrame = true;

        [SerializeField]
        private float alignmentTolerance = 0.03f;

        private bool hasStoredBowPose;
        private Vector3 storedBowLocalOffset;
        private Quaternion storedBowLocalRotation;
        private Rigidbody[] bowRigidbodies;
        private bool lastGameplayVisible;

        public bool HasRequiredReferences =>
            eyeTransform != null &&
            bowRoot != null &&
            trajectoryButtonRect != null &&
            scoreHudRootRect != null;

        public bool HasStoredBowPose =>
            hasStoredBowPose;

        public void ResolveReferences()
        {
            if (eyeTransform == null)
            {
                eyeTransform =
                    FindByExactSuffix(
                        "[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor");
            }

            if (bowRoot == null)
            {
                GameObject bow =
                    FindSceneObjectExact(
                        "Bow");

                bowRoot =
                    bow != null
                        ? bow.transform
                        : null;
            }

            if (
                initialBowPlacement == null &&
                bowRoot != null
            )
            {
                initialBowPlacement =
                    bowRoot.GetComponent<BowEyeLevelStartPlacement>();
            }

            if (trajectoryButtonRect == null)
            {
                GameObject trajectoryButton =
                    FindSceneObjectExact(
                        "TrajectoryButton");

                trajectoryButtonRect =
                    trajectoryButton != null
                        ? trajectoryButton.transform as RectTransform
                        : null;
            }

            if (scoreHudRootRect == null)
            {
                Transform scoreHud =
                    FindByExactSuffix(
                        "[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor/WildlifeHUD_RabbitPrototype");

                scoreHudRootRect =
                    scoreHud as RectTransform;
            }

            if (
                bowRoot != null &&
                bowRigidbodies == null
            )
            {
                bowRigidbodies =
                    bowRoot.GetComponentsInChildren<Rigidbody>(
                        true);
            }
        }

        private void Awake()
        {
            ResolveReferences();
            TryCacheBowPose();
            ApplyHudAlignment();
            lastGameplayVisible =
                IsGameplayVisible();
        }

        private void OnEnable()
        {
            ResolveReferences();
            TryCacheBowPose();
            ApplyHudAlignment();
            lastGameplayVisible =
                IsGameplayVisible();
        }

        private void LateUpdate()
        {
            ResolveReferences();
            TryCacheBowPose();

            if (enforceHudAlignmentEveryFrame)
            {
                ApplyHudAlignment();
            }

            bool gameplayVisible =
                IsGameplayVisible();

            if (
                gameplayVisible &&
                !lastGameplayVisible
            )
            {
                RespawnBowInFrontOfCurrentPlayer();
            }

            lastGameplayVisible =
                gameplayVisible;
        }

        private bool IsGameplayVisible()
        {
            return
                trajectoryButtonRect != null &&
                trajectoryButtonRect.gameObject.activeInHierarchy;
        }

        private void TryCacheBowPose()
        {
            if (
                hasStoredBowPose ||
                eyeTransform == null ||
                bowRoot == null
            )
            {
                return;
            }

            if (
                initialBowPlacement != null &&
                !initialBowPlacement.PlacementCompleted
            )
            {
                return;
            }

            storedBowLocalOffset =
                Quaternion.Inverse(
                    eyeTransform.rotation) *
                (
                    bowRoot.position -
                    eyeTransform.position
                );

            storedBowLocalRotation =
                Quaternion.Inverse(
                    eyeTransform.rotation) *
                bowRoot.rotation;

            hasStoredBowPose =
                true;
        }

        public void RespawnBowInFrontOfCurrentPlayer()
        {
            TryCacheBowPose();

            if (
                !hasStoredBowPose ||
                eyeTransform == null ||
                bowRoot == null
            )
            {
                return;
            }

            Vector3 targetPosition =
                eyeTransform.position +
                eyeTransform.rotation *
                storedBowLocalOffset;

            Quaternion targetRotation =
                eyeTransform.rotation *
                storedBowLocalRotation;

            if (!bowRoot.gameObject.activeSelf)
            {
                bowRoot.gameObject.SetActive(
                    true);
            }

            bowRoot.SetPositionAndRotation(
                targetPosition,
                targetRotation);

            if (bowRigidbodies != null)
            {
                foreach (Rigidbody body in bowRigidbodies)
                {
                    if (body == null)
                    {
                        continue;
                    }

                    body.position =
                        body.transform.position;

                    body.rotation =
                        body.transform.rotation;

                    body.linearVelocity =
                        Vector3.zero;

                    body.angularVelocity =
                        Vector3.zero;
                }
            }

            Debug.Log(
                "[STAGE 10H V2] Bow respawned relative to the current player pose.");
        }

        public void ApplyHudAlignment()
        {
            if (
                eyeTransform == null ||
                trajectoryButtonRect == null ||
                scoreHudRootRect == null
            )
            {
                return;
            }

            Canvas trajectoryCanvas =
                trajectoryButtonRect.GetComponentInParent<Canvas>(
                    true);

            if (trajectoryCanvas != null)
            {
                scoreHudRootRect.rotation =
                    trajectoryCanvas.transform.rotation;
            }

            Vector3 trajectoryCenterWorld =
                trajectoryButtonRect.TransformPoint(
                    trajectoryButtonRect.rect.center);

            Vector3 scoreCenterWorld =
                scoreHudRootRect.TransformPoint(
                    scoreHudRootRect.rect.center);

            Vector3 trajectoryCenterLocal =
                eyeTransform.InverseTransformPoint(
                    trajectoryCenterWorld);

            Vector3 scoreCenterLocal =
                eyeTransform.InverseTransformPoint(
                    scoreCenterWorld);

            Vector3 desiredScoreCenterLocal =
                new Vector3(
                    scoreCenterLocal.x,
                    trajectoryCenterLocal.y,
                    trajectoryCenterLocal.z);

            Vector3 desiredScoreCenterWorld =
                eyeTransform.TransformPoint(
                    desiredScoreCenterLocal);

            scoreHudRootRect.position +=
                desiredScoreCenterWorld -
                scoreCenterWorld;
        }

        public bool IsHudAligned(
            float tolerance)
        {
            if (
                eyeTransform == null ||
                trajectoryButtonRect == null ||
                scoreHudRootRect == null
            )
            {
                return false;
            }

            Vector3 trajectoryCenterLocal =
                eyeTransform.InverseTransformPoint(
                    trajectoryButtonRect.TransformPoint(
                        trajectoryButtonRect.rect.center));

            Vector3 scoreCenterLocal =
                eyeTransform.InverseTransformPoint(
                    scoreHudRootRect.TransformPoint(
                        scoreHudRootRect.rect.center));

            return
                Mathf.Abs(
                    trajectoryCenterLocal.y -
                    scoreCenterLocal.y) <=
                tolerance &&
                Mathf.Abs(
                    trajectoryCenterLocal.z -
                    scoreCenterLocal.z) <=
                tolerance;
        }

        public bool IsHudAligned()
        {
            return
                IsHudAligned(
                    alignmentTolerance);
        }

        private static Transform FindByExactSuffix(
            string requiredSuffix)
        {
            Transform[] transforms =
                Resources.FindObjectsOfTypeAll<Transform>();

            List<Transform> matches =
                new List<Transform>();

            foreach (Transform item in transforms)
            {
                if (
                    item == null ||
                    !item.gameObject.scene.IsValid() ||
                    !item.gameObject.scene.isLoaded
                )
                {
                    continue;
                }

                string path =
                    GetHierarchyPath(
                        item);

                if (
                    path.EndsWith(
                        requiredSuffix,
                        StringComparison.OrdinalIgnoreCase)
                )
                {
                    matches.Add(
                        item);
                }
            }

            return
                matches.Count == 1
                    ? matches[0]
                    : null;
        }

        private static GameObject FindSceneObjectExact(
            string objectName)
        {
            Transform[] transforms =
                Resources.FindObjectsOfTypeAll<Transform>();

            foreach (Transform item in transforms)
            {
                if (
                    item != null &&
                    item.gameObject.scene.IsValid() &&
                    item.gameObject.scene.isLoaded &&
                    item.gameObject.name ==
                        objectName
                )
                {
                    return
                        item.gameObject;
                }
            }

            return null;
        }

        private static string GetHierarchyPath(
            Transform item)
        {
            if (item == null)
            {
                return "<null>";
            }

            string path =
                item.name;

            Transform current =
                item.parent;

            while (current != null)
            {
                path =
                    current.name +
                    "/" +
                    path;

                current =
                    current.parent;
            }

            return path;
        }
    }
}