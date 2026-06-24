using UnityEngine;

namespace ForestArchery.Wildlife
{
    public enum WildlifeMovementMode
    {
        GroundNavMesh,
        Flying
    }

    [CreateAssetMenu(
        fileName = "WildlifeSpeciesDefinition",
        menuName = "Forest Archery/Wildlife/Species Definition")]
    public sealed class WildlifeSpeciesDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string speciesId = "rabbit";
        public string displayName = "Rabbit";
        public WildlifeMovementMode movementMode =
            WildlifeMovementMode.GroundNavMesh;

        [Header("Prefab and Spawn Balance")]
        public GameObject gameplayPrefab;
        [Min(1)] public int maxAlive = 4;
        [Min(1)] public int poolSize = 9;
        [Min(0.01f)] public float spawnWeight = 1f;

        [Header("Scoring")]
        [Min(0)] public int baseScore = 100;
        [Min(1f)] public float headshotMultiplier = 1.5f;
        [Min(1f)] public float movingScoreMultiplier = 1.25f;
        [Min(1f)] public float airborneScoreMultiplier = 1.40f;

        [Header("Ground Behaviour")]
        public Vector2 idleDurationRange = new Vector2(1.5f, 4f);
        public Vector2 movementSpeedRange = new Vector2(2.4f, 3.3f);
        public Vector2 roamDistanceRange = new Vector2(3f, 7f);
        [Min(0f)] public float angularSpeed = 720f;
        [Min(0f)] public float acceleration = 14f;
        [Min(0f)] public float stoppingDistance = 0.12f;
        [Min(0f)] public float fleeDistance = 2.75f;
        public Vector2 activeLifetimeRange = new Vector2(28f, 48f);

        [Header("Animation States")]
        public string idleStateName = "Idle";
        public string runningStateName = "Run";
        public string deathStateName = "Dead";
        public bool hasDeathAnimation = true;

        [Header("Death")]
        [Min(0f)] public float corpseLifetime = 120f;
        public Vector3 fallbackDeathEuler = new Vector3(0f, 0f, 90f);
        public Vector3 fallbackDeathOffset = Vector3.zero;
        [Min(0.05f)] public float fallbackDeathDuration = 0.55f;

        private void OnValidate()
        {
            maxAlive = Mathf.Max(1, maxAlive);
            poolSize = Mathf.Max(maxAlive, poolSize);
            spawnWeight = Mathf.Max(0.01f, spawnWeight);
            headshotMultiplier =
                Mathf.Max(1f, headshotMultiplier);
            movingScoreMultiplier =
                Mathf.Max(1f, movingScoreMultiplier);
            airborneScoreMultiplier =
                Mathf.Max(1f, airborneScoreMultiplier);
            movementSpeedRange.x = Mathf.Max(0.1f, movementSpeedRange.x);
            movementSpeedRange.y = Mathf.Max(
                movementSpeedRange.x,
                movementSpeedRange.y);
            idleDurationRange.x = Mathf.Max(0.1f, idleDurationRange.x);
            idleDurationRange.y = Mathf.Max(
                idleDurationRange.x,
                idleDurationRange.y);
            roamDistanceRange.x = Mathf.Max(0.5f, roamDistanceRange.x);
            roamDistanceRange.y = Mathf.Max(
                roamDistanceRange.x,
                roamDistanceRange.y);
            activeLifetimeRange.x = Mathf.Max(5f, activeLifetimeRange.x);
            activeLifetimeRange.y = Mathf.Max(
                activeLifetimeRange.x,
                activeLifetimeRange.y);
        }
    }
}