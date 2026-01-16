
// 탭 생성 시 아이콘만 > 저장 후 툴팁, 거리 따라 아이콘/툴팁 전환
using TMPro;
using UnityEngine;

public class MemoPinView : MonoBehaviour
{
    public enum ViewMode { Icon, Tooltip }

    [Header("Roots")]
    [SerializeField] private GameObject iconRoot;      // 아이콘만 묶인 루트 (IconCanvas 또는 IconRoot)
    [SerializeField] private GameObject tooltipRoot;   // 툴팁만 묶인 루트 (TooltipCanvas 또는 TooltipRoot)

    [Header("Tooltip UI")]
    [SerializeField] private TMP_Text tooltipTitleText; // TooltipCanvas > TooltipRoot > TooltipText (TMP)

    [Header("Distance Rule")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private float showTooltipDistanceMeters = 1.2f; // 가까우면 툴팁
    [SerializeField] private float hideTooltipDistanceMeters = 1.4f; // 멀어지면 아이콘(히스테리시스)
    [SerializeField] private bool distanceBasedAutoSwitch = true;

    [Header("Billboard (optional)")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private Transform billboardTarget; // 보통 tooltipRoot(또는 TooltipCanvas)의 Transform

    private MemoData data;
    private ViewMode mode = ViewMode.Icon;

    // “저장 완료 전에는 아이콘만 보여야 함”을 보장하는 플래그
    private bool isSaved = false;

    public MemoData Data => data;
    public bool IsTooltip => mode == ViewMode.Tooltip;
    public bool IsSaved => isSaved;

    private void Awake()
    {
        // MemoData 자동 확보
        data = GetComponent<MemoData>();

        if (!arCamera) arCamera = Camera.main;
        if (!billboardTarget && tooltipRoot) billboardTarget = tooltipRoot.transform;

        // 시작 시 둘 다 켜져있어도 무조건 아이콘으로 통일(동시 표시 방지)
        SetMode(ViewMode.Icon, force: true);
        ApplySavedState(false); // 기본: 저장 전(아이콘만)
        RefreshTexts();
    }

    private void LateUpdate()
    {
        // 저장 전에는 어떤 조건이든 아이콘만
        if (!isSaved) return;

        if (distanceBasedAutoSwitch && arCamera)
        {
            float dist = Vector3.Distance(arCamera.transform.position, transform.position);

            // 히스테리시스로 깜빡임 방지
            if (mode == ViewMode.Icon && dist <= showTooltipDistanceMeters)
                SetMode(ViewMode.Tooltip);
            else if (mode == ViewMode.Tooltip && dist >= hideTooltipDistanceMeters)
                SetMode(ViewMode.Icon);
        }

        if (faceCamera && billboardTarget && arCamera && IsTooltip)
        {
            Vector3 dir = billboardTarget.position - arCamera.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                billboardTarget.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }

    public void SetCamera(Camera cam)
    {
        arCamera = cam;
    }

    public void Bind(MemoData memoData)
    {
        data = memoData != null ? memoData : GetComponent<MemoData>();
        RefreshTexts();
    }

    /// <summary>
    /// 저장 완료/미완료 상태를 외부에서 확정하는 함수.
    /// - false: 생성 직후 상태(아이콘만)
    /// - true : 저장 완료(거리 기반 아이콘/툴팁 전환)
    /// </summary>
    public void SetSaved(bool saved)
    {
        ApplySavedState(saved);
    }

    private void ApplySavedState(bool saved)
    {
        isSaved = saved;

        if (!isSaved)
        {
            // 저장 전: 아이콘만(툴팁 강제 OFF)
            SetMode(ViewMode.Icon, force: true);
        }
        else
        {
            // 저장 후: 거리 기반으로 전환되도록 (현재 거리에 따른 즉시 반영은 LateUpdate에서 처리)
            RefreshTexts();
        }
    }

    public void SetTitle(string title)
    {
        if (data == null) return;
        data.title = title ?? "";
        RefreshTexts();
    }

    // “body”를 표준으로 쓰되, content도 같이 동기화(기존 코드 호환)
    public void SetBody(string body)
    {
        if (data == null) return;
        data.body = body ?? "";
        data.content = data.body;
    }

    // MemoEditorUI가 SetContent를 호출하는 경우가 있어 호환용으로 제공
    public void SetContent(string content)
    {
        SetBody(content);
    }

    public void RefreshTexts()
    {
        if (tooltipTitleText == null) return;

        string t = (data != null) ? (data.title ?? "") : "";
        tooltipTitleText.text = string.IsNullOrWhiteSpace(t) ? "(제목 없음)" : t;
    }

    public void SetMode(ViewMode newMode, bool force = false)
    {
        if (!force && mode == newMode) return;
        mode = newMode;

        if (iconRoot) iconRoot.SetActive(mode == ViewMode.Icon);
        if (tooltipRoot) tooltipRoot.SetActive(mode == ViewMode.Tooltip);
    }

    // “툴팁 + 저장완료 상태일 때만 탭해서 편집 UI 열기” 규칙 보장
    public bool TryOpenEdit(MemoEditorUI editor)
    {
        if (!isSaved) return false;
        if (!IsTooltip) return false;
        if (!editor) return false;

        editor.OpenForEdit(this);
        return true;
    }
}
