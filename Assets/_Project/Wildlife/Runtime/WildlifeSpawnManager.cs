using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestArchery.Wildlife
{
    public sealed class WildlifeSpawnManager : MonoBehaviour
    {
        [Header("Species")]
        [SerializeField]
        private List<WildlifeSpeciesDefinition> speciesDefinitions =
            new List<WildlifeSpeciesDefinition>();

        [Header("Spawn Zones")]
        [SerializeField]
        private List<WildlifeSpawnZone> spawnZones =
            new List<WildlifeSpawnZone>();

        [Header("Player")]
        [SerializeField] private Transform player;
        [SerializeField] private Camera playerCamera;

        [Header("Spawn Timing")]
        [SerializeField, Min(0)]
        private int initialSpawnCount = 2;

        [SerializeField]
        private Vector2 spawnIntervalRange =
            new Vector2(4f, 7f);

        [SerializeField, Min(1)]
        private int maxSpawnAttempts = 30;

        [Header("Spacing")]
        [SerializeField, Min(0f)]
        private float minimumPlayerDistance = 4f;

        [SerializeField, Min(0f)]
        private float minimumAnimalDistance = 2.25f;

        [SerializeField]
        private bool avoidSpawningInsideView = true;

        [Header("Corpses")]
        [SerializeField, Min(0)]
        private int maximumVisibleCorpses = 5;

        [Header("Runtime")]
        [SerializeField]
        private Transform poolRoot;

        private readonly Dictionary<
            WildlifeSpeciesDefinition,
            Queue<WildlifeAnimal>> pools =
                new Dictionary<
                    WildlifeSpeciesDefinition,
                    Queue<WildlifeAnimal>>();

        private readonly List<WildlifeAnimal> activeAnimals =
            new List<WildlifeAnimal>();

        private readonly Queue<WildlifeAnimal> corpseQueue =
            new Queue<WildlifeAnimal>();

        private Coroutine spawnRoutine;

        public IReadOnlyList<WildlifeAnimal> ActiveAnimals =>
            activeAnimals;

        public void Configure(
            IEnumerable<WildlifeSpeciesDefinition> definitions,
            IEnumerable<WildlifeSpawnZone> zones,
            Transform playerTransform,
            Camera camera)
        {
            speciesDefinitions =
                definitions != null
                    ? definitions
                        .Where(item => item != null)
                        .Distinct()
                        .ToList()
                    : new List<WildlifeSpeciesDefinition>();

            spawnZones =
                zones != null
                    ? zones
                        .Where(item => item != null)
                        .Distinct()
                        .ToList()
                    : new List<WildlifeSpawnZone>();

            player = playerTransform;
            playerCamera = camera;
        }

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (player == null && playerCamera != null)
            {
                player = playerCamera.transform;
            }

            if (spawnZones == null || spawnZones.Count == 0)
            {
                spawnZones = FindObjectsByType<WildlifeSpawnZone>(
                        FindObjectsSortMode.None)
                    .ToList();
            }

            EnsurePoolRoot();
            BuildPools();
        }

        private void Start()
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }

        private void OnDestroy()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }
        }

        public void NotifyAnimalDied(WildlifeAnimal animal)
        {
            if (animal == null)
            {
                return;
            }

            corpseQueue.Enqueue(animal);
            EnforceCorpseLimit();
        }

        public void ReturnToPool(WildlifeAnimal animal)
        {
            if (animal == null)
            {
                return;
            }

            WildlifeSpeciesDefinition definition =
                animal.Definition;

            activeAnimals.Remove(animal);
            animal.PrepareForPool();

            if (poolRoot != null)
            {
                animal.transform.SetParent(poolRoot, false);
            }

            animal.gameObject.SetActive(false);

            if (definition == null)
            {
                Destroy(animal.gameObject);
                return;
            }

            if (!pools.TryGetValue(definition, out Queue<WildlifeAnimal> pool))
            {
                pool = new Queue<WildlifeAnimal>();
                pools.Add(definition, pool);
            }

            if (!pool.Contains(animal))
            {
                pool.Enqueue(animal);
            }
        }

        private IEnumerator SpawnLoop()
        {
            yield return null;

            for (int index = 0; index < initialSpawnCount; index++)
            {
                SpawnOne();
                yield return new WaitForSeconds(0.35f);
            }

            while (true)
            {
                float wait = Random.Range(
                    Mathf.Max(0.25f, spawnIntervalRange.x),
                    Mathf.Max(
                        spawnIntervalRange.x,
                        spawnIntervalRange.y));

                yield return new WaitForSeconds(wait);

                SpawnOne();
            }
        }

        private void BuildPools()
        {
            pools.Clear();

            foreach (
                WildlifeSpeciesDefinition definition in
                speciesDefinitions
            )
            {
                if (
                    definition == null ||
                    definition.gameplayPrefab == null
                )
                {
                    continue;
                }

                Queue<WildlifeAnimal> pool =
                    new Queue<WildlifeAnimal>();

                pools.Add(definition, pool);

                for (
                    int index = 0;
                    index < definition.poolSize;
                    index++
                )
                {
                    WildlifeAnimal animal =
                        CreatePooledAnimal(definition);

                    if (animal != null)
                    {
                        pool.Enqueue(animal);
                    }
                }
            }
        }

        private WildlifeAnimal CreatePooledAnimal(
            WildlifeSpeciesDefinition definition)
        {
            GameObject instance = Instantiate(
                definition.gameplayPrefab,
                poolRoot);

            instance.name =
                definition.displayName +
                "_Pooled";

            WildlifeAnimal animal =
                instance.GetComponent<WildlifeAnimal>();

            if (animal == null)
            {
                Debug.LogError(
                    "[WILDLIFE] Gameplay prefab lacks WildlifeAnimal: " +
                    definition.gameplayPrefab.name);

                Destroy(instance);
                return null;
            }

            animal.PrepareForPool();
            instance.SetActive(false);

            return animal;
        }

        private bool SpawnOne()
        {
            WildlifeSpeciesDefinition definition =
                ChooseDefinition();

            if (definition == null)
            {
                return false;
            }

            if (!TryFindSpawnPosition(out Vector3 position))
            {
                Debug.LogWarning(
                    "[WILDLIFE] No valid spawn position found.");
                return false;
            }

            WildlifeAnimal animal =
                Acquire(definition);

            if (animal == null)
            {
                return false;
            }

            Vector3 lookDirection =
                player != null
                    ? player.position - position
                    : Vector3.forward;

            lookDirection.y = 0f;

            Quaternion rotation =
                lookDirection.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(-lookDirection.normalized)
                    : Quaternion.Euler(
                        0f,
                        Random.Range(0f, 360f),
                        0f);

            animal.gameObject.SetActive(true);
            activeAnimals.Add(animal);

            animal.Activate(
                this,
                player,
                position,
                rotation);

            return true;
        }

        private WildlifeSpeciesDefinition ChooseDefinition()
        {
            List<WildlifeSpeciesDefinition> candidates =
                speciesDefinitions
                    .Where(
                        definition =>
                            definition != null &&
                            definition.gameplayPrefab != null &&
                            CountAlive(definition) <
                                definition.maxAlive)
                    .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;

            foreach (
                WildlifeSpeciesDefinition candidate in
                candidates
            )
            {
                int alive = CountAlive(candidate);

                totalWeight +=
                    candidate.spawnWeight /
                    (1f + alive);
            }

            float randomValue =
                Random.value * totalWeight;

            foreach (
                WildlifeSpeciesDefinition candidate in
                candidates
            )
            {
                int alive = CountAlive(candidate);

                randomValue -=
                    candidate.spawnWeight /
                    (1f + alive);

                if (randomValue <= 0f)
                {
                    return candidate;
                }
            }

            return candidates[candidates.Count - 1];
        }

        private int CountAlive(
            WildlifeSpeciesDefinition definition)
        {
            int count = 0;

            foreach (WildlifeAnimal animal in activeAnimals)
            {
                if (
                    animal != null &&
                    animal.IsAlive &&
                    animal.Definition == definition
                )
                {
                    count++;
                }
            }

            return count;
        }

        private WildlifeAnimal Acquire(
            WildlifeSpeciesDefinition definition)
        {
            if (
                pools.TryGetValue(
                    definition,
                    out Queue<WildlifeAnimal> pool)
            )
            {
                while (pool.Count > 0)
                {
                    WildlifeAnimal animal =
                        pool.Dequeue();

                    if (animal != null)
                    {
                        return animal;
                    }
                }
            }

            return CreatePooledAnimal(definition);
        }

        private bool TryFindSpawnPosition(
            out Vector3 position)
        {
            position = Vector3.zero;

            if (spawnZones == null || spawnZones.Count == 0)
            {
                return false;
            }

            for (
                int attempt = 0;
                attempt < maxSpawnAttempts;
                attempt++
            )
            {
                WildlifeSpawnZone zone =
                    spawnZones[
                        Random.Range(
                            0,
                            spawnZones.Count)];

                if (
                    zone == null ||
                    !zone.TryGetRandomNavMeshPoint(
                        out Vector3 candidate)
                )
                {
                    continue;
                }

                if (
                    player != null &&
                    Vector3.Distance(
                        player.position,
                        candidate) <
                    minimumPlayerDistance
                )
                {
                    continue;
                }

                bool tooCloseToAnimal = false;

                foreach (
                    WildlifeAnimal animal in
                    activeAnimals
                )
                {
                    if (
                        animal != null &&
                        animal.gameObject.activeInHierarchy &&
                        Vector3.Distance(
                            animal.transform.position,
                            candidate) <
                        minimumAnimalDistance
                    )
                    {
                        tooCloseToAnimal = true;
                        break;
                    }
                }

                if (tooCloseToAnimal)
                {
                    continue;
                }

                if (
                    avoidSpawningInsideView &&
                    playerCamera != null &&
                    IsInsideCameraView(candidate)
                )
                {
                    continue;
                }

                position = candidate;
                return true;
            }

            return false;
        }

        private bool IsInsideCameraView(
            Vector3 worldPosition)
        {
            Vector3 viewport =
                playerCamera.WorldToViewportPoint(
                    worldPosition);

            return
                viewport.z > 0f &&
                viewport.x > 0.05f &&
                viewport.x < 0.95f &&
                viewport.y > 0.05f &&
                viewport.y < 0.95f;
        }

        private void EnforceCorpseLimit()
        {
            while (
                corpseQueue.Count >
                maximumVisibleCorpses
            )
            {
                WildlifeAnimal oldest =
                    corpseQueue.Dequeue();

                if (
                    oldest != null &&
                    oldest.gameObject.activeInHierarchy &&
                    oldest.State ==
                        WildlifeAnimalState.Dead
                )
                {
                    ReturnToPool(oldest);
                }
            }
        }

        private void EnsurePoolRoot()
        {
            if (poolRoot != null)
            {
                return;
            }

            Transform existing =
                transform.Find("WildlifePool");

            if (existing != null)
            {
                poolRoot = existing;
                return;
            }

            GameObject root =
                new GameObject("WildlifePool");

            root.transform.SetParent(transform, false);
            poolRoot = root.transform;
        }
    }
}