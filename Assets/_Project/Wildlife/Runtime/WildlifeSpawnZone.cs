using UnityEngine;
using UnityEngine.AI;

namespace ForestArchery.Wildlife
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WildlifeSpawnZone : MonoBehaviour
    {
        [SerializeField] private BoxCollider zoneCollider;
        [SerializeField] private float navMeshSampleDistance = 8f;

        private void Reset()
        {
            zoneCollider = GetComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
        }

        private void OnValidate()
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<BoxCollider>();
            }

            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }

            navMeshSampleDistance =
                Mathf.Max(0.5f, navMeshSampleDistance);
        }

        public bool TryGetRandomNavMeshPoint(
            out Vector3 result)
        {
            result = transform.position;

            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<BoxCollider>();
            }

            if (zoneCollider == null)
            {
                return false;
            }

            Bounds bounds = zoneCollider.bounds;

            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y,
                Random.Range(bounds.min.z, bounds.max.z));

            if (
                NavMesh.SamplePosition(
                    randomPoint,
                    out NavMeshHit hit,
                    navMeshSampleDistance,
                    NavMesh.AllAreas)
            )
            {
                result = hit.position;
                return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider box =
                zoneCollider != null
                    ? zoneCollider
                    : GetComponent<BoxCollider>();

            if (box == null)
            {
                return;
            }

            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = previous;
        }
    }
}