
// 메모를 입력하고 편집하는 모든 UI를 관리하는 컨트롤러
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Reflection;

public class MemoUIController : MonoBehaviour
{
    [Header("Assignee Check Toggle")]
    [Tooltip("지정자 체크박스(토글) 넣는 자리")]
    [SerializeField] private Toggle assigneeCheckToggle;

    [Header("Bottom Bar")]
    [Tooltip("BottomBar 오브젝트를 넣는 자리")]
    [SerializeField] private GameObject bottomBar;

    [Header("Buttons (inside BottomBar)")]
    [Tooltip("BottomBar 안의 버튼들을 넣는 자리")]
    [SerializeField] private Button btnText;
    [SerializeField] private Button btnVoice;
    [SerializeField] private Button btnChecklist;
    [SerializeField] private Button btnImage;

    [Header("Panels (inside BottomBar)")]
    [Tooltip("BottomBar 안의 패널들을 넣는 자리")]
    [SerializeField] private GameObject panelText;
    [SerializeField] private GameObject panelVoice;
    [SerializeField] private GameObject panelChecklist;
    [SerializeField] private GameObject panelImage;

    [Header("Text Memo Inputs (Panel Text)")]
    [Tooltip("TextMemo 패널 안의 TMP_InputField(타이틀) 넣는 자리")]
    [SerializeField] private TMP_InputField inputTitle;
    [Tooltip("TextMemo 패널 안의 TMP_InputField(내용) 넣는 자리")]
    [SerializeField] private TMP_InputField inputBody;

    // 저장 버튼 & 옵션
    [Header("Text Memo Save Button (Panel Text)")]
    [Tooltip("TextMemo 패널 안의 저장 버튼 넣는 자리")]
    [SerializeField] private Button btnSaveText;
    [Tooltip("메모 부착 직후 자동으로 텍스트 패널을 열지 여부")]
    [SerializeField] private bool autoOpenTextPanelOnPlaced = true;

    // 핀 저장소 - JSON 파일에 메모 데이터를 저장하는 TabPinCreate 참조
    [Header("TabPinCreate")]
    [Tooltip("TabPinCreate를 넣는 자리 (JSON 저장 갱신용)")]
    [SerializeField] private TabPinCreate pinStore;

    [Header("Keyboard Focus")]
    [Tooltip("텍스트 패널이 열리면 자동으로 입력칸에 커서를 놓고 키보드를 띄울지 여부")]
    [SerializeField] private bool autoFocusTextInput = true;

    [Header("Back/Close Buttons")]
    [Tooltip("각 패널의 '뒤로가기/닫기' 버튼들을 전부 넣는 자리")]
    [SerializeField] private Button[] backButtons;

    // 메타 정보 (날짜/시간/사용자ID) & 지정자 UI
    [Header("Meta / Assignee UI (Panel Text)")]
    [Tooltip("메모 패널 안에 날짜/시간/사용자ID를 표시할 TMP_Text 넣는 자리")]
    [SerializeField] private TMP_Text metaInfoText;
    [Tooltip("지정자(메모를 봐야하는 사람) 입력 TMP_InputField 넣는 자리")]
    [SerializeField] private TMP_InputField inputAssignee;

    // 사용자 ID 관련 설정
    [Header("User ID")]
    [Tooltip("사용자 ID를 PlayerPrefs에서 읽을 키(없으면 디바이스ID 일부로 대체)")]
    [SerializeField] private string userIdPrefKey = "MEMO_USER_ID";
    [Tooltip("PlayerPrefs에 userId가 없을 때 기기 고유 번호를 대신 사용할지")]
    [SerializeField] private bool useDeviceIdFallback = true;
    [Tooltip("패널을 열 때마다 지정자 입력칸을 비울지 결정")]
    [SerializeField] private bool clearAssigneeOnOpen = false;

    [Header("Debug")]
    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool logDebug = false;

    // 현재 편집 중인 메모 GameObject
    private GameObject currentMemo;

    // Draft 시스템 - 입력 중 데이터가 지워지는 것을 방지하기 위한 임시 저장소
    private bool isLoadingUI = false;   // UI 로딩 중 플래그 (덮어쓰기 방지용)
    private string draftTitle = "";     // 임시 저장된 제목
    private string draftBody = "";      // 임시 저장된 본문
    private string draftAssignee = "";  // 임시 저장된 지정자 이름

    // 지정자 확정 이벤트 - 외부 스크립트에 지정자 확정 알림
    public event Action<string> OnAssigneeConfirmed;

    private void Awake()
    {
        // 모든 UI 숨김
        ForceHideBottomBar();
        HideAllPanels();

        // 하단바 버튼 연결 체크
        if (!btnText || !btnVoice || !btnChecklist || !btnImage)
        {
            Debug.LogWarning("[MemoUIController] One or more bottom bar buttons are not assigned.");
        }

        // 하단바 버튼 클릭 이벤트 연결 - 각 버튼 클릭 시 해당 패널 열기
        if (btnText) btnText.onClick.AddListener(() => OpenPanel(panelText));
        if (btnVoice) btnVoice.onClick.AddListener(() => OpenPanel(panelVoice));
        if (btnChecklist) btnChecklist.onClick.AddListener(() => OpenPanel(panelChecklist));
        if (btnImage) btnImage.onClick.AddListener(() => OpenPanel(panelImage));

        // 저장 버튼 클릭 이벤트 연결
        if (btnSaveText)
        {
            // 중복 등록 방지를 위한 기존 리스너 제거
            btnSaveText.onClick.RemoveListener(SaveTextMemoNow);
            btnSaveText.onClick.AddListener(SaveTextMemoNow);
        }

        // 입력 변화 감지 리스너 연결 (Draft 시스템)
        WireDraftListeners();

        // 지정자 UI 이벤트 연결
        WireAssigneeListeners();

        // 뒤로가기 버튼들 연결
        WireBackButtons();

        // 지정자 토글 초기 상태 설정
        UpdateAssigneeToggleVisibility();
    }

    // 컴포넌트 활성화 시 실행 함수
    private void OnEnable()
    {
        // 재활성화 시 UI 초기화
        ForceHideBottomBar();
        HideAllPanels();
    }

    // 새 메모 부착 완료 시 실행 함수
    public void OnMemoPlaced(GameObject memo)
    {
        // 현재 편집 중인 메모로 설정
        currentMemo = memo;

        if (logDebug)
            Debug.Log($"[MemoUIController] OnMemoPlaced: {(currentMemo ? currentMemo.name : "null")}");

        // 하단바 표시 및 텍스트 패널 열기
        ShowBottomBarOnly();
        if (autoOpenTextPanelOnPlaced)
            OpenPanel(panelText);
    }

    // 기존 메모 선택 시 실행 함수
    public void OnMemoSelected(GameObject memo)
    {
        currentMemo = memo;

        if (logDebug)
            Debug.Log($"[MemoUIController] OnMemoSelected: {(currentMemo ? currentMemo.name : "null")}");

        // 하단바 표시 및 텍스트 패널 열기
        ShowBottomBarOnly();
        OpenPanel(panelText);
    }

    // 모든 UI 닫기 (뒤로가기 버튼에서 호출)
    public void CloseAll()
    {
        if (logDebug) Debug.Log("[MemoUIController] CloseAll()");

        // 닫기 전 작성 중인 내용 자동 저장
        SaveTextMemoIfOpen();

        // 모든 UI 숨김
        HideAllPanels();
        ForceHideBottomBar();

        currentMemo = null;
    }

    // 하단바만 표시하고 모든 패널 숨기기 함수
    private void ShowBottomBarOnly()
    {
        // 하단바 활성화 & 모든 패널 비활성화
        if (bottomBar) bottomBar.SetActive(true);
        HideAllPanels();
    }

    // 다른 패널은 모두 닫고 하나만 활성화
    private void OpenPanel(GameObject target)
    {
        // 하단바가 비활성화 상태면 패널 열기 불가
        if (!bottomBar || !bottomBar.activeSelf)
        {
            if (logDebug) Debug.Log("[MemoUIController] BottomBar is not active. Ignoring panel open.");
            return;
        }

        // 패널 전환 전 현재 작성 중인 내용 저장
        SaveTextMemoIfOpen();

        // 모든 패널 닫기 & 요청한 패널만 활성화
        HideAllPanels();
        if (target) target.SetActive(true);

        // 텍스트 패널일 경우 추가 처리
        if (target == panelText)
        {
            // MemoData에서 저장된 내용 불러오기
            LoadTextMemoToUI();

            // 자동 포커스
            if (autoFocusTextInput)
                StartCoroutine(FocusTextInputNextFrame());
        }

        if (logDebug)
            Debug.Log($"[MemoUIController] OpenPanel: {(target ? target.name : "null")}, currentMemo: {(currentMemo ? currentMemo.name : "null")}");
    }

    // 모든 패널 숨기기 함수
    private void HideAllPanels()
    {
        if (panelText) panelText.SetActive(false);
        if (panelVoice) panelVoice.SetActive(false);
        if (panelChecklist) panelChecklist.SetActive(false);
        if (panelImage) panelImage.SetActive(false);
    }

    // 하단바 강제 숨김 함수
    private void ForceHideBottomBar()
    {
        if (bottomBar) bottomBar.SetActive(false);
    }

    // 뒤로가기 버튼들을 CloseAll 함수에 연결 함수
    private void WireBackButtons()
    {
        // 버튼 배열이 비어있으면 종료
        if (backButtons == null || backButtons.Length == 0)
        {
            if (logDebug) Debug.Log("[MemoUIController] backButtons is empty. (No close buttons wired)");
            return;
        }

        // 모든 뒤로가기 버튼에 CloseAll 연결
        for (int i = 0; i < backButtons.Length; i++)
        {
            Button b = backButtons[i];
            if (!b) continue;

            // 중복 연결 방지
            b.onClick.RemoveListener(CloseAll);
            b.onClick.AddListener(CloseAll);
        }

        if (logDebug) Debug.Log($"[MemoUIController] Wired backButtons: {backButtons.Length}");
    }

    // 입력칸 변화 감지 리스너 연결 함수 - Draft 시스템 구현
    private void WireDraftListeners()
    {
        // 제목 입력칸 변화 감지
        if (inputTitle)
        {
            inputTitle.onValueChanged.RemoveListener(OnTitleChanged);
            inputTitle.onValueChanged.AddListener(OnTitleChanged);
        }

        // 본문 입력칸 변화 감지
        if (inputBody)
        {
            inputBody.onValueChanged.RemoveListener(OnBodyChanged);
            inputBody.onValueChanged.AddListener(OnBodyChanged);
        }
    }

    // 제목 입력 변화 시 Draft에 임시 저장 함수
    private void OnTitleChanged(string v)
    {
        // UI 로딩 중이면 무시 (덮어쓰기 방지)
        if (isLoadingUI) return;

        // Draft에 임시 저장
        draftTitle = v ?? "";
    }

    // 본문 입력 변화 시 Draft에 임시 저장 함수
    private void OnBodyChanged(string v)
    {

        if (isLoadingUI) return;
        draftBody = v ?? "";
    }

    // 지정자 UI 이벤트 연결 함수
    private void WireAssigneeListeners()
    {
        // 지정자 입력칸 변화 감지
        if (inputAssignee)
        {
            inputAssignee.onValueChanged.RemoveListener(OnAssigneeChanged);
            inputAssignee.onValueChanged.AddListener(OnAssigneeChanged);
        }

        // 지정자 체크박스(토글) 변화 감지
        if (assigneeCheckToggle)
        {
            assigneeCheckToggle.onValueChanged.RemoveListener(OnAssigneeToggleChanged);
            assigneeCheckToggle.onValueChanged.AddListener(OnAssigneeToggleChanged);
        }
    }

    // 지정자 입력 변화 시 Draft에 임시 저장 및 토글 표시 함수      
    private void OnAssigneeChanged(string v)
    {
        if (isLoadingUI) return;

        // Draft에 임시 저장
        draftAssignee = v ?? "";

        // 토글 표시/숨김 갱신 (이름 입력하면 토글 나타남)
        UpdateAssigneeToggleVisibility();

        // 입력이 바뀌면 토글 OFF로 초기화
        if (assigneeCheckToggle) assigneeCheckToggle.isOn = false;
    }

    // 지정자 토글 변화 시 처리 - ON되면 지정자 확정 함수   
    private void OnAssigneeToggleChanged(bool isOn)
    {
        // ON될 때만 확정 처리
        if (!isOn) return;

        // 지정자 확정 함수 호출
        ConfirmAssigneeNow();
    }

    // 지정자 토글 표시/숨김 상태 갱신 함수 - 이름 입력 여부에 따라
    private void UpdateAssigneeToggleVisibility()
    {
        // 지정자 이름이 입력되었는지 확인
        bool show = !string.IsNullOrWhiteSpace(draftAssignee);

        // 토글 표시/숨김 및 활성화 상태 설정
        if (assigneeCheckToggle)
        {
            assigneeCheckToggle.gameObject.SetActive(show);
            assigneeCheckToggle.interactable = show;

            // 이름이 비면 토글도 초기화
            if (!show) assigneeCheckToggle.isOn = false;
        }

        if (logDebug)
        {
            Debug.Log($"[MemoUIController] AssigneeUI show={show} draftAssignee='{draftAssignee}' " +
                      $"toggleAssigned={(assigneeCheckToggle != null)}");
        }
    }

    // 지정자 확정 처리 함수 - 토글 ON 시 실행
    private void ConfirmAssigneeNow()
    {
        // 현재 메모가 없으면 종료
        if (!currentMemo) return;

        // 지정자 이름 가져오기
        string assignee = draftAssignee ?? "";
        if (string.IsNullOrWhiteSpace(assignee)) return;

        // MemoData에 지정자 저장 (리플렉션 사용)
        TrySetMemoAssignee(currentMemo, assignee);

        // 외부 스크립트에 이벤트 알림
        OnAssigneeConfirmed?.Invoke(assignee);

        if (logDebug) Debug.Log($"[MemoUIController] Assignee confirmed: '{assignee}'");

        // 토글 상태 갱신
        UpdateAssigneeToggleVisibility();
    }

    // MemoData의 내용을 UI에 로드하는 함수
    private void LoadTextMemoToUI()
    {
        // 현재 메모나 입력칸이 없으면 종료
        if (!currentMemo) return;
        if (!inputTitle || !inputBody)
        {
            if (logDebug) Debug.LogWarning("[MemoUIController] inputTitle/inputBody is not assigned.");
            return;
        }

        // 프리팹 세팅 검증 - 두 InputField가 같은 Text 컴포넌트를 공유하는지 확인
        if (inputTitle.textComponent != null && inputBody.textComponent != null)
        {
            if (ReferenceEquals(inputTitle.textComponent, inputBody.textComponent))
            {
                Debug.LogWarning("[MemoUIController] Title/Body TMP_InputField가 같은 Text(TMP) 컴포넌트를 공유 중입니다. " +
                    "(한쪽 입력 시 다른쪽이 지워지는 현상 발생) 각 InputField의 Text Component를 서로 다른 Text(TMP)로 다시 연결하세요.");
            }
        }

        // 입력 필드 활성화 (읽기 전용 해제)
        inputTitle.interactable = true;
        inputBody.interactable = true;
        inputTitle.readOnly = false;
        inputBody.readOnly = false;

        // MemoData 컴포넌트 가져오기
        MemoData memo = currentMemo.GetComponent<MemoData>();
        if (!memo)
        {
            // MemoData가 없으면 모든 값 초기화
            if (logDebug) Debug.LogWarning("[MemoUIController] MemoData is missing on currentMemo.");
            isLoadingUI = true;
            draftTitle = "";
            draftBody = "";
            inputTitle.text = "";
            inputBody.text = "";

            // 메타 정보 및 지정자도 초기화
            UpdateMetaInfoText();
            LoadAssigneeToUI(null);

            isLoadingUI = false;
            return;
        }

        // UI 로딩 시작 (입력 변화 이벤트 무시)
        isLoadingUI = true;

        // MemoData에서 저장된 값 읽어서 Draft에 저장
        draftTitle = memo.title ?? "";
        draftBody = memo.body ?? "";

        // UI 입력칸에 표시
        inputTitle.text = draftTitle;
        inputBody.text = draftBody;

        // 메타 정보 갱신 (날짜/시간/사용자ID)
        UpdateMetaInfoText();

        // 지정자 정보 로드
        LoadAssigneeToUI(memo);

        // UI 로딩 완료
        isLoadingUI = false;
    }

    // 저장 버튼 클릭 시 실행 함수
    private void SaveTextMemoNow()
    {
        // 현재 메모나 입력칸이 없으면 종료
        if (!currentMemo) return;
        if (!inputTitle || !inputBody) return;

        // 실제 저장 처리
        ApplySaveFromUIAndSync();

        // 저장 완료 후 모든 UI 닫기
        CloseAll();
    }

    // 텍스트 패널이 열려있을 때만 저장 함수 - 패널 전환/닫기 시 자동 호출
    private void SaveTextMemoIfOpen()
    {
        // 텍스트 패널이 열려있지 않으면 종료
        if (!panelText || !panelText.activeSelf) return;
        if (!currentMemo) return;

        // 실제 저장 처리
        ApplySaveFromUIAndSync();
    }

    // UI의 Draft 값을 MemoData와 JSON에 저장하는 실제 저장 함수
    private void ApplySaveFromUIAndSync()
    {
        // MemoData 컴포넌트 가져오기
        MemoData memo = currentMemo.GetComponent<MemoData>();
        if (!memo)
        {
            if (logDebug) Debug.LogWarning("[MemoUIController] MemoData is missing on currentMemo (cannot save).");
            return;
        }

        // Draft를 우선 사용 (입력 중 UI 갱신으로 인한 덮어쓰기 방지)
        string title = draftTitle ?? (inputTitle ? inputTitle.text : "");
        string body = draftBody ?? (inputBody ? inputBody.text : "");

        // MemoData에 저장
        memo.title = title ?? "";
        memo.body = body ?? "";
        memo.content = memo.body; // 호환성 유지

        // 지정자가 있으면 저장
        if (!string.IsNullOrWhiteSpace(draftAssignee))
            TrySetMemoAssignee(currentMemo, draftAssignee);

        // JSON 파일에도 저장
        if (pinStore != null)
        {
            pinStore.SaveTextMemoById(memo.id, memo.title, memo.body);
        }
        else
        {
            if (logDebug) Debug.LogWarning("[MemoUIController] pinStore is null. Assign TabPinCreate in inspector.");
        }
    }

    // UI가 활성화되어 있으면 입력 차단 함수
    public bool IsUIBlockingWorldInput()
    {
        // 하단바가 켜져 있으면 차단
        if (bottomBar && bottomBar.activeInHierarchy) return true;

        // 패널 중 하나라도 켜져 있으면 차단
        if (panelText && panelText.activeInHierarchy) return true;
        if (panelVoice && panelVoice.activeInHierarchy) return true;
        if (panelChecklist && panelChecklist.activeInHierarchy) return true;
        if (panelImage && panelImage.activeInHierarchy) return true;

        return false;
    }

    // 자동 포커스 설정 함수
    private IEnumerator FocusTextInputNextFrame()
    {
        // UI 레이아웃이 완전히 준비될 시간 확보
        yield return null;

        // 패널이 닫혔거나 입력칸이 없으면 종료
        if (!panelText || !panelText.activeSelf) yield break;
        if (!inputBody && !inputTitle) yield break;

        // 포커스 대상 결정 - 제목이 비어있으면 제목, 아니면 본문
        TMP_InputField target = inputTitle;
        if (target && !string.IsNullOrWhiteSpace(target.text))
            target = inputBody ? inputBody : inputTitle;

        if (!target) yield break;

        // 입력 필드 활성화
        target.interactable = true;
        target.readOnly = false;

        // 포커스 설정 및 키보드 활성화
        target.Select();
        target.ActivateInputField();
    }

    // 메타 정보 텍스트 갱신 함수
    private void UpdateMetaInfoText()
    {
        if (!metaInfoText) return;

        // 사용자 ID 가져오기 (PlayerPrefs 또는 디바이스 ID)
        string userId = PlayerPrefs.GetString(userIdPrefKey, "");
        if (string.IsNullOrWhiteSpace(userId) && useDeviceIdFallback)
        {
            // PlayerPrefs에 없으면 디바이스 ID 사용
            string dev = SystemInfo.deviceUniqueIdentifier ?? "";
            userId = (dev.Length > 8) ? dev.Substring(0, 8) : dev;
        }

        // 현재 날짜/시간 가져오기
        string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
        string timeStr = DateTime.Now.ToString("HH:mm:ss");

        // 텍스트 표시 설정 (한 줄 고정)
        metaInfoText.enableWordWrapping = false;                 // 줄바꿈 금지
        metaInfoText.overflowMode = TextOverflowModes.Ellipsis;  // 넘치면 ... 처리
        metaInfoText.maxVisibleLines = 1;                        // 한 줄만 표시

        // 메타 정보 텍스트 설정
        metaInfoText.text = $"Date : {dateStr} / Time : {timeStr} / UserID : {userId}";
    }

    // 지정자 UI에 저장된 값 로드 함수
    private void LoadAssigneeToUI(MemoData memo)
    {
        if (!inputAssignee) return;

        // 저장된 지정자 읽기 (옵션에 따라)
        string existing = "";
        if (!clearAssigneeOnOpen && memo != null)
            existing = TryGetMemoAssignee(memo);

        // UI 로딩 시작
        isLoadingUI = true;

        // Draft와 UI에 값 설정
        draftAssignee = existing ?? "";
        inputAssignee.interactable = true;
        inputAssignee.readOnly = false;
        inputAssignee.text = draftAssignee;

        // 토글 초기화
        if (assigneeCheckToggle) assigneeCheckToggle.isOn = false;

        // 토글 표시/숨김 갱신
        UpdateAssigneeToggleVisibility();

        // UI 로딩 완료
        isLoadingUI = false;
    }

    // MemoData에 assignee 필드가 있으면 저장 함수
    private static void TrySetMemoAssignee(GameObject memoGO, string assignee)
    {
        if (!memoGO) return;
        var memo = memoGO.GetComponent<MemoData>();
        if (!memo) return;

        // 리플렉션으로 필드/프로퍼티 찾기
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var t = memo.GetType();

        // 필드 우선 검색
        var f = t.GetField("assignee", flags);
        if (f != null && f.FieldType == typeof(string))
        {
            f.SetValue(memo, assignee ?? "");
            return;
        }

        // 프로퍼티 검색
        var p = t.GetProperty("Assignee", flags) ?? t.GetProperty("assignee", flags);
        if (p != null && p.CanWrite && p.PropertyType == typeof(string))
        {
            p.SetValue(memo, assignee ?? "");
        }
    }

    // MemoData에서 assignee 필드 읽기 함수
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
