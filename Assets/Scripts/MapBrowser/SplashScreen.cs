
// Splash화면 애니메이션 스크립트
using System.Collections;
using UnityEngine;

public class SplashScreen : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Splash Screen CanvasGroup 넣는 자리")]
    [SerializeField] private CanvasGroup splashCanvasGroup;

    [Header("Timing")]
    [Tooltip("페이드인 시간 (0이면 바로 표시)")]
    [SerializeField] private float fadeInDuration = 0.3f;

    [Tooltip("유지 시간 (완전히 보이는 시간)")]
    [SerializeField] private float displayDuration = 2f;

    [Tooltip("페이드아웃 시간")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Next Screen")]
    [Tooltip("Splash 종료 후 활성화할 Canvas (AuthCanvas)")]
    [SerializeField] private Canvas authCanvas;

    [Header("Debug")]
    [Tooltip("디버그 로그 출력")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        // 초기 설정: 시작 시 투명하게
        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.alpha = 0f;
            splashCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            Debug.LogError("[SplashScreen] CanvasGroup이 연결되지 않았습니다!");
        }

        // 인증 화면은 초기에 숨김
        if (authCanvas != null)
        {
            authCanvas.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (splashCanvasGroup == null)
        {
            Debug.LogError("[SplashScreen] CanvasGroup이 null입니다. 실행을 중단합니다.");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"[SplashScreen] 시작 - 페이드인:{fadeInDuration}s, 유지:{displayDuration}s, 페이드아웃:{fadeOutDuration}s");

        StartCoroutine(ShowSplash());
    }

    private IEnumerator ShowSplash()
    {
        // 1단계: 페이드인 (0 → 1)
        if (fadeInDuration > 0f)
        {
            if (showDebugLogs)
                Debug.Log("[SplashScreen] 페이드인 시작");

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                splashCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
        }

        // 완전히 보이게
        splashCanvasGroup.alpha = 1f;

        if (showDebugLogs)
            Debug.Log($"[SplashScreen] 유지 시작 ({displayDuration}초)");

        // 2단계: 유지 시간
        yield return new WaitForSeconds(displayDuration);

        // 3단계: 페이드아웃 (1 → 0)
        if (showDebugLogs)
            Debug.Log("[SplashScreen] 페이드아웃 시작");

        if (fadeOutDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                splashCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }
        }

        // 완전히 사라진 후 오브젝트 비활성화
        splashCanvasGroup.alpha = 0f;
        splashCanvasGroup.blocksRaycasts = false;

        if (showDebugLogs)
            Debug.Log("[SplashScreen] Splash 종료");

        // 인증 화면 활성화
        if (authCanvas != null)
        {
            authCanvas.gameObject.SetActive(true);
            if (showDebugLogs)
                Debug.Log("[SplashScreen] 인증 화면 활성화");
        }

        gameObject.SetActive(false);
    }
}