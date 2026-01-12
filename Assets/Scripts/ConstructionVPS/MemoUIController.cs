
// 메모 부착 시에만 하단바 표시 > 버튼으로 각 메모 패널을 열기/닫기 (메모UI 관리 컨트롤러)
using UnityEngine;
using UnityEngine.UI;

public class MemoUIController : MonoBehaviour
{
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

    [Header("Back/Close Buttons (All panels)")]
    [Tooltip("각 패널의 '뒤로가기/닫기' 버튼들을 전부 넣는 자리")]
    [SerializeField] private Button[] backButtons; 

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private GameObject currentMemo;

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

        // 모든 뒤로가기/닫기 버튼에 CloseAll() 연결 (코드로 일괄)
        WireBackButtons(); 
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
    }


    // 뒤로가기/닫기: 패널 전부 끄고 하단바도 끔
    public void CloseAll()
    {
        if (logDebug) Debug.Log("[MemoUIController] CloseAll()");
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

        HideAllPanels();

        if (target) target.SetActive(true);

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

    // 추가: 뒤로가기 버튼들을 코드로 CloseAll에 연결
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
}
