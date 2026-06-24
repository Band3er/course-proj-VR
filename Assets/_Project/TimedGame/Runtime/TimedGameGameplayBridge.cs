using System;
using System.Reflection;
using ForestArchery.Wildlife;
using UnityEngine;

namespace ForestArchery.TimedGame
{
    public sealed class TimedGameGameplayBridge : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField]
        private TimedRoundController roundController;

        [SerializeField]
        private WildlifeScoreManager scoreManager;

        [SerializeField]
        private global::BowDrawController bowController;

        [SerializeField]
        private global::ArrowTrajectoryPreview trajectoryPreview;

        [Header("Gameplay World")]
        [SerializeField]
        private WildlifeSpawnManager groundSpawnManager;

        [SerializeField]
        private global::lb_BirdController birdController;

        [SerializeField]
        private GameObject trajectoryHudRoot;

        [SerializeField]
        private GameObject scoreHudRoot;

        [Header("Round Reset")]
        [SerializeField]
        private bool clearExistingArrowsAtRoundStart = true;

        [SerializeField]
        private bool resetSceneOnRoundExit = true;

        [SerializeField]
        private bool verboseLogging;

        private Transform bowInitialParent;
        private Vector3 bowInitialLocalPosition;
        private Quaternion bowInitialLocalRotation;
        private Vector3 bowInitialLocalScale;

        private Rigidbody bowRigidbody;
        private bool bowInitialActive;
        private bool bowInitialKinematic;
        private bool bowInitialUseGravity;

        private bool trajectoryHudInitiallyActive;
        private bool scoreHudInitiallyActive;
        private bool groundSpawnInitiallyEnabled;

        private bool roundPrepared;
        private bool subscribed;

        private bool birdSimulationShouldRun;
        private bool pendingBirdReset;

        private FieldInfo birdPauseField;
        private FieldInfo birdArrayField;
        private MethodInfo birdUnspawnMethod;

        private void Awake()
        {
            ResolveReferences();
            CaptureInitialState();
            ResolveBirdReflection();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ResolveBirdReflection();
            Subscribe();

            ApplyState(
                roundController != null
                    ? roundController.State
                    : TimedRoundState.Idle);
        }

        private void Update()
        {
            ApplyPendingBirdState();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (!Application.isPlaying)
            {
                return;
            }

            SetBirdSimulationRunning(
                true);

            if (groundSpawnManager != null)
            {
                groundSpawnManager.enabled =
                    groundSpawnInitiallyEnabled;
            }

            SetGameplayPresentationVisible(
                true);
        }

        private void ResolveReferences()
        {
            if (roundController == null)
            {
                roundController =
                    GetComponent<TimedRoundController>();
            }

            if (scoreManager == null)
            {
                scoreManager =
                    WildlifeScoreManager.Instance;

                if (scoreManager == null)
                {
                    scoreManager =
                        FindFirstObjectByType<WildlifeScoreManager>();
                }
            }

            if (bowController == null)
            {
                bowController =
                    FindFirstObjectByType<global::BowDrawController>();
            }

            if (trajectoryPreview == null)
            {
                trajectoryPreview =
                    FindFirstObjectByType<global::ArrowTrajectoryPreview>();
            }

            if (groundSpawnManager == null)
            {
                groundSpawnManager =
                    FindFirstObjectByType<WildlifeSpawnManager>();
            }

            if (birdController == null)
            {
                birdController =
                    FindFirstObjectByType<global::lb_BirdController>();
            }

            if (
                trajectoryHudRoot == null
            )
            {
                trajectoryHudRoot =
                    GameObject.Find(
                        "TrajectoryToggleHUD");
            }

            if (
                scoreHudRoot == null
            )
            {
                scoreHudRoot =
                    GameObject.Find(
                        "WildlifeHUD_RabbitPrototype");
            }
        }

        private void CaptureInitialState()
        {
            if (bowController != null)
            {
                Transform bowTransform =
                    bowController.transform;

                bowInitialParent =
                    bowTransform.parent;

                bowInitialLocalPosition =
                    bowTransform.localPosition;

                bowInitialLocalRotation =
                    bowTransform.localRotation;

                bowInitialLocalScale =
                    bowTransform.localScale;

                bowInitialActive =
                    bowController.gameObject.activeSelf;

                bowRigidbody =
                    bowController.GetComponent<Rigidbody>();

                if (bowRigidbody != null)
                {
                    bowInitialKinematic =
                        bowRigidbody.isKinematic;

                    bowInitialUseGravity =
                        bowRigidbody.useGravity;
                }
            }

            trajectoryHudInitiallyActive =
                trajectoryHudRoot == null ||
                trajectoryHudRoot.activeSelf;

            scoreHudInitiallyActive =
                scoreHudRoot == null ||
                scoreHudRoot.activeSelf;

            groundSpawnInitiallyEnabled =
                groundSpawnManager == null ||
                groundSpawnManager.enabled;
        }

        private void ResolveBirdReflection()
        {
            if (birdController == null)
            {
                return;
            }

            Type controllerType =
                birdController.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.NonPublic;

            birdPauseField =
                controllerType.GetField(
                    "pause",
                    flags);

            birdArrayField =
                controllerType.GetField(
                    "myBirds",
                    flags);

            birdUnspawnMethod =
                controllerType.GetMethod(
                    "Unspawn",
                    flags);
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (roundController != null)
            {
                roundController.StateChanged +=
                    HandleRoundStateChanged;
            }

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged +=
                    HandleScoreChanged;
            }

            global::ArrowController.AnyArrowLaunched +=
                HandleArrowLaunched;

            subscribed =
                true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (roundController != null)
            {
                roundController.StateChanged -=
                    HandleRoundStateChanged;
            }

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged -=
                    HandleScoreChanged;
            }

            global::ArrowController.AnyArrowLaunched -=
                HandleArrowLaunched;

            subscribed =
                false;
        }

        private void HandleRoundStateChanged(
            TimedRoundState newState)
        {
            ApplyState(
                newState);
        }

        private void ApplyState(
            TimedRoundState state)
        {
            switch (state)
            {
                case TimedRoundState.Countdown:
                    PrepareRound();

                    SetGameplayPresentationVisible(
                        false);

                    if (scoreManager != null)
                    {
                        scoreManager.SetScoringEnabled(
                            false);
                    }

                    break;

                case TimedRoundState.Playing:
                    if (!roundPrepared)
                    {
                        PrepareRound();
                    }

                    SetGroundWildlifeRunning(
                        true);

                    SetBirdSimulationRunning(
                        true);

                    if (scoreManager != null)
                    {
                        scoreManager.SetScoringEnabled(
                            true);

                        roundController?.UpdateScoreSnapshot(
                            scoreManager.TotalScore,
                            scoreManager.TotalHits);
                    }

                    SetGameplayPresentationVisible(
                        true);

                    break;

                case TimedRoundState.Paused:
                    if (scoreManager != null)
                    {
                        scoreManager.SetScoringEnabled(
                            false);
                    }

                    SetGameplayPresentationVisible(
                        false);

                    SetGroundWildlifeRunning(
                        false);

                    SetBirdSimulationRunning(
                        false);

                    break;

                case TimedRoundState.Results:
                case TimedRoundState.Cancelled:
                case TimedRoundState.Idle:
                default:
                    if (scoreManager != null)
                    {
                        scoreManager.SetScoringEnabled(
                            false);
                    }

                    SetGameplayPresentationVisible(
                        false);

                    SetGroundWildlifeRunning(
                        false);

                    SetBirdSimulationRunning(
                        false);

                    if (resetSceneOnRoundExit)
                    {
                        ResetSceneState();
                    }

                    roundPrepared =
                        false;

                    break;
            }

            if (verboseLogging)
            {
                Debug.Log(
                    "[TIMED GAME BRIDGE]" +
                    " state=" +
                    state +
                    " | scoring=" +
                    (
                        scoreManager != null &&
                        scoreManager.ScoringEnabled
                    ) +
                    " | bowVisible=" +
                    (
                        bowController != null &&
                        bowController.gameObject.activeSelf
                    ));
            }
        }

        private void PrepareRound()
        {
            SetGameplayPresentationVisible(
                false);

            SetGroundWildlifeRunning(
                false);

            SetBirdSimulationRunning(
                false);

            if (scoreManager != null)
            {
                scoreManager.SetScoringEnabled(
                    false);

                scoreManager.ResetScore();
            }

            ResetSceneState();

            SetGroundWildlifeRunning(
                true);

            SetBirdSimulationRunning(
                true);

            roundPrepared =
                true;

            if (verboseLogging)
            {
                Debug.Log(
                    "[TIMED GAME BRIDGE] Round prepared." +
                    " Score, arrows, animals, birds and bow were reset.");
            }
        }

        private void HandleScoreChanged(
            int totalScore,
            int totalHits)
        {
            if (
                roundController == null ||
                !roundController.IsScoringAllowed
            )
            {
                return;
            }

            roundController.UpdateScoreSnapshot(
                totalScore,
                totalHits);

            if (verboseLogging)
            {
                Debug.Log(
                    "[TIMED GAME BRIDGE] Score synchronized" +
                    " | score=" +
                    totalScore +
                    " | hits=" +
                    totalHits);
            }
        }

        private void HandleArrowLaunched(
            global::ArrowController arrow)
        {
            if (
                roundController == null ||
                !roundController.IsGameplayAllowed
            )
            {
                return;
            }

            roundController.RegisterArrowLaunched();

            if (verboseLogging)
            {
                Debug.Log(
                    "[TIMED GAME BRIDGE] Arrow counted" +
                    " | total=" +
                    roundController
                        .Session
                        .ArrowsLaunched);
            }
        }

        private void SetGameplayPresentationVisible(
            bool visible)
        {
            if (bowController != null)
            {
                bool desiredBowState =
                    visible &&
                    bowInitialActive;

                if (
                    bowController.gameObject.activeSelf !=
                    desiredBowState
                )
                {
                    bowController.gameObject.SetActive(
                        desiredBowState);
                }
            }

            if (trajectoryHudRoot != null)
            {
                bool desiredTrajectoryHudState =
                    visible &&
                    trajectoryHudInitiallyActive;

                if (
                    trajectoryHudRoot.activeSelf !=
                    desiredTrajectoryHudState
                )
                {
                    trajectoryHudRoot.SetActive(
                        desiredTrajectoryHudState);
                }
            }

            if (scoreHudRoot != null)
            {
                bool desiredScoreHudState =
                    visible &&
                    scoreHudInitiallyActive;

                if (
                    scoreHudRoot.activeSelf !=
                    desiredScoreHudState
                )
                {
                    scoreHudRoot.SetActive(
                        desiredScoreHudState);
                }
            }
        }

        private void SetGroundWildlifeRunning(
            bool running)
        {
            if (groundSpawnManager == null)
            {
                return;
            }

            bool desiredState =
                running &&
                groundSpawnInitiallyEnabled;

            groundSpawnManager.enabled =
                desiredState;
        }

        private void SetBirdSimulationRunning(
            bool running)
        {
            birdSimulationShouldRun =
                running;

            if (birdController == null)
            {
                return;
            }

            if (birdPauseField != null)
            {
                birdPauseField.SetValue(
                    birdController,
                    !running);
            }

            GameObject[] birds =
                GetBirdArray();

            if (birds == null)
            {
                return;
            }

            if (running)
            {
                birdController.AllUnPause();
            }
            else
            {
                birdController.AllPause();
            }
        }

        private void ApplyPendingBirdState()
        {
            if (birdController == null)
            {
                return;
            }

            GameObject[] birds =
                GetBirdArray();

            if (birds == null)
            {
                return;
            }

            if (pendingBirdReset)
            {
                ResetBirds();

                pendingBirdReset =
                    false;
            }

            if (birdSimulationShouldRun)
            {
                if (birdPauseField != null)
                {
                    birdPauseField.SetValue(
                        birdController,
                        false);
                }
            }
            else
            {
                if (birdPauseField != null)
                {
                    birdPauseField.SetValue(
                        birdController,
                        true);
                }
            }
        }

        private GameObject[] GetBirdArray()
        {
            if (
                birdController == null ||
                birdArrayField == null
            )
            {
                return null;
            }

            return
                birdArrayField.GetValue(
                    birdController)
                as GameObject[];
        }

        private void ResetSceneState()
        {
            ClearExistingArrows();
            ResetGroundAnimals();
            ResetBirds();
            ResetBowTransform();

            if (scoreManager != null)
            {
                scoreManager.ResetScore();
            }
        }

        private void ClearExistingArrows()
        {
            if (!clearExistingArrowsAtRoundStart)
            {
                return;
            }

            global::ArrowController[] arrows =
                FindObjectsByType<global::ArrowController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int removedCount =
                0;

            foreach (
                global::ArrowController arrow in
                arrows
            )
            {
                if (arrow == null)
                {
                    continue;
                }

                Destroy(
                    arrow.gameObject);

                removedCount++;
            }

            if (
                verboseLogging &&
                removedCount > 0
            )
            {
                Debug.Log(
                    "[TIMED GAME BRIDGE] Cleared arrows=" +
                    removedCount);
            }
        }

        private void ResetGroundAnimals()
        {
            WildlifeAnimal[] animals =
                FindObjectsByType<WildlifeAnimal>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            int resetCount =
                0;

            foreach (
                WildlifeAnimal animal in
                animals
            )
            {
                if (
                    animal == null ||
                    !animal.gameObject.activeInHierarchy
                )
                {
                    continue;
                }

                animal.ForceDespawn();

                resetCount++;
            }

            if (
                verboseLogging &&
                resetCount > 0
            )
            {
                Debug.Log(
                    "[TIMED GAME BRIDGE] Reset ground animals=" +
                    resetCount);
            }
        }

        private void ResetBirds()
        {
            if (birdController == null)
            {
                return;
            }

            GameObject[] birds =
                GetBirdArray();

            if (birds == null)
            {
                pendingBirdReset =
                    true;

                if (birdPauseField != null)
                {
                    birdPauseField.SetValue(
                        birdController,
                        true);
                }

                return;
            }

            int resetCount =
                0;

            if (birdUnspawnMethod != null)
            {
                foreach (GameObject bird in birds)
                {
                    if (
                        bird == null ||
                        !bird.activeSelf
                    )
                    {
                        continue;
                    }

                    birdUnspawnMethod.Invoke(
                        birdController,
                        new object[]
                        {
                            bird
                        });

                    resetCount++;
                }
            }
            else
            {
                foreach (GameObject bird in birds)
                {
                    if (
                        bird != null &&
                        bird.activeSelf
                    )
                    {
                        bird.SetActive(
                            false);

                        resetCount++;
                    }
                }
            }

            ParticleSystem[] particles =
                birdController
                    .GetComponentsInChildren<ParticleSystem>(
                        true);

            foreach (
                ParticleSystem particle in
                particles
            )
            {
                if (particle == null)
                {
                    continue;
                }

                particle.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear);
            }

            SetBirdSimulationRunning(
                false);

            if (
                verboseLogging &&
                resetCount > 0
            )
            {
                Debug.Log(
                    "[TIMED GAME BRIDGE] Reset birds=" +
                    resetCount);
            }
        }

        private void ResetBowTransform()
        {
            if (bowController == null)
            {
                return;
            }

            GameObject bowObject =
                bowController.gameObject;

            bool wasActive =
                bowObject.activeSelf;

            if (wasActive)
            {
                bowObject.SetActive(
                    false);
            }

            Transform bowTransform =
                bowController.transform;

            bowTransform.SetParent(
                bowInitialParent,
                false);

            bowTransform.localPosition =
                bowInitialLocalPosition;

            bowTransform.localRotation =
                bowInitialLocalRotation;

            bowTransform.localScale =
                bowInitialLocalScale;

            if (bowRigidbody != null)
            {
                bowRigidbody.linearVelocity =
                    Vector3.zero;

                bowRigidbody.angularVelocity =
                    Vector3.zero;

                bowRigidbody.isKinematic =
                    bowInitialKinematic;

                bowRigidbody.useGravity =
                    bowInitialUseGravity;
            }
        }
    }
}
