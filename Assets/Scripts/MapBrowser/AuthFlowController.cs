
// 로그인/회원가입 화면 전환을 관리하는 스크립트
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AuthFlowController : MonoBehaviour
{
    [Header("Canvas Groups")]
    [Tooltip("전체 인증 화면 Canvas")]
    [SerializeField] private Canvas authCanvas;

    [Tooltip("홈 화면 Canvas")]
    [SerializeField] private Canvas homeCanvas;

    [Header("Panels")]
    [Tooltip("초기 로그인 메인 화면")]
    [SerializeField] private GameObject loginMainPanel;
    [SerializeField] private CanvasGroup loginMainCanvasGroup;

    [Tooltip("로그인 입력 화면")]
    [SerializeField] private GameObject loginInputPanel;
    [SerializeField] private CanvasGroup loginInputCanvasGroup;

    [Tooltip("회원가입 입력 화면")]
    [SerializeField] private GameObject signupInputPanel;
    [SerializeField] private CanvasGroup signupInputCanvasGroup;

    [Header("Buttons - Login Main")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button signupButton;

    [Header("Buttons - Login Input")]
    [SerializeField] private Button loginBackButton;
    [SerializeField] private Button loginCompleteButton;

    [Header("Buttons - Signup Input")]
    [SerializeField] private Button signupBackButton;
    [SerializeField] private Button signupCompleteButton;

    [Header("Fade Settings")]
    [Tooltip("패널 전환 시 페이드 효과 사용")]
    [SerializeField] private bool useFadeEffect = true;

    [Tooltip("페이드 인/아웃 시간")]
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        // CanvasGroup 자동 찾기 (연결되지 않은 경우)
        if (loginMainPanel && loginMainCanvasGroup == null)
            loginMainCanvasGroup = GetOrAddCanvasGroup(loginMainPanel);
        if (loginInputPanel && loginInputCanvasGroup == null)
            loginInputCanvasGroup = GetOrAddCanvasGroup(loginInputPanel);
        if (signupInputPanel && signupInputCanvasGroup == null)
            signupInputCanvasGroup = GetOrAddCanvasGroup(signupInputPanel);

        // 초기 상태: 인증 화면만 표시, 홈 화면 숨김
        if (authCanvas) authCanvas.gameObject.SetActive(true);
        if (homeCanvas) homeCanvas.gameObject.SetActive(false);

        ShowLoginMain();

        // 버튼 이벤트 연결
        RegisterButtonEvents();
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = obj.AddComponent<CanvasGroup>();
        }
        return cg;
    }

    private void RegisterButtonEvents()
    {
        // 메인 화면 버튼
        if (loginButton) loginButton.onClick.AddListener(OnLoginButtonClicked);
        if (signupButton) signupButton.onClick.AddListener(OnSignupButtonClicked);

        // 로그인 입력 화면 버튼
        if (loginBackButton) loginBackButton.onClick.AddListener(OnLoginBackClicked);
        if (loginCompleteButton) loginCompleteButton.onClick.AddListener(OnLoginComplete);

        // 회원가입 입력 화면 버튼
        if (signupBackButton) signupBackButton.onClick.AddListener(OnSignupBackClicked);
        if (signupCompleteButton) signupCompleteButton.onClick.AddListener(OnSignupComplete);
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지
        if (loginButton) loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        if (signupButton) signupButton.onClick.RemoveListener(OnSignupButtonClicked);
        if (loginBackButton) loginBackButton.onClick.RemoveListener(OnLoginBackClicked);
        if (loginCompleteButton) loginCompleteButton.onClick.RemoveListener(OnLoginComplete);
        if (signupBackButton) signupBackButton.onClick.RemoveListener(OnSignupBackClicked);
        if (signupCompleteButton) signupCompleteButton.onClick.RemoveListener(OnSignupComplete);
    }

    // === 화면 전환 함수들 ===

    private void ShowLoginMain()
    {
        if (showDebugLogs) Debug.Log("[AuthFlow] 로그인 메인 화면 표시");

        if (useFadeEffect)
        {
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = StartCoroutine(FadeToPanel(loginMainPanel, loginMainCanvasGroup));
        }
        else
        {
            if (loginMainPanel) loginMainPanel.SetActive(true);
            if (loginInputPanel) loginInputPanel.SetActive(false);
            if (signupInputPanel) signupInputPanel.SetActive(false);
        }
    }

    private void ShowLoginInput()
    {
        if (showDebugLogs) Debug.Log("[AuthFlow] 로그인 입력 화면 표시");

        if (useFadeEffect)
        {
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = StartCoroutine(FadeToPanel(loginInputPanel, loginInputCanvasGroup));
        }
        else
        {
            if (loginMainPanel) loginMainPanel.SetActive(false);
            if (loginInputPanel) loginInputPanel.SetActive(true);
            if (signupInputPanel) signupInputPanel.SetActive(false);
        }
    }

    private void ShowSignupInput()
    {
        if (showDebugLogs) Debug.Log("[AuthFlow] 회원가입 입력 화면 표시");

        if (useFadeEffect)
        {
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = StartCoroutine(FadeToPanel(signupInputPanel, signupInputCanvasGroup));
        }
        else
        {
            if (loginMainPanel) loginMainPanel.SetActive(false);
            if (loginInputPanel) loginInputPanel.SetActive(false);
            if (signupInputPanel) signupInputPanel.SetActive(true);
        }
    }

    private void ShowHome()
    {
        if (showDebugLogs) Debug.Log("[AuthFlow] 홈 화면으로 이동");

        // 인증 화면 숨기고 홈 화면 표시
        if (authCanvas) authCanvas.gameObject.SetActive(false);
        if (homeCanvas) homeCanvas.gameObject.SetActive(true);
    }

    // === 페이드 효과 함수 ===

    private IEnumerator FadeToPanel(GameObject targetPanel, CanvasGroup targetCanvasGroup)
    {
        // 1단계: 현재 활성화된 패널 페이드아웃
        CanvasGroup currentCanvasGroup = null;

        if (loginMainPanel.activeSelf && loginMainPanel != targetPanel)
            currentCanvasGroup = loginMainCanvasGroup;
        else if (loginInputPanel.activeSelf && loginInputPanel != targetPanel)
            currentCanvasGroup = loginInputCanvasGroup;
        else if (signupInputPanel.activeSelf && signupInputPanel != targetPanel)
            currentCanvasGroup = signupInputCanvasGroup;

        if (currentCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                currentCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            currentCanvasGroup.alpha = 0f;
        }

        // 모든 패널 비활성화
        if (loginMainPanel) loginMainPanel.SetActive(false);
        if (loginInputPanel) loginInputPanel.SetActive(false);
        if (signupInputPanel) signupInputPanel.SetActive(false);

        // 2단계: 타겟 패널 활성화 및 페이드인
        if (targetPanel && targetCanvasGroup)
        {
            targetPanel.SetActive(true);
            targetCanvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                targetCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            targetCanvasGroup.alpha = 1f;
        }
    }

    // === 버튼 클릭 이벤트 핸들러들 ===

    private void OnLoginButtonClicked()
    {
        ShowLoginInput();
    }

    private void OnSignupButtonClicked()
    {
        ShowSignupInput();
    }

    private void OnLoginBackClicked()
    {
        ShowLoginMain();
    }

    private void OnSignupBackClicked()
    {
        ShowLoginMain();
    }

    private void OnLoginComplete()
    {
        if (showDebugLogs) Debug.Log("[AuthFlow] 로그인 완료");

        // 실제 인증은 없고 바로 홈으로 이동
        ShowHome();
    }

    private void OnSignupComplete()
    {
        if (showDebugLogs) Debug.Log("[AuthFlow] 회원가입 완료");

        // 실제 인증은 없고 바로 홈으로 이동
        ShowHome();
    }
}