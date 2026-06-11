using UnityEngine;

public class BladeVisuals : MonoBehaviour
{
    [SerializeField] private Transform rightBlade;
    [SerializeField] private Transform leftBlade;
    [SerializeField] private Vector3 rightViewportIdle = new Vector3(0.62f, 0.08f, 1.05f);
    [SerializeField] private Vector3 leftViewportIdle = new Vector3(0.38f, 0.08f, 1.05f);
    [SerializeField] private Vector3 bladeRootOffset = new Vector3(0f, -0.12f, 0f);

    private Material rightMaterial;
    private Material leftMaterial;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        rightMaterial = CreateMaterial("Right Saber Material", new Color(0.25f, 0.9f, 1f));
        leftMaterial = CreateMaterial("Left Saber Material", new Color(1f, 0.55f, 0.12f));

        rightBlade = CreateBlade("Right VR Saber", rightMaterial, ViewportToWorld(rightViewportIdle));
        leftBlade = CreateBlade("Left VR Saber", leftMaterial, ViewportToWorld(leftViewportIdle));
    }

    public void SetRightBladePose(Vector3 bladeTip, bool active)
    {
        SetRightBladePose(bladeTip, Quaternion.identity, active);
    }

    public void SetRightBladePose(Vector3 bladeTip, Quaternion rotation, bool active)
    {
        SetRightBladePose(bladeTip, rotation, active, true);
    }

    public void SetBladePoses(Vector3 rightBladeTip, bool rightActive, Vector3 leftBladeTip, bool leftActive)
    {
        SetBladePoses(rightBladeTip, Quaternion.identity, rightActive, leftBladeTip, Quaternion.identity, leftActive);
    }

    public void SetBladePoses(Vector3 rightBladeTip, Quaternion rightRotation, bool rightActive, Vector3 leftBladeTip, Quaternion leftRotation, bool leftActive)
    {
        SetRightBladePose(rightBladeTip, rightRotation, rightActive, false);
        SetLeftBladePose(leftBladeTip, leftRotation, leftActive);
    }

    private void SetRightBladePose(Vector3 bladeTip, Quaternion rotation, bool active, bool mirrorLeft)
    {
        if (rightBlade == null)
            return;

        if (active) {
            rightBlade.position = bladeTip + rotation * bladeRootOffset;
            rightBlade.rotation = rotation == Quaternion.identity ? rightBlade.rotation : rotation;
        } else {
            Vector3 idle = ViewportToWorld(rightViewportIdle);
            rightBlade.position = Vector3.Lerp(rightBlade.position, idle, 0.08f);
        }

        if (mirrorLeft) {
            Vector3 mirroredTip = new Vector3(-bladeTip.x, bladeTip.y * 0.65f - 0.5f, bladeTip.z - 0.25f);
            SetLeftBladePose(mirroredTip, rotation, active);
        }
    }

    private void SetLeftBladePose(Vector3 bladeTip, bool active)
    {
        SetLeftBladePose(bladeTip, Quaternion.identity, active);
    }

    private void SetLeftBladePose(Vector3 bladeTip, Quaternion rotation, bool active)
    {
        if (leftBlade == null)
            return;

        if (active) {
            leftBlade.position = bladeTip + rotation * bladeRootOffset;
            leftBlade.rotation = rotation == Quaternion.identity ? leftBlade.rotation : rotation;
        } else {
            Vector3 idle = ViewportToWorld(leftViewportIdle);
            leftBlade.position = Vector3.Lerp(leftBlade.position, idle, 0.08f);
        }
    }

    private Transform CreateBlade(string name, Material material, Vector3 idlePosition)
    {
        GameObject root = new GameObject(name);
        root.transform.position = idlePosition;

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Saber Shaft";
        shaft.transform.SetParent(root.transform, false);
        shaft.transform.localScale = new Vector3(0.055f, 0.26f, 0.055f);
        shaft.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        shaft.GetComponent<Renderer>().material = material;
        Destroy(shaft.GetComponent<Collider>());

        GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "Saber Guard";
        guard.transform.SetParent(root.transform, false);
        guard.transform.localPosition = new Vector3(0f, -0.12f, 0f);
        guard.transform.localScale = new Vector3(0.14f, 0.025f, 0.045f);
        guard.GetComponent<Renderer>().material = material;
        Destroy(guard.GetComponent<Collider>());

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Saber Handle";
        handle.transform.SetParent(root.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.24f, 0f);
        handle.transform.localScale = new Vector3(0.04f, 0.11f, 0.04f);
        handle.GetComponent<Renderer>().material = material;
        Destroy(handle.GetComponent<Collider>());

        return root.transform;
    }

    private Vector3 ViewportToWorld(Vector3 viewportPosition)
    {
        if (mainCamera == null) {
            mainCamera = Camera.main;
        }

        return mainCamera != null ? mainCamera.ViewportToWorldPoint(viewportPosition) : Vector3.zero;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 1.6f);
        return material;
    }
}
