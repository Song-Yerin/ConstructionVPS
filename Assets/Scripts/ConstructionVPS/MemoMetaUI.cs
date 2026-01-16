
// 현재 날짜/시간/사용자ID 표시, 입력 시 토글 변경 후 이벤트 전달
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoMetaUI : MonoBehaviour
{
    [Header("UI Refs")]
    [Tooltip("메모 창에 날짜/시간/사용자ID를 표시할 텍스트")]
    [SerializeField] private TMP_Text metaText;

    [Tooltip("지정자(메모를 봐야하는 사람) 입력칸")]
    [SerializeField] private TMP_InputField assigneeInput;

    [Tooltip("지정자 입력 시 나타날 체크 버튼(기본 비활성 권장)")]
    [SerializeField] private Button assigneeCheckButton;

    [Header("User Id Source")]
    [Tooltip("PlayerPrefs에서 사용자 ID를 읽을 키(없으면 deviceUniqueIdentifier로 대체)")]
    [SerializeField] private string userIdPrefKey = "USER_ID";

    [Header("Format")]
    [Tooltip("초까지 표시할지")]
    [SerializeField] private bool includeSeconds = true;

    // 외부에서 필요하면 구독해서 “지정자 확정” 이벤트를 받을 수 있게
    public event Action<string> OnAssigneeConfirmed;

    private void Awake()
    {
        // 버튼은 기본 숨김
        if (assigneeCheckButton != null)
            assigneeCheckButton.gameObject.SetActive(false);

        // 입력 변화에 따라 버튼 노출 토글
        if (assigneeInput != null)
            assigneeInput.onValueChanged.AddListener(OnAssigneeChanged);

        // 체크 버튼 클릭 처리
        if (assigneeCheckButton != null)
            assigneeCheckButton.onClick.AddListener(ConfirmAssignee);
    }

    private void OnEnable()
    {
        RefreshMetaText();
        RefreshAssigneeUI();
    }

    private void OnDestroy()
    {
        if (assigneeInput != null)
            assigneeInput.onValueChanged.RemoveListener(OnAssigneeChanged);

        if (assigneeCheckButton != null)
            assigneeCheckButton.onClick.RemoveListener(ConfirmAssignee);
    }

    private void RefreshMetaText()
    {
        if (metaText == null) return;

        // 사용자 ID: PlayerPrefs 우선, 없으면 기기 고유값(대체)
        string userId = PlayerPrefs.GetString(userIdPrefKey, "");
        if (string.IsNullOrWhiteSpace(userId))
            userId = SystemInfo.deviceUniqueIdentifier;

        DateTime now = DateTime.Now;
        string fmt = includeSeconds ? "yyyy-MM-dd HH:mm:ss" : "yyyy-MM-dd HH:mm";
        metaText.text = $"{now.ToString(fmt)} | User: {userId}";
    }

    private void RefreshAssigneeUI()
    {
        if (assigneeInput == null || assigneeCheckButton == null) return;

        string text = assigneeInput.text ?? "";
        bool hasValue = !string.IsNullOrWhiteSpace(text);

        // 값이 있으면 체크 버튼 노출
        assigneeCheckButton.gameObject.SetActive(hasValue);
    }

    private void OnAssigneeChanged(string value)
    {
        RefreshAssigneeUI();
    }

    private void ConfirmAssignee()
    {
        if (assigneeInput == null) return;

        string assignee = (assigneeInput.text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(assignee)) return;

        // “확정” 시 UI 정책(원하면 아래 중 택1로 바꾸면 됨)
        // 1) 입력 잠그기
        // assigneeInput.interactable = false;

        // 2) 버튼 숨기기
        if (assigneeCheckButton != null)
            assigneeCheckButton.gameObject.SetActive(false);

        // 3) 외부에 알리기(저장 로직 연결용)
        OnAssigneeConfirmed?.Invoke(assignee);

        // 디버그 필요하면 여기서 로그
        // Debug.Log($"[MemoMetaUI] Assignee confirmed: {assignee}");
    }
}
