using System.Collections.Generic;
using UnityEngine;

public class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer[] targetRenderers;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.1f;
    [SerializeField, ColorUsage(true, true)]
    private Color flashColor = new Color(3f, 3f, 3f, 1f);

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private sealed class MaterialSlot
    {
        public Renderer Renderer;
        public int Index;
        public int ColorProperty;
        public MaterialPropertyBlock Original =
            new MaterialPropertyBlock();
        public MaterialPropertyBlock Working =
            new MaterialPropertyBlock();
    }

    private readonly List<MaterialSlot> slots =
        new List<MaterialSlot>();

    private bool isFlashing;
    private float flashEndTime;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers =
                GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        foreach (SkinnedMeshRenderer target in targetRenderers)
        {
            if (target == null)
                continue;

            Material[] materials = target.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null)
                    continue;

                int property;

                if (material.HasProperty(BaseColorId))
                    property = BaseColorId;
                else if (material.HasProperty(ColorId))
                    property = ColorId;
                else
                    continue;

                slots.Add(new MaterialSlot
                {
                    Renderer = target,
                    Index = i,
                    ColorProperty = property
                });
            }
        }

        if (slots.Count == 0)
        {
            Debug.LogWarning(
                $"{name}: 피격 효과를 적용할 Renderer 또는 색상 속성이 없습니다.",
                this);
        }
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
            return;

        foreach (MaterialSlot slot in slots)
        {
            if (slot.Renderer == null)
                continue;

            if (!isFlashing)
            {
                slot.Renderer.GetPropertyBlock(
                    slot.Original, slot.Index);

                slot.Renderer.GetPropertyBlock(
                    slot.Working, slot.Index);

                if (slot.Original.isEmpty)
                    slot.Renderer.GetPropertyBlock(slot.Working);
            }

            slot.Working.SetColor(slot.ColorProperty, flashColor);

            slot.Renderer.SetPropertyBlock(
                slot.Working, slot.Index);
        }

        isFlashing = true;
        flashEndTime = Time.time + flashDuration;
    }

    private void Update()
    {
        if (isFlashing && Time.time >= flashEndTime)
            Restore();
    }

    private void Restore()
    {
        if (!isFlashing)
            return;

        foreach (MaterialSlot slot in slots)
        {
            if (slot.Renderer == null)
                continue;

            slot.Renderer.SetPropertyBlock(
                slot.Original.isEmpty ? null : slot.Original,
                slot.Index);
        }

        isFlashing = false;
    }

    private void OnDisable()
    {
        Restore();
    }
}