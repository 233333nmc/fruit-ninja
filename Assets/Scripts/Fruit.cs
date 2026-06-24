using UnityEngine;

public class Fruit : MonoBehaviour
{
    public GameObject whole;
    public GameObject sliced;

    private Rigidbody fruitRigidbody;
    private Collider fruitCollider;
    private bool hasBeenSliced;
    private float defaultDrag;
    private bool slowedNearApex;

    public int points = 1;
    [SerializeField] private float slowMotionEnterY = 1.85f;
    [SerializeField] private float slowMotionExitY = 1.1f;
    [SerializeField] private float apexDrag = 5.5f;
    [SerializeField] private float apexMaxUpwardSpeed = 0.2f;
    [SerializeField] private float apexMaxDownwardSpeed = 0.18f;

    private void Awake()
    {
        fruitRigidbody = GetComponent<Rigidbody>();
        fruitCollider = GetComponent<Collider>();
        if (fruitRigidbody != null)
            defaultDrag = fruitRigidbody.drag;

        DownloadedArtLibrary.ApplyFruitVisuals(gameObject, whole, sliced);
        DisableLegacyJuiceEffects();
    }

    private void FixedUpdate()
    {
        UpdateApexSlowMotion();
    }

    private void Slice(Vector3 direction, Vector3 position, Quaternion bladeRotation, float force)
    {
        if (hasBeenSliced)
            return;

        hasBeenSliced = true;
        RestoreNormalDrag();
        GameManager.Instance.IncreaseScore(points);

        // Disable the whole fruit
        fruitCollider.enabled = false;
        whole.SetActive(false);

        // Enable the sliced fruit
        sliced.SetActive(true);

        Vector3 sliceDirection = direction.sqrMagnitude > 0.0001f ? direction : bladeRotation * Vector3.up;
        sliced.transform.rotation = Quaternion.LookRotation(bladeRotation * Vector3.forward, sliceDirection.normalized);
        DownloadedArtLibrary.PlayFruitSliceVfx(position, sliceDirection, gameObject);

        Rigidbody[] slices = sliced.GetComponentsInChildren<Rigidbody>();
        Vector3 separationAxis = bladeRotation * Vector3.right;
        if (separationAxis.sqrMagnitude < 0.0001f)
            separationAxis = Vector3.right;
        separationAxis.Normalize();

        float separationForce = Mathf.Max(force * 0.58f, 1.35f);
        float spinForce = Mathf.Max(force * 0.12f, 0.34f);
        float separationOffset = Mathf.Max(transform.lossyScale.x * 0.24f, 0.07f);

        // Add a force to each slice based on the blade direction
        for (int i = 0; i < slices.Length; i++)
        {
            Rigidbody slice = slices[i];
            float side = i == 0 ? -1f : 1f;
            slice.WakeUp();
            slice.transform.position += separationAxis * side * separationOffset;
            slice.velocity = fruitRigidbody.velocity + separationAxis * side * separationForce + sliceDirection.normalized * Mathf.Max(force * 0.14f, 0.28f);
            slice.angularVelocity = (separationAxis * side + Random.insideUnitSphere * 0.35f) * spinForce;
            slice.AddForceAtPosition(sliceDirection * force * 0.22f, position, ForceMode.Impulse);
            slice.AddForce(separationAxis * side * separationForce * 0.72f, ForceMode.Impulse);
            slice.AddTorque((separationAxis * side + Random.insideUnitSphere * 0.5f) * spinForce, ForceMode.Impulse);
        }

        Destroy(gameObject, 3f);
    }

    private void UpdateApexSlowMotion()
    {
        if (hasBeenSliced || fruitRigidbody == null)
            return;

        if (!slowedNearApex && transform.position.y >= slowMotionEnterY)
        {
            slowedNearApex = true;
            fruitRigidbody.drag = apexDrag;
        }

        if (slowedNearApex)
        {
            Vector3 velocity = fruitRigidbody.velocity;
            if (velocity.y > apexMaxUpwardSpeed)
                velocity.y = apexMaxUpwardSpeed;
            else if (velocity.y < -apexMaxDownwardSpeed)
                velocity.y = -apexMaxDownwardSpeed;
            fruitRigidbody.velocity = velocity;
        }

        if (slowedNearApex && transform.position.y <= slowMotionExitY && fruitRigidbody.velocity.y < 0f)
        {
            RestoreNormalDrag();
        }
    }

    private void RestoreNormalDrag()
    {
        if (fruitRigidbody == null)
            return;

        slowedNearApex = false;
        fruitRigidbody.drag = defaultDrag;
    }

    private void DisableLegacyJuiceEffects()
    {
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = false;
            particle.gameObject.SetActive(false);
        }
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
        TrySlice(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySlice(other);
    }

    private void TrySlice(Collider other)
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
