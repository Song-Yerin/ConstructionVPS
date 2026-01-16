
// 메모 부착 시에만 하단바 표시 > 버튼으로 각 메모 패널을 열기/닫기 (메모UI 관리 컨트롤러)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Reflection;

public class MemoUIController : MonoBehaviour
{
    // 지정자 체크박스(토글)
    [SerializeField] private Toggle assigneeCheckToggle;

    [Header("Bottom Bar Root (SafeArea/BottomBar)")]
    [SerializeField] private GameObject bottomBar;

    [Header("Buttons (inside BottomBar)")]
    [SerializeField] private Button btnText;
    [SerializeField] private Button btnVoice;
    [SerializeField] private Button btnChecklist;
    [SerializeField] private Button btnImage;

    [Header("Panels")]
    [SerializeField] private GameObject panelText;
    [SerializeField] private GameObject panelVoice;
    [SerializeField] private GameObject panelChecklist;
    [SerializeField] private GameObject panelImage;

    // 텍스트 메모 입력칸(타이틀/내용)
    [Header("Text Memo Inputs (Panel Text)")]
    [Tooltip("TextMemo 패널 안의 TMP_InputField(타이틀) 연결")]
    [SerializeField] private TMP_InputField inputTitle;

    [Tooltip("TextMemo 패널 안의 TMP_InputField(내용) 연결")]
    [SerializeField] private TMP_InputField inputBody;

    [Header("Text Memo Save Button (Panel Text)")]
    [Tooltip("TextMemo 패널 안의 저장 버튼(Button) 연결")]
    [SerializeField] private Button btnSaveText;

    [Tooltip("메모 부착 직후 자동으로 텍스트 패널을 열지 여부")]
    [SerializeField] private bool autoOpenTextPanelOnPlaced = true;

    [Header("Pin Storage (TabPinCreate)")]
    [Tooltip("TabPinCreate를 넣는 자리 (JSON 저장 갱신용)")]
    [SerializeField] private TabPinCreate pinStore;

    [Header("Keyboard Focus (Optional)")]
    [Tooltip("텍스트 패널 열릴 때 자동 포커스/키보드 오픈")]
    [SerializeField] private bool autoFocusTextInput = true;

    [Header("Back/Close Buttons (All panels)")]
    [Tooltip("각 패널의 '뒤로가기/닫기' 버튼들을 전부 넣는 자리")]
    [SerializeField] private Button[] backButtons;

    // 메모 메타(날짜/시간/사용자ID) + 지정자 UI
    [Header("Meta / Assignee UI (Panel Text)")]
    [Tooltip("메모 패널 안에 날짜/시간/사용자ID를 표시할 TMP_Text")]
    [SerializeField] private TMP_Text metaInfoText;

    [Tooltip("지정자(메모를 봐야하는 사람) 입력 TMP_InputField")]
    [SerializeField] private TMP_InputField inputAssignee;

    [Tooltip("지정자 입력 후 나타날 체크 버튼")]
    [SerializeField] private Button btnConfirmAssignee;

    [Tooltip("사용자 ID를 PlayerPrefs에서 읽을 키(없으면 디바이스ID 일부로 대체)")]
    [SerializeField] private string userIdPrefKey = "MEMO_USER_ID";

    [Tooltip("PlayerPrefs에 userId가 없을 때 SystemInfo.deviceUniqueIdentifier 일부를 표시할지")]
    [SerializeField] private bool useDeviceIdFallback = true;

    [Tooltip("메모 패널 열 때 지정자 입력칸을 비울지(원하면 false)")]
    [SerializeField] private bool clearAssigneeOnOpen = false;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private GameObject currentMemo;

    // 입력 중 다른 곳 클릭/포커스 이동 시 텍스트가 지워지는 것을 막기 위해 "드래프트(임시값)"를 유지
    private bool isLoadingUI = false;   // LoadTextMemoToUI()로 UI에 값 주입할 때 onValueChanged가 덮어쓰지 않게
    private string draftTitle = "";
    private string draftBody = "";

    // 지정자 드래프트
    private string draftAssignee = "";

    // (선택) 지정자 확정 이벤트(외부에서 받고 싶으면 사용)
    public event Action<string> OnAssigneeConfirmed;

    private void Awake()
    {
        // 씬 시작 시 무조건 숨김
        ForceHideBottomBar();
        HideAllPanels();

        // 버튼/패널 연결 체크
        if (!btnText || !btnVoice || !btnChecklist || !btnImage)
        {
            Debug.LogWarning("[MemoUIController] One or more bottom bar buttons are not assigned.");
        }

        // 하단바 버튼 클릭 이벤트 연결
        if (btnText) btnText.onClick.AddListener(() => OpenPanel(panelText));
        if (btnVoice) btnVoice.onClick.AddListener(() => OpenPanel(panelVoice));
        if (btnChecklist) btnChecklist.onClick.AddListener(() => OpenPanel(panelChecklist));
        if (btnImage) btnImage.onClick.AddListener(() => OpenPanel(panelImage));

        // 텍스트 저장 버튼
        if (btnSaveText)
        {
            btnSaveText.onClick.RemoveListener(SaveTextMemoNow);
            btnSaveText.onClick.AddListener(SaveTextMemoNow);
        }

        // 입력 변화는 MemoData에 즉시 쓰지 않고 "드래프트"에만 반영(지워짐/덮어쓰기 방지)
        WireDraftListeners();

        // 지정자 입력/체크 버튼 연결
        WireAssigneeListeners();

        // 모든 뒤로가기/닫기 버튼에 CloseAll() 연결
        WireBackButtons();

        // 시작 시 체크 버튼/토글 숨김
        UpdateAssigneeConfirmButtonVisibility();
    }

    private void OnEnable()
    {
        // 컨트롤러가 재활성화될 때도 초기화
        ForceHideBottomBar();
        HideAllPanels();
    }

    // "메모 부착이 확정되는 순간" 외부에서 호출
    public void OnMemoPlaced(GameObject memo)
    {
        currentMemo = memo;

        if (logDebug)
            Debug.Log($"[MemoUIController] OnMemoPlaced: {(currentMemo ? currentMemo.name : "null")}");

        ShowBottomBarOnly();

        // 바로 텍스트 패널 열기
        if (autoOpenTextPanelOnPlaced)
            OpenPanel(panelText);
    }

    // 기존 핀(버튼)을 탭해서 수정 모드" 외부에서 호출
    public void OnMemoSelected(GameObject memo)
    {
        currentMemo = memo;

        if (logDebug)
            Debug.Log($"[MemoUIController] OnMemoSelected: {(currentMemo ? currentMemo.name : "null")}");

        ShowBottomBarOnly();
        OpenPanel(panelText); // 선택 시 텍스트 패널을 열어 저장된 내용 로드
    }

    // 뒤로가기/닫기: 패널 전부 끄고 하단바도 끔
    public void CloseAll()
    {
        if (logDebug) Debug.Log("[MemoUIController] CloseAll()");

        // 닫기 전에 텍스트 패널이 열려있으면 저장
        SaveTextMemoIfOpen();

        HideAllPanels();
        ForceHideBottomBar();
        currentMemo = null;
    }

    private void ShowBottomBarOnly()
    {
        if (bottomBar) bottomBar.SetActive(true);
        HideAllPanels();
    }

    private void OpenPanel(GameObject target)
    {
        if (!bottomBar || !bottomBar.activeSelf)
        {
            if (logDebug) Debug.Log("[MemoUIController] BottomBar is not active. Ignoring panel open.");
            return;
        }

        // 다른 패널로 이동할 때, 텍스트 패널이 열려 있었다면 먼저 저장
        SaveTextMemoIfOpen();

        HideAllPanels();

        if (target) target.SetActive(true);

        // 텍스트 패널이면 현재 핀 데이터 로드 + 포커스
        if (target == panelText)
        {
            LoadTextMemoToUI();

            if (autoFocusTextInput)
                StartCoroutine(FocusTextInputNextFrame());
        }

        if (logDebug)
            Debug.Log($"[MemoUIController] OpenPanel: {(target ? target.name : "null")}, currentMemo: {(currentMemo ? currentMemo.name : "null")}");
    }

    private void HideAllPanels()
    {
        if (panelText) panelText.SetActive(false);
        if (panelVoice) panelVoice.SetActive(false);
        if (panelChecklist) panelChecklist.SetActive(false);
        if (panelImage) panelImage.SetActive(false);
    }

    private void ForceHideBottomBar()
    {
        if (bottomBar) bottomBar.SetActive(false);
    }

    // 뒤로가기 버튼들을 코드로 CloseAll에 연결
    private void WireBackButtons()
    {
        if (backButtons == null || backButtons.Length == 0)
        {
            if (logDebug) Debug.Log("[MemoUIController] backButtons is empty. (No close buttons wired)");
            return;
        }

        for (int i = 0; i < backButtons.Length; i++)
        {
            Button b = backButtons[i];
            if (!b) continue;

            // 중복 연결 방지(같은 스크립트가 재컴파일/재실행될 때 안전)
            b.onClick.RemoveListener(CloseAll);
            b.onClick.AddListener(CloseAll);
        }

        if (logDebug) Debug.Log($"[MemoUIController] Wired backButtons: {backButtons.Length}");
    }

    // 입력 변화는 드래프트만 갱신 (입력 중 다른 칸이 지워지는 문제를 줄인다)
    private void WireDraftListeners()
    {
        if (inputTitle)
        {
            inputTitle.onValueChanged.RemoveListener(OnTitleChanged);
            inputTitle.onValueChanged.AddListener(OnTitleChanged);
        }

        if (inputBody)
        {
            inputBody.onValueChanged.RemoveListener(OnBodyChanged);
            inputBody.onValueChanged.AddListener(OnBodyChanged);
        }
    }

    private void OnTitleChanged(string v)
    {
        if (isLoadingUI) return;
        draftTitle = v ?? "";
    }

    private void OnBodyChanged(string v)
    {
        if (isLoadingUI) return;
        draftBody = v ?? "";
    }

    // 지정자 입력/확정 UI 연결
    private void WireAssigneeListeners()
    {
        if (inputAssignee)
        {
            inputAssignee.onValueChanged.RemoveListener(OnAssigneeChanged);
            inputAssignee.onValueChanged.AddListener(OnAssigneeChanged);
        }

        // 버튼 방식은 유지하되, 토글이 있으면 토글로 "확정"도 할 수 있게 한다.
        if (btnConfirmAssignee)
        {
            btnConfirmAssignee.onClick.RemoveListener(ConfirmAssigneeNow);
            btnConfirmAssignee.onClick.AddListener(ConfirmAssigneeNow);
        }

        if (assigneeCheckToggle)
        {
            assigneeCheckToggle.onValueChanged.RemoveListener(OnAssigneeToggleChanged);
            assigneeCheckToggle.onValueChanged.AddListener(OnAssigneeToggleChanged);
        }
    }

    private void OnAssigneeChanged(string v)
    {
        if (isLoadingUI) return;
        draftAssignee = v ?? "";
        UpdateAssigneeConfirmButtonVisibility();

        // 입력이 바뀌면 토글은 기본 OFF로 되돌림(원하면 제거 가능)
        if (assigneeCheckToggle) assigneeCheckToggle.isOn = false;
    }

    private void OnAssigneeToggleChanged(bool isOn)
    {
        // ON될 때만 확정 처리 (OFF는 단순 토글 상태)
        if (!isOn) return;
        ConfirmAssigneeNow();
    }

    private void UpdateAssigneeConfirmButtonVisibility()
    {
        bool show = !string.IsNullOrWhiteSpace(draftAssignee);

        // 버튼 방식 유지할 거면 이 줄 유지
        if (btnConfirmAssignee) btnConfirmAssignee.gameObject.SetActive(show);

        // 토글이 “이름 입력 시 나타나지 않는 문제” 방지용 강제 처리
        if (assigneeCheckToggle)
        {
            assigneeCheckToggle.gameObject.SetActive(show);
            assigneeCheckToggle.interactable = show;

            // 이름이 비면 토글도 초기화
            if (!show) assigneeCheckToggle.isOn = false;
        }

        // 디버그: 인스펙터 할당이 안 된 경우를 바로 잡아냄
        if (logDebug)
        {
            Debug.Log($"[MemoUIController] AssigneeUI show={show} draftAssignee='{draftAssignee}' " +
                      $"toggleAssigned={(assigneeCheckToggle != null)} buttonAssigned={(btnConfirmAssignee != null)}");
        }
    }

    private void ConfirmAssigneeNow()
    {
        if (!currentMemo) return;

        string assignee = draftAssignee ?? "";
        if (string.IsNullOrWhiteSpace(assignee)) return;

        // 여기서 “현재 선택된 메모”에 assignee를 저장하도록 연결(가능하면 MemoData에 넣고, 없으면 무시)
        TrySetMemoAssignee(currentMemo, assignee);

        // 외부에서 받고 싶으면 이벤트로 전달
        OnAssigneeConfirmed?.Invoke(assignee);

        if (logDebug) Debug.Log($"[MemoUIController] Assignee confirmed: '{assignee}'");

        // 원하면 확정 후 버튼을 숨길 수도 있음(요구사항엔 없어서 유지)
        UpdateAssigneeConfirmButtonVisibility();
    }

    // 텍스트 메모: UI 로드/저장
    private void LoadTextMemoToUI()
    {
        if (!currentMemo) return;
        if (!inputTitle || !inputBody)
        {
            if (logDebug) Debug.LogWarning("[MemoUIController] inputTitle/inputBody is not assigned.");
            return;
        }

        // 프리팹 세팅이 잘못돼서 두 InputField가 같은 Text(TMP)를 공유하면
        // 한쪽 입력 시 다른쪽이 지워지는 현상이 매우 자주 발생
        // 이 경우 코드가 아니라, 프리팹 연결을 고쳐야 함. (경고 로그를 찍는다)
        if (inputTitle.textComponent != null && inputBody.textComponent != null)
        {
            if (ReferenceEquals(inputTitle.textComponent, inputBody.textComponent))
            {
                Debug.LogWarning("[MemoUIController] Title/Body TMP_InputField가 같은 Text(TMP) 컴포넌트를 공유 중입니다. (한쪽 입력 시 다른쪽이 지워지는 현상 발생) 각 InputField의 Text Component를 서로 다른 Text(TMP)로 다시 연결하세요.");
            }
        }

        // 입력 필드가 읽기전용/비활성으로 되어있으면 타이핑이 안 될 수 있어서 안전 세팅
        inputTitle.interactable = true;
        inputBody.interactable = true;
        inputTitle.readOnly = false;
        inputBody.readOnly = false;

        MemoData memo = currentMemo.GetComponent<MemoData>();
        if (!memo)
        {
            if (logDebug) Debug.LogWarning("[MemoUIController] MemoData is missing on currentMemo.");
            isLoadingUI = true;
            draftTitle = "";
            draftBody = "";
            inputTitle.text = "";
            inputBody.text = "";

            // 메타/지정자도 초기화
            UpdateMetaInfoText();
            LoadAssigneeToUI(null);

            isLoadingUI = false;
            return;
        }

        // 저장된 값이 UI에 들어오게 한다 (드래프트도 함께 초기화)
        isLoadingUI = true;

        draftTitle = memo.title ?? "";
        draftBody = memo.body ?? "";

        inputTitle.text = draftTitle;
        inputBody.text = draftBody;

        // 메모 윈도우가 뜨면, 그 안에 현재 날짜, 시간, 사용자 ID를 Text로 자동 추가
        UpdateMetaInfoText();

        // 지정자 입력칸 로드(저장된 값이 있으면 보여주고, 없으면 옵션에 따라 비움)
        LoadAssigneeToUI(memo);

        isLoadingUI = false;
    }

    // 저장 버튼이 누르는 명확한 작성 완료 트리거
    private void SaveTextMemoNow()
    {
        if (!currentMemo) return;
        if (!inputTitle || !inputBody) return;

        // 텍스트 패널이 안 열려있어도, 버튼이 눌렸다면 저장 처리해도 무방
        ApplySaveFromUIAndSync();

        // 저장 완료 후 UI 정리(원하면 CloseAll 대신 panelText 유지로 바꿀 수 있음)
        CloseAll();
    }

    private void SaveTextMemoIfOpen()
    {
        if (!panelText || !panelText.activeSelf) return; // 텍스트 패널이 열려있을 때만 저장
        if (!currentMemo) return;

        ApplySaveFromUIAndSync();
    }

    // UI > MemoData/JSON/툴팁 타이틀까지 동기화
    private void ApplySaveFromUIAndSync()
    {
        MemoData memo = currentMemo.GetComponent<MemoData>();
        if (!memo)
        {
            if (logDebug) Debug.LogWarning("[MemoUIController] MemoData is missing on currentMemo (cannot save).");
            return;
        }

        // 저장은 "현재 입력 필드 값"이 아니라 드래프트 기준으로
        // 이유: 입력 중 Load/Focus 등으로 UI가 갱신되면 text가 덮일 수 있어서, 드래프트를 단일 진실로
        string title = draftTitle ?? (inputTitle ? inputTitle.text : "");
        string body = draftBody ?? (inputBody ? inputBody.text : "");

        // 핀(메모 오브젝트)에도 저장
        memo.title = title ?? "";
        memo.body = body ?? "";
        memo.content = memo.body; // 호환 유지

        // 지정자는 여기서 자동 저장까지는 “옵션” (원하면 MemoData/PinData 확장 필요)
        // 현재는 MemoData에 assignee 필드/프로퍼티가 있을 때만 넣는다.
        if (!string.IsNullOrWhiteSpace(draftAssignee))
            TrySetMemoAssignee(currentMemo, draftAssignee);

        // JSON(DB)에도 저장 (TabPinCreate에 저장 함수 추가되어 있어야 함)
        if (pinStore != null)
        {
            pinStore.SaveTextMemoById(memo.id, memo.title, memo.body);
        }
        else
        {
            if (logDebug) Debug.LogWarning("[MemoUIController] pinStore is null. Assign TabPinCreate in inspector.");
        }
    }

    // UI가 열려있는 동안은 AR 탭(핀 생성/선택)을 막기 위한 상태 제공
    public bool IsUIBlockingWorldInput()
    {
        // bottomBar가 켜져 있거나, 어떤 패널이든 켜져 있으면 UI가 입력을 가져가야 함
        if (bottomBar && bottomBar.activeInHierarchy) return true;

        if (panelText && panelText.activeInHierarchy) return true;
        if (panelVoice && panelVoice.activeInHierarchy) return true;
        if (panelChecklist && panelChecklist.activeInHierarchy) return true;
        if (panelImage && panelImage.activeInHierarchy) return true;

        return false;
    }

    private IEnumerator FocusTextInputNextFrame()
    {
        // 패널 활성화 프레임에 레이아웃이 아직 안 잡힐 수 있어 1프레임 대기
        yield return null;

        if (!panelText || !panelText.activeSelf) yield break;
        if (!inputBody && !inputTitle) yield break;

        // 타이틀이 비어있으면 타이틀부터, 아니면 내용으로 포커스
        TMP_InputField target = inputTitle;
        if (target && !string.IsNullOrWhiteSpace(target.text))
            target = inputBody ? inputBody : inputTitle;

        if (!target) yield break;

        // 포커스/키보드가 안 뜨는 케이스 대비
        target.interactable = true;
        target.readOnly = false;

        target.Select();
        target.ActivateInputField();
    }

    // 메타정보 텍스트 갱신(현재 날짜/시간 + 사용자ID)
    private void UpdateMetaInfoText()
    {
        if (!metaInfoText) return;

        string userId = PlayerPrefs.GetString(userIdPrefKey, "");
        if (string.IsNullOrWhiteSpace(userId) && useDeviceIdFallback)
        {
            string dev = SystemInfo.deviceUniqueIdentifier ?? "";
            userId = (dev.Length > 8) ? dev.Substring(0, 8) : dev;
        }

        // 날짜 / 시간 분리
        string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
        string timeStr = DateTime.Now.ToString("HH:mm:ss");

        // “한 줄 고정” 강제 세팅 (Rect가 좁아도 줄바꿈 대신 잘리게)
        metaInfoText.enableWordWrapping = false;                 // 줄바꿈 금지
        metaInfoText.overflowMode = TextOverflowModes.Ellipsis; // 넘치면 ... 처리(원하면 Overflow로)
        metaInfoText.maxVisibleLines = 1;                        // 한 줄만 보이게

        // 한 줄 + 슬래시 구분 (deltaTime 같은 건 출력하지 않음)
        metaInfoText.text = $"Date : {dateStr} / Time : {timeStr} / UserID : {userId}";
    }


    // 지정자 UI 로드(가능하면 MemoData에서 assignee 읽기)
    private void LoadAssigneeToUI(MemoData memo)
    {
        if (!inputAssignee) return;

        string existing = "";

        if (!clearAssigneeOnOpen && memo != null)
            existing = TryGetMemoAssignee(memo);

        isLoadingUI = true;

        draftAssignee = existing ?? "";
        inputAssignee.interactable = true;
        inputAssignee.readOnly = false;
        inputAssignee.text = draftAssignee;

        // 토글도 같이 동기화(확정 여부 저장은 MemoData 확장 없으면 불가하므로 기본 OFF)
        if (assigneeCheckToggle) assigneeCheckToggle.isOn = false;

        UpdateAssigneeConfirmButtonVisibility();

        isLoadingUI = false;
    }

    // MemoData에 assignee 필드/프로퍼티가 "있을 때만" 저장/읽기 (없으면 조용히 무시)
    private static void TrySetMemoAssignee(GameObject memoGO, string assignee)
    {
        if (!memoGO) return;
        var memo = memoGO.GetComponent<MemoData>();
        if (!memo) return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var t = memo.GetType();

        // field 우선
        var f = t.GetField("assignee", flags);
        if (f != null && f.FieldType == typeof(string))
        {
            f.SetValue(memo, assignee ?? "");
            return;
        }

        // property 다음
        var p = t.GetProperty("Assignee", flags) ?? t.GetProperty("assignee", flags);
        if (p != null && p.CanWrite && p.PropertyType == typeof(string))
        {
            p.SetValue(memo, assignee ?? "");
        }
    }

    private static string TryGetMemoAssignee(MemoData memo)
    {
        if (!memo) return "";

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var t = memo.GetType();

        var f = t.GetField("assignee", flags);
        if (f != null && f.FieldType == typeof(string))
            return (string)f.GetValue(memo) ?? "";

        var p = t.GetProperty("Assignee", flags) ?? t.GetProperty("assignee", flags);
        if (p != null && p.CanRead && p.PropertyType == typeof(string))
            return (string)p.GetValue(memo) ?? "";

        return "";
    }
}
