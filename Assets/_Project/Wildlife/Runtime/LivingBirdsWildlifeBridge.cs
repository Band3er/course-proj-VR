using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForestArchery.Wildlife
{
    [Serializable]
    public sealed class LivingBirdMaterialSwap
    {
        public string sourceMaterialName;
        public Material replacementMaterial;
    }

    [DefaultExecutionOrder(100)]
    public sealed class LivingBirdsWildlifeBridge : MonoBehaviour
    {
        [SerializeField] private lb_BirdController birdController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private WildlifeSpeciesDefinition scoreDefinition;
        [SerializeField] private LivingBirdMaterialSwap[] materialSwaps;

        [Header("Hit Detection")]
        [SerializeField, Min(1f)]
        private float hitboxScale = 1.35f;

        [SerializeField, Min(0.1f)]
        private float arrowForceMultiplier = 8f;

        [SerializeField, Min(1f)]
        private float minimumArrowForce = 45f;

        [Header("Player Proximity")]
        [SerializeField, Min(0f)]
        private float scareDistance = 3.25f;

        [SerializeField, Min(0.1f)]
        private float updateInterval = 0.4f;

        [SerializeField, Min(0.1f)]
        private float fleeCooldown = 2.5f;

        private readonly List<lb_Bird> birds =
            new List<lb_Bird>();

        private readonly Dictionary<lb_Bird, bool>
            previousActiveState =
                new Dictionary<lb_Bird, bool>();

        private readonly Dictionary<lb_Bird, float>
            nextFleeTime =
                new Dictionary<lb_Bird, float>();

        private readonly Dictionary<string, Material>
            materialLookup =
                new Dictionary<string, Material>(
                    StringComparer.OrdinalIgnoreCase);

        private float nextUpdateTime;
        private bool configured;

        public void Configure(
            lb_BirdController controller,
            Camera configuredCamera,
            WildlifeSpeciesDefinition definition,
            LivingBirdMaterialSwap[] swaps,
            float configuredHitboxScale,
            float configuredScareDistance)
        {
            birdController = controller;
            playerCamera = configuredCamera;
            scoreDefinition = definition;
            materialSwaps = swaps;
            hitboxScale =
                Mathf.Max(1f, configuredHitboxScale);
            scareDistance =
                Mathf.Max(0f, configuredScareDistance);

            BuildMaterialLookup();
        }

        private IEnumerator Start()
        {
            if (birdController == null)
            {
                birdController =
                    GetComponent<lb_BirdController>();
            }

            if (playerCamera == null)
            {
                playerCamera =
                    Camera.main;
            }

            BuildMaterialLookup();

            const int maxFrames = 240;

            for (
                int frame = 0;
                frame < maxFrames;
                frame++
            )
            {
                yield return null;

                if (
                    birdController == null
                )
                {
                    continue;
                }

                lb_Bird[] foundBirds =
                    birdController
                        .GetComponentsInChildren
                            <lb_Bird>(
                                true);

                if (foundBirds.Length == 0)
                {
                    continue;
                }

                ConfigureBirds(
                    foundBirds);

                ApplyMaterialSwaps(
                    birdController.gameObject);

                configured = true;

                Debug.Log(
                    "[WILDLIFE BIRD] Bridge ready" +
                    " | birds=" + birds.Count +
                    " | camera=" +
                    (
                        playerCamera != null
                            ? playerCamera.name
                            : "<null>"
                    ));

                yield break;
            }

            Debug.LogError(
                "[WILDLIFE BIRD] Bridge could not find pooled birds.");
        }

        private void Update()
        {
            if (
                !configured ||
                Time.time < nextUpdateTime
            )
            {
                return;
            }

            nextUpdateTime =
                Time.time +
                updateInterval;

            for (
                int index = 0;
                index < birds.Count;
                index++
            )
            {
                lb_Bird bird =
                    birds[index];

                if (bird == null)
                {
                    continue;
                }

                bool isActive =
                    bird.gameObject.activeSelf;

                bool wasActive =
                    previousActiveState.TryGetValue(
                        bird,
                        out bool previous) &&
                    previous;

                if (
                    isActive &&
                    !wasActive
                )
                {
                    Debug.Log(
                        "[WILDLIFE BIRD] Spawned" +
                        " | bird=" +
                        CleanBirdName(
                            bird.gameObject.name) +
                        " | position=" +
                        bird.transform.position);
                }

                previousActiveState[bird] =
                    isActive;

                if (
                    !isActive ||
                    playerCamera == null ||
                    scareDistance <= 0f
                )
                {
                    continue;
                }

                LivingBirdArrowTarget target =
                    bird.GetComponent
                        <LivingBirdArrowTarget>();

                if (
                    target != null &&
                    target.HitAccepted
                )
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        bird.transform.position,
                        playerCamera.transform.position);

                if (
                    distance > scareDistance
                )
                {
                    continue;
                }

                float allowedTime =
                    nextFleeTime.TryGetValue(
                        bird,
                        out float savedTime)
                        ? savedTime
                        : 0f;

                if (Time.time < allowedTime)
                {
                    continue;
                }

                nextFleeTime[bird] =
                    Time.time +
                    fleeCooldown;

                bird.gameObject.SendMessage(
                    "Flee",
                    SendMessageOptions
                        .DontRequireReceiver);

                Debug.Log(
                    "[WILDLIFE BIRD] Flee" +
                    " | bird=" +
                    CleanBirdName(
                        bird.gameObject.name) +
                    " | playerDistance=" +
                    distance.ToString("F2"));
            }
        }

        private void ConfigureBirds(
            lb_Bird[] foundBirds)
        {
            birds.Clear();
            previousActiveState.Clear();
            nextFleeTime.Clear();

            for (
                int index = 0;
                index < foundBirds.Length;
                index++
            )
            {
                lb_Bird bird =
                    foundBirds[index];

                if (bird == null)
                {
                    continue;
                }

                birds.Add(bird);

                previousActiveState[bird] =
                    bird.gameObject.activeSelf;

                nextFleeTime[bird] = 0f;

                TrySetBirdTag(
                    bird.gameObject);

                LivingBirdArrowTarget target =
                    bird.GetComponent
                        <LivingBirdArrowTarget>();

                if (target == null)
                {
                    target =
                        bird.gameObject
                            .AddComponent
                                <LivingBirdArrowTarget>();
                }

                target.Configure(
                    bird,
                    scoreDefinition,
                    arrowForceMultiplier,
                    minimumArrowForce,
                    hitboxScale);

                Collider[] colliders =
                    bird.GetComponentsInChildren
                        <Collider>(
                            true);

                for (
                    int colliderIndex = 0;
                    colliderIndex <
                    colliders.Length;
                    colliderIndex++
                )
                {
                    Collider collider =
                        colliders[colliderIndex];

                    if (collider == null)
                    {
                        continue;
                    }

                    LivingBirdColliderRelay relay =
                        collider.GetComponent
                            <LivingBirdColliderRelay>();

                    if (relay == null)
                    {
                        relay =
                            collider.gameObject
                                .AddComponent
                                    <LivingBirdColliderRelay>();
                    }

                    relay.Configure(
                        target);
                }

                ApplyMaterialSwaps(
                    bird.gameObject);
            }
        }

        private void BuildMaterialLookup()
        {
            materialLookup.Clear();

            if (materialSwaps == null)
            {
                return;
            }

            for (
                int index = 0;
                index < materialSwaps.Length;
                index++
            )
            {
                LivingBirdMaterialSwap swap =
                    materialSwaps[index];

                if (
                    swap == null ||
                    string.IsNullOrWhiteSpace(
                        swap.sourceMaterialName) ||
                    swap.replacementMaterial == null
                )
                {
                    continue;
                }

                materialLookup[
                    NormalizeMaterialName(
                        swap.sourceMaterialName)
                ] =
                    swap.replacementMaterial;
            }
        }

        private void ApplyMaterialSwaps(
            GameObject root)
        {
            if (
                root == null ||
                materialLookup.Count == 0
            )
            {
                return;
            }

            Renderer[] renderers =
                root.GetComponentsInChildren
                    <Renderer>(
                        true);

            for (
                int rendererIndex = 0;
                rendererIndex <
                renderers.Length;
                rendererIndex++
            )
            {
                Renderer renderer =
                    renderers[rendererIndex];

                Material[] materials =
                    renderer.sharedMaterials;

                bool changed = false;

                for (
                    int materialIndex = 0;
                    materialIndex <
                    materials.Length;
                    materialIndex++
                )
                {
                    Material source =
                        materials[materialIndex];

                    if (source == null)
                    {
                        continue;
                    }

                    string normalizedName =
                        NormalizeMaterialName(
                            source.name);

                    if (
                        !materialLookup.TryGetValue(
                            normalizedName,
                            out Material replacement) ||
                        replacement == null ||
                        replacement == source
                    )
                    {
                        continue;
                    }

                    materials[materialIndex] =
                        replacement;

                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials =
                        materials;
                }
            }
        }

        private static string NormalizeMaterialName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            const string instanceSuffix =
                " (Instance)";

            string result =
                value.Trim();

            if (
                result.EndsWith(
                    instanceSuffix,
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                result =
                    result.Substring(
                        0,
                        result.Length -
                        instanceSuffix.Length);
            }

            return result;
        }

        private static void TrySetBirdTag(
            GameObject birdObject)
        {
            if (birdObject == null)
            {
                return;
            }

            try
            {
                birdObject.tag =
                    "lb_bird";
            }
            catch (UnityException)
            {
                Debug.LogWarning(
                    "[WILDLIFE BIRD] Tag lb_bird is unavailable.");
            }
        }

        private static string CleanBirdName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Bird";
            }

            return value
                .Replace("lb_", string.Empty)
                .Replace("HQ", string.Empty)
                .Replace("(Clone)", string.Empty)
                .Trim();
        }
    }
}
