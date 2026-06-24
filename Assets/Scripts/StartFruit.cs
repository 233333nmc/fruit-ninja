using UnityEngine;

public class StartFruit : MonoBehaviour
{
    [SerializeField] private FruitNinjaMode mode;
    private bool started;

    public void Init(FruitNinjaMode selectedMode)
    {
        mode = selectedMode;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStart(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStart(other);
    }

    private void TryStart(Collider other)
    {
        if (started || !other.CompareTag("Player"))
            return;

        started = true;
        GetComponent<Collider>().enabled = false;
        gameObject.SetActive(false);
        GameManager.Instance.StartGameFromMenu(mode);
    }
}
