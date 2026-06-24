using UnityEngine;

public class Bomb : MonoBehaviour
{
    private bool hasBeenHit;

    private void Awake()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true))
            Destroy(trail);

        DownloadedArtLibrary.ApplyBombVisuals(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IBladeSliceSource blade = null;
            MonoBehaviour[] behaviours = other.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                blade = behaviour as IBladeSliceSource;
                if (blade != null)
                    break;
            }

            Vector3 direction = blade != null && blade.Direction.sqrMagnitude > 0.0001f ? blade.Direction.normalized : Vector3.up;
            Vector3 hitPosition = blade != null ? blade.Position : transform.position;
            Hit(direction, hitPosition);
        }
    }

    public bool TrySlice(IBladeSliceSource blade)
    {
        if (hasBeenHit || blade == null)
            return false;

        Vector3 direction = blade.Direction.sqrMagnitude > 0.0001f ? blade.Direction.normalized : Vector3.up;
        Hit(direction, blade.Position);
        return true;
    }

    private void Hit(Vector3 direction, Vector3 position)
    {
        if (hasBeenHit)
            return;

        hasBeenHit = true;

        foreach (Collider bombCollider in GetComponentsInChildren<Collider>())
            bombCollider.enabled = false;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;

        DownloadedArtLibrary.PlayBombHitVfx(position, direction);
        GameManager.Instance.HandleBombHit(position);
        Destroy(gameObject, 0.25f);
    }

}
