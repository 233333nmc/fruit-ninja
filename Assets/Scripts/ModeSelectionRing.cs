using UnityEngine;

public class ModeSelectionRing : MonoBehaviour
{
    [SerializeField] private float radius = 0.62f;
    [SerializeField] private float lineWidth = 0.085f;
    [SerializeField] private float spinSpeed = 28f;
    [SerializeField] private int segmentCount = 96;
    [SerializeField] private int labelCount = 3;
    [SerializeField] private string labelText = "CLASSIC";
    [SerializeField] private Color ringColor = new Color(1f, 0.82f, 0.08f, 0.36f);
    [SerializeField] private Color textColor = new Color(1f, 0.92f, 0.35f, 0.78f);

    private Transform ringRoot;
    private Vector3 followOffset = new Vector3(0f, 0f, -0.02f);

    public void Init(string text, Color color)
    {
        labelText = text;
        ringColor = new Color(color.r, color.g, color.b, 0.36f);
        textColor = new Color(
            Mathf.Lerp(color.r, 1f, 0.35f),
            Mathf.Lerp(color.g, 1f, 0.35f),
            Mathf.Lerp(color.b, 1f, 0.35f),
            0.82f);
    }

    private void Start()
    {
        BuildRing();
    }

    private void Update()
    {
        if (ringRoot != null)
        {
            ringRoot.position = transform.position + followOffset;
            ringRoot.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void OnDestroy()
    {
        if (ringRoot != null)
            Destroy(ringRoot.gameObject);
    }

    private void BuildRing()
    {
        if (ringRoot != null)
            Destroy(ringRoot.gameObject);

        GameObject rootObject = new GameObject("Mode Selection Ring");
        rootObject.transform.position = transform.position + followOffset;
        rootObject.transform.rotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        ringRoot = rootObject.transform;

        CreateLineRing(ringRoot);
        CreateLabels(ringRoot);
    }

    private void CreateLineRing(Transform parent)
    {
        GameObject ringObject = new GameObject("Transparent Mode Ring");
        ringObject.transform.SetParent(parent, false);

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segmentCount;
        line.widthMultiplier = lineWidth;
        line.numCornerVertices = 8;
        line.numCapVertices = 8;
        line.material = CreateTransparentMaterial("Mode Ring Material", ringColor);
        line.startColor = ringColor;
        line.endColor = ringColor;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i / (float)segmentCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    private void CreateLabels(Transform parent)
    {
        for (int i = 0; i < labelCount; i++)
        {
            float angle = i / (float)labelCount * Mathf.PI * 2f;
            Vector3 localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -0.012f);

            GameObject labelObject = new GameObject("Mode Ring Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);

            TextMesh text = labelObject.AddComponent<TextMesh>();
            text.text = labelText;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 56;
            text.characterSize = 0.028f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = textColor;
        }
    }

    private static Material CreateTransparentMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        return material;
    }
}
