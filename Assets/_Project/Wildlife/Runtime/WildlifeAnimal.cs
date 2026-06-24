using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace ForestArchery.Wildlife
{
    public enum WildlifeAnimalState
    {
        Idle,
        Running,
        Dead
    }

    public sealed class WildlifeAnimal : MonoBehaviour
    {
        [SerializeField] private WildlifeSpeciesDefinition definition;
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private WildlifeHitbox[] hitboxes;

        private WildlifeSpawnManager owner;
        private Transform player;
        private WildlifeAnimalState state;
        private bool initialized;
        private bool hitAccepted;
        private float stateTimer;
        private float activeLifetime;
        private float awarenessTimer;
        private Quaternion visualStartRotation;
        private Vector3 visualStartPosition;
        private Coroutine deathRoutine;

        public WildlifeSpeciesDefinition Definition => definition;
        public WildlifeAnimalState State => state;
        public bool IsAlive =>
            initialized &&
            state != WildlifeAnimalState.Dead;

        public void Configure(
            WildlifeSpeciesDefinition speciesDefinition,
            Animator animalAnimator,
            NavMeshAgent navMeshAgent,
            Transform visual,
            WildlifeHitbox[] configuredHitboxes)
        {
            definition = speciesDefinition;
            animator = animalAnimator;
            agent = navMeshAgent;
            visualRoot = visual;
            hitboxes = configuredHitboxes;
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (visualRoot == null && animator != null)
            {
                visualRoot = animator.transform;
            }

            if (hitboxes == null || hitboxes.Length == 0)
            {
                hitboxes = GetComponentsInChildren<WildlifeHitbox>(true);
            }

            CacheVisualTransform();
        }

        private void Update()
        {
            if (
                !initialized ||
                definition == null ||
                state == WildlifeAnimalState.Dead
            )
            {
                return;
            }

            activeLifetime -= Time.deltaTime;

            if (activeLifetime <= 0f)
            {
                owner?.ReturnToPool(this);
                return;
            }

            awarenessTimer -= Time.deltaTime;

            if (awarenessTimer <= 0f)
            {
                awarenessTimer = 0.2f;
                TryFleeFromPlayer();
            }

            switch (state)
            {
                case WildlifeAnimalState.Idle:
                    UpdateIdle();
                    break;

                case WildlifeAnimalState.Running:
                    UpdateRunning();
                    break;
            }
        }

        public void Activate(
            WildlifeSpawnManager spawnOwner,
            Transform playerTransform,
            Vector3 position,
            Quaternion rotation)
        {
            owner = spawnOwner;
            player = playerTransform;
            initialized = true;
            hitAccepted = false;
            awarenessTimer = 0f;

            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            transform.SetPositionAndRotation(position, rotation);

            RestoreVisualTransform();
            EnableHitboxes(true);

            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.Rebind();
                animator.Update(0f);
            }

            if (
                agent != null &&
                definition.movementMode ==
                    WildlifeMovementMode.GroundNavMesh
            )
            {
                agent.enabled = false;
                transform.SetPositionAndRotation(position, rotation);
                agent.enabled = true;

                agent.speed = Random.Range(
                    definition.movementSpeedRange.x,
                    definition.movementSpeedRange.y);

                agent.angularSpeed = definition.angularSpeed;
                agent.acceleration = definition.acceleration;
                agent.stoppingDistance = definition.stoppingDistance;
                agent.autoBraking = true;

                if (agent.isOnNavMesh)
                {
                    agent.Warp(position);
                    agent.ResetPath();
                    agent.isStopped = true;
                }
            }

            activeLifetime = Random.Range(
                definition.activeLifetimeRange.x,
                definition.activeLifetimeRange.y);

            EnterIdle(randomizeAnimationTime: true);

            Debug.Log(
                "[WILDLIFE] Spawned" +
                " | species=" + definition.displayName +
                " | position=" + position);
        }

        public void TryReceiveArrowHit(
            float scoreMultiplier,
            string hitLabel,
            Vector3 hitPoint,
            Vector3 arrowVelocity)
        {
            if (
                !initialized ||
                hitAccepted ||
                state == WildlifeAnimalState.Dead ||
                definition == null
            )
            {
                return;
            }

            hitAccepted = true;

            bool targetWasMoving =
                state == WildlifeAnimalState.Running;

            WildlifeScoreBreakdown dynamicScore =
                WildlifeDynamicScore.Calculate(
                    definition,
                    scoreMultiplier,
                    hitPoint,
                    targetWasMoving,
                    false);

            int awardedScore =
                dynamicScore.finalScore;

            WildlifeScoreManager.Instance?.RegisterHit(
                definition,
                awardedScore,
                hitLabel);

            Debug.Log(
                WildlifeDynamicScore.BuildDebugText(
                    definition,
                    dynamicScore));

            EnterDead(hitPoint, arrowVelocity);

            Debug.Log(
                "[WILDLIFE] Animal killed" +
                " | species=" + definition.displayName +
                " | hitbox=" + hitLabel +
                " | point=" + hitPoint);
        }

        public void PrepareForPool()
        {
            initialized = false;
            hitAccepted = false;
            owner = null;
            player = null;

            StopAllCoroutines();
            deathRoutine = null;

            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                }

                agent.enabled = false;
            }

            EnableHitboxes(false);
            RestoreVisualTransform();

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.enabled = false;
            }
        }

        public void ForceDespawn()
        {
            owner?.ReturnToPool(this);
        }

        private void UpdateIdle()
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer <= 0f)
            {
                TryStartMovement(Vector3.zero);
            }
        }

        private void UpdateRunning()
        {
            if (
                agent == null ||
                !agent.enabled ||
                !agent.isOnNavMesh
            )
            {
                EnterIdle();
                return;
            }

            if (
                !agent.pathPending &&
                (
                    !agent.hasPath ||
                    agent.remainingDistance <=
                        agent.stoppingDistance + 0.08f
                )
            )
            {
                EnterIdle();
            }
        }

        private void TryFleeFromPlayer()
        {
            if (
                player == null ||
                definition == null ||
                state == WildlifeAnimalState.Dead
            )
            {
                return;
            }

            Vector3 toAnimal =
                transform.position - player.position;

            toAnimal.y = 0f;

            if (
                toAnimal.sqrMagnitude <=
                definition.fleeDistance *
                definition.fleeDistance
            )
            {
                TryStartMovement(
                    toAnimal.sqrMagnitude > 0.001f
                        ? toAnimal.normalized
                        : transform.forward);
            }
        }

        private bool TryStartMovement(
            Vector3 preferredDirection)
        {
            if (
                definition == null ||
                agent == null ||
                !agent.enabled ||
                !agent.isOnNavMesh
            )
            {
                EnterIdle();
                return false;
            }

            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3 direction;

                if (preferredDirection.sqrMagnitude > 0.001f)
                {
                    Vector2 jitter =
                        Random.insideUnitCircle * 0.35f;

                    direction = new Vector3(
                        preferredDirection.x + jitter.x,
                        0f,
                        preferredDirection.z + jitter.y);
                }
                else
                {
                    Vector2 randomDirection =
                        Random.insideUnitCircle.normalized;

                    direction = new Vector3(
                        randomDirection.x,
                        0f,
                        randomDirection.y);
                }

                direction.Normalize();

                float distance = Random.Range(
                    definition.roamDistanceRange.x,
                    definition.roamDistanceRange.y);

                Vector3 candidate =
                    transform.position +
                    direction * distance;

                if (
                    NavMesh.SamplePosition(
                        candidate,
                        out NavMeshHit hit,
                        2.5f,
                        NavMesh.AllAreas)
                )
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                    EnterRunning();
                    return true;
                }
            }

            EnterIdle();
            return false;
        }

        private void EnterIdle(
            bool randomizeAnimationTime = false)
        {
            state = WildlifeAnimalState.Idle;

            if (
                agent != null &&
                agent.enabled &&
                agent.isOnNavMesh
            )
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            stateTimer = Random.Range(
                definition.idleDurationRange.x,
                definition.idleDurationRange.y);

            PlayAnimation(
                definition.idleStateName,
                0.12f,
                randomizeAnimationTime
                    ? Random.value
                    : 0f);
        }

        private void EnterRunning()
        {
            state = WildlifeAnimalState.Running;

            PlayAnimation(
                definition.runningStateName,
                0.12f,
                Random.value);
        }

        private void EnterDead(
            Vector3 hitPoint,
            Vector3 arrowVelocity)
        {
            state = WildlifeAnimalState.Dead;

            if (
                agent != null &&
                agent.enabled
            )
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                agent.enabled = false;
            }

            if (
                animator != null &&
                definition.hasDeathAnimation &&
                !string.IsNullOrWhiteSpace(
                    definition.deathStateName)
            )
            {
                animator.CrossFadeInFixedTime(
                    definition.deathStateName,
                    0.08f,
                    0,
                    0f);
            }
            else
            {
                StartCoroutine(
                    FallbackDeathAnimation());
            }

            owner?.NotifyAnimalDied(this);

            StartCoroutine(
                DisableHitboxesAfterPhysicsStep());

            deathRoutine = StartCoroutine(
                ReturnAfterDeathDelay());
        }

        private IEnumerator DisableHitboxesAfterPhysicsStep()
        {
            yield return new WaitForFixedUpdate();
            EnableHitboxes(false);
        }

        private IEnumerator ReturnAfterDeathDelay()
        {
            float delay =
                definition != null
                    ? definition.corpseLifetime
                    : 10f;

            yield return new WaitForSeconds(delay);

            deathRoutine = null;
            owner?.ReturnToPool(this);
        }

        private IEnumerator FallbackDeathAnimation()
        {
            if (visualRoot == null || definition == null)
            {
                yield break;
            }

            Quaternion startRotation =
                visualRoot.localRotation;

            Vector3 startPosition =
                visualRoot.localPosition;

            Quaternion targetRotation =
                startRotation *
                Quaternion.Euler(
                    definition.fallbackDeathEuler);

            Vector3 targetPosition =
                startPosition +
                definition.fallbackDeathOffset;

            float duration =
                Mathf.Max(
                    0.05f,
                    definition.fallbackDeathDuration);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / duration));

                visualRoot.localRotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        t);

                visualRoot.localPosition =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        t);

                yield return null;
            }
        }

        private void PlayAnimation(
            string stateName,
            float transitionDuration,
            float normalizedTime)
        {
            if (
                animator == null ||
                string.IsNullOrWhiteSpace(stateName)
            )
            {
                return;
            }

            animator.CrossFadeInFixedTime(
                stateName,
                transitionDuration,
                0,
                normalizedTime);
        }

        private void EnableHitboxes(bool stateValue)
        {
            if (hitboxes == null)
            {
                return;
            }

            foreach (WildlifeHitbox hitbox in hitboxes)
            {
                if (hitbox == null)
                {
                    continue;
                }

                Collider hitboxCollider =
                    hitbox.GetComponent<Collider>();

                if (hitboxCollider != null)
                {
                    hitboxCollider.enabled = stateValue;
                }

                hitbox.enabled = stateValue;
            }
        }

        private void CacheVisualTransform()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualStartRotation =
                visualRoot.localRotation;

            visualStartPosition =
                visualRoot.localPosition;
        }

        private void RestoreVisualTransform()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localRotation =
                visualStartRotation;

            visualRoot.localPosition =
                visualStartPosition;
        }
    }
}