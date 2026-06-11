using UnityEngine;

public class DojoSceneBuilder : MonoBehaviour
{
    [SerializeField] private Spawner spawner;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector3 desktopCameraPosition = new Vector3(0f, 0f, -20f);
    [SerializeField] private Quaternion desktopCameraRotation = Quaternion.identity;
    [SerializeField] private float desktopOrthographicSize = 10f;
    [Header("Quest Background Board")]
    [SerializeField] private string backgroundName = "Background";
    [SerializeField] private Vector3 backgroundWorldPosition = new Vector3(0f, 1.25f, -2.95f);
    [SerializeField] private Vector3 backgroundWorldEulerAngles = new Vector3(-90f, 0f, 0f);
    [SerializeField] private Vector3 backgroundWorldScale = new Vector3(1.05f, 1.05f, 1.05f);
    private bool built;
    private static readonly string[] GeneratedDojoObjectNames =
    {
        "Dojo Stone Floor",
        "Dojo Back Gate",
        "Dojo Left Screen",
        "Dojo Right Screen",
        "Player Rail",
        "Slice Zone Top",
        "Slice Zone Bottom",
        "Slice Zone Left",
        "Slice Zone Right",
        "Soft Dojo Fill Light"
    };

    private void Awake()
    {
        BuildNow();
    }

    public void BuildNow()
    {
        if (built)
            return;

        built = true;

        RemoveGeneratedDojo();
        RestoreLegacyCamera();
        ConfigureStaticBackground();
        PositionSpawner();
    }

    private void RestoreLegacyCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = new Color(0.32156864f, 0.21960786f, 0.14509805f, 0f);

        if (!UnityEngine.XR.XRSettings.enabled)
        {
            targetCamera.orthographic = true;
            targetCamera.orthographicSize = desktopOrthographicSize;
            targetCamera.transform.SetPositionAndRotation(desktopCameraPosition, desktopCameraRotation);
        }
        else
        {
            targetCamera.orthographic = false;
        }
    }

    private void ConfigureStaticBackground()
    {
        GameObject background = GameObject.Find(backgroundName);
        if (background == null)
            return;

        background.SetActive(true);
        background.transform.SetParent(null, true);
        background.transform.SetPositionAndRotation(backgroundWorldPosition, Quaternion.Euler(backgroundWorldEulerAngles));
        background.transform.localScale = backgroundWorldScale;

        MeshCollider meshCollider = background.GetComponent<MeshCollider>();
        if (meshCollider != null) {
            meshCollider.enabled = false;
        }
    }

    private void PositionSpawner()
    {
        if (spawner == null) {
            spawner = FindObjectOfType<Spawner>();
        }

        if (spawner == null)
            return;

        spawner.transform.position = new Vector3(0f, 0.6f, -3.25f);
        spawner.transform.localScale = Vector3.one;

        BoxCollider box = spawner.GetComponent<BoxCollider>();
        if (box != null) {
            box.size = new Vector3(0.95f, 0.08f, 0.25f);
            box.center = Vector3.zero;
        }
    }

    private static void RemoveGeneratedDojo()
    {
        foreach (string objectName in GeneratedDojoObjectNames)
        {
            GameObject generatedObject = GameObject.Find(objectName);
            if (generatedObject != null)
                Destroy(generatedObject);
        }
    }

}
