using UnityEngine;

public class Fruit : MonoBehaviour
{
    public GameObject whole;
    public GameObject sliced;

    private Rigidbody fruitRigidbody;
    private Collider fruitCollider;
    private ParticleSystem juiceEffect;
    private bool hasBeenSliced;

    public int points = 1;

    private void Awake()
    {
        fruitRigidbody = GetComponent<Rigidbody>();
        fruitCollider = GetComponent<Collider>();
        juiceEffect = GetComponentInChildren<ParticleSystem>();
    }

    private void Slice(Vector3 direction, Vector3 position, Quaternion bladeRotation, float force)
    {
        if (hasBeenSliced)
            return;

        hasBeenSliced = true;
        GameManager.Instance.IncreaseScore(points);

        // Disable the whole fruit
        fruitCollider.enabled = false;
        whole.SetActive(false);

        // Enable the sliced fruit
        sliced.SetActive(true);
        juiceEffect.Play();

        Vector3 sliceDirection = direction.sqrMagnitude > 0.0001f ? direction : bladeRotation * Vector3.up;
        sliced.transform.rotation = Quaternion.LookRotation(bladeRotation * Vector3.forward, sliceDirection.normalized);

        Rigidbody[] slices = sliced.GetComponentsInChildren<Rigidbody>();

        // Add a force to each slice based on the blade direction
        foreach (Rigidbody slice in slices)
        {
            slice.velocity = fruitRigidbody.velocity;
            slice.AddForceAtPosition(sliceDirection * force, position, ForceMode.Impulse);
        }

        Destroy(gameObject, 3f);
    }

    public void BeginLifetime(float lifetime)
    {
        StartCoroutine(LifetimeRoutine(lifetime));
    }

    private System.Collections.IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (!hasBeenSliced && GameManager.Instance != null) {
            GameManager.Instance.ReportMissedFruit();
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
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

            if (blade != null) {
                Slice(blade.Direction, blade.Position, blade.Rotation, blade.SliceForce);
            }
        }
    }

}
