
// 거리 기반 표시 제어: 멀리 > 아이콘, 가까이 > 툴팁(타이틀)
using TMPro;
using UnityEngine;

public class PinDistanceView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform cam;                 // AR Camera Transform
    [SerializeField] private GameObject iconRoot;           // IconCanvas 또는 IconRoot
    [SerializeField] private GameObject tooltipRoot;        // TooltipCanvas 또는 TooltipRoot
    [SerializeField] private TMP_Text tooltipTitleText;     // TooltipText (TMP)

    [Header("Distance")]
    [SerializeField] private float showTooltipDistance = 1.2f; // 가까우면 툴팁
    [SerializeField] private float hideTooltipDistance = 1.4f; // 멀어지면 아이콘 (히스테리시스)

    [Header("Facing")]
    [SerializeField] private bool billboardToCamera = true;

    private MemoData memo;
    private bool tooltipOn = false;

    private void Awake()
    {
        memo = GetComponent<MemoData>();
        ApplyTitle();
        SetTooltip(false);
    }

    public void SetCamera(Transform cameraTransform)
    {
        cam = cameraTransform;
    }

    public void ApplyTitle()
    {
        if (tooltipTitleText == null) return;

        string t = (memo != null) ? (memo.title ?? "") : "";
        tooltipTitleText.text = string.IsNullOrWhiteSpace(t) ? "(제목 없음)" : t;
    }

    private void Update()
    {
        if (cam == null) return;

        float d = Vector3.Distance(cam.position, transform.position);

        if (!tooltipOn && d <= showTooltipDistance) SetTooltip(true);
        else if (tooltipOn && d >= hideTooltipDistance) SetTooltip(false);

        if (billboardToCamera)
        {
            Vector3 dir = transform.position - cam.position; // 오브젝트가 카메라를 바라보도록
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }

    private void SetTooltip(bool on)
    {
        tooltipOn = on;
        if (iconRoot) iconRoot.SetActive(!on);
        if (tooltipRoot) tooltipRoot.SetActive(on);
    }
}
