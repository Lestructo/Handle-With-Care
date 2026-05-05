using UnityEngine;
using UnityEngine.Rendering;

public class Highlightable : MonoBehaviour
{
    public Material outlineMaterialSource;

    private GameObject outlineObject;
    private Material outlineMaterial;

    void Awake()
    {
        if (outlineMaterialSource == null) return;

        MeshFilter sourceMF = GetComponent<MeshFilter>();
        if (sourceMF == null)
            sourceMF = GetComponentInChildren<MeshFilter>();

        if (sourceMF == null) return;

        outlineMaterial = new Material(outlineMaterialSource);

        // separate child mesh with no collider so it renders the outline without affecting physics
        outlineObject = new GameObject("_Outline");
        outlineObject.transform.SetParent(sourceMF.transform, false);

        outlineObject.AddComponent<MeshFilter>().sharedMesh = sourceMF.sharedMesh;

        MeshRenderer mr = outlineObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = outlineMaterial;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        outlineObject.SetActive(false);
    }

    public void SetHighlight(bool on, Color color = default)
    {
        if (outlineObject == null) return;
        if (on) outlineMaterial.SetColor("_OutlineColor", color);
        outlineObject.SetActive(on);
    }

    public void UpdateOutlineColor(Color color)
    {
        if (outlineMaterial != null)
            outlineMaterial.SetColor("_OutlineColor", color);
    }

    void OnDestroy()
    {
        if (outlineMaterial != null)
            Destroy(outlineMaterial);
    }
}
