using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BossRangeIndicator : MonoBehaviour
{
    [SerializeField] private Material indicatorMaterial;
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.05f, 1f);
    [SerializeField] private Color explosionColor = new Color(1f, 0.05f, 0.02f, 1f);
    [SerializeField, Range(0f, 1f)] private float fillOpacity = 0.45f;
    [SerializeField, Range(16, 128)] private int segments = 64;
    [SerializeField, Min(0.01f)] private float lineWidth = 0.06f;
    [SerializeField] private float groundOffset = 0.04f;
    [SerializeField, Min(0)] private int animatorLayer = 0;

    private readonly List<AnimatorClipInfo> clipInfos = new List<AnimatorClipInfo>(2);
    private GameObject visualRoot;
    private Transform fillTransform;
    private MeshRenderer fillRenderer;
    private Mesh fillMesh;
    private Color[] vertexColors;
    private LineRenderer ring;
    private Animator animator;
    private readonly Dictionary<AnimationClip, float> hitTimes = new Dictionary<AnimationClip, float>();
    private bool progressDetected;
    private float attackRadius;
    private bool warningActive;

    public bool IsReady => indicatorMaterial != null && isActiveAndEnabled;

    private void Awake()
    {
        visualRoot = new GameObject(name + "_WarningVisual");
        visualRoot.layer = gameObject.layer;
        visualRoot.SetActive(false);

        GameObject fillObject = new GameObject("Fill");
        fillObject.layer = gameObject.layer;
        fillTransform = fillObject.transform;
        fillTransform.SetParent(visualRoot.transform, false);
        MeshFilter filter = fillObject.AddComponent<MeshFilter>();
        fillRenderer = fillObject.AddComponent<MeshRenderer>();
        fillRenderer.sharedMaterial = indicatorMaterial;
        fillRenderer.shadowCastingMode = ShadowCastingMode.Off;
        fillRenderer.receiveShadows = false;
        fillRenderer.lightProbeUsage = LightProbeUsage.Off;
        fillRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        int count = Mathf.Clamp(segments, 16, 128);
        Vector3[] vertices = new Vector3[count + 1];
        Vector2[] uv = new Vector2[count + 1];
        int[] triangles = new int[count * 3];
        vertexColors = new Color[count + 1];
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices[i + 1] = new Vector3(x, 0f, z);
            uv[i + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = (i + 1) % count + 1;
            triangles[i * 3 + 2] = i + 1;
        }

        fillMesh = new Mesh { name = "BossWarningDisk" };
        fillMesh.vertices = vertices;
        fillMesh.uv = uv;
        fillMesh.triangles = triangles;
        fillMesh.RecalculateNormals();
        fillMesh.RecalculateBounds();
        filter.sharedMesh = fillMesh;

        GameObject ringObject = new GameObject("Boundary");
        ringObject.layer = gameObject.layer;
        ringObject.transform.SetParent(visualRoot.transform, false);
        ringObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.alignment = LineAlignment.TransformZ;
        ring.loop = true;
        ring.positionCount = count;
        ring.sharedMaterial = indicatorMaterial;
        ring.shadowCastingMode = ShadowCastingMode.Off;
        ring.receiveShadows = false;
    }

    public void ShowWarning(Vector3 origin, float radius)
    {
        if (!IsReady || visualRoot == null)
            return;

        EnemyController enemy = GetComponentInParent<EnemyController>();
        animator = enemy != null ? enemy.Animator : null;
        hitTimes.Clear();
        progressDetected = false;
        warningActive = true;
        attackRadius = Mathf.Max(0f, radius);

        Vector3 center = origin + Vector3.up * groundOffset;
        visualRoot.transform.SetPositionAndRotation(center, Quaternion.identity);
        visualRoot.transform.localScale = Vector3.one;
        fillRenderer.sharedMaterial = indicatorMaterial;
        ring.sharedMaterial = indicatorMaterial;
        ring.widthMultiplier = lineWidth;

        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ring.positionCount;
            ring.SetPosition(i, center + new Vector3(
                Mathf.Cos(angle) * attackRadius,
                0.005f,
                Mathf.Sin(angle) * attackRadius));
        }

        SetColor(warningColor);
        SetProgress(0f);
        visualRoot.SetActive(true);
    }

    private void LateUpdate()
    {
        if (!warningActive || animator == null ||
            animatorLayer < 0 || animatorLayer >= animator.layerCount)
            return;

        float progress;
        if (animator.IsInTransition(animatorLayer) &&
            TryGetProgress(true, out progress))
        {
            progressDetected = true;
            SetProgress(progress);
            return;
        }

        if (TryGetProgress(false, out progress))
        {
            progressDetected = true;
            SetProgress(progress);
        }
    }

    private bool TryGetProgress(bool nextState, out float progress)
    {
        progress = 0f;
        clipInfos.Clear();

        if (nextState)
            animator.GetNextAnimatorClipInfo(animatorLayer, clipInfos);
        else
            animator.GetCurrentAnimatorClipInfo(animatorLayer, clipInfos);

        float selectedHitTime = -1f;
        float selectedWeight = -1f;

        foreach (AnimatorClipInfo info in clipInfos)
        {
            if (info.clip == null)
                continue;

            float hitTime = GetHitTime(info.clip);
            if (hitTime <= 0f || info.weight <= selectedWeight)
                continue;

            selectedHitTime = hitTime;
            selectedWeight = info.weight;
        }

        if (selectedHitTime <= 0f)
            return false;

        AnimatorStateInfo state = nextState
            ? animator.GetNextAnimatorStateInfo(animatorLayer)
            : animator.GetCurrentAnimatorStateInfo(animatorLayer);

        progress = Mathf.Clamp01(state.normalizedTime / selectedHitTime);
        return true;
    }

    private float GetHitTime(AnimationClip clip)
    {
        if (hitTimes.TryGetValue(clip, out float cached))
            return cached;

        float hitTime = -1f;
        foreach (AnimationEvent animationEvent in clip.events)
        {
            if (animationEvent.functionName != "OnSpecialAttackHit")
                continue;

            float candidate = animationEvent.time / Mathf.Max(clip.length, 0.0001f);
            if (hitTime < 0f || candidate < hitTime)
                hitTime = candidate;
        }

        hitTimes.Add(clip, hitTime);
        return hitTime;
    }

    private void SetProgress(float progress)
    {
        float radius = attackRadius * Mathf.Clamp01(progress);
        fillTransform.localScale = new Vector3(radius, 1f, radius);
        fillRenderer.enabled = radius > 0.0001f;
    }

    private void SetColor(Color color)
    {
        ring.startColor = color;
        ring.endColor = color;
        color.a *= fillOpacity;
        for (int i = 0; i < vertexColors.Length; i++)
            vertexColors[i] = color;
        fillMesh.colors = vertexColors;
    }

    public void ShowExplosion()
    {
        if (visualRoot == null)
            return;

        if (warningActive && !progressDetected)
            Debug.LogWarning($"{name}: 명중 전에 경고 진행률을 읽지 못했습니다. Animator Layer와 OnSpecialAttackHit 이벤트 위치(0초보다 뒤)를 확인하세요.");

        warningActive = false;
        SetProgress(1f);
        SetColor(explosionColor);
        ring.widthMultiplier = lineWidth * 1.5f;
    }

    public void Hide()
    {
        warningActive = false;
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (visualRoot != null)
            Destroy(visualRoot);
        if (fillMesh != null)
            Destroy(fillMesh);
    }
}
