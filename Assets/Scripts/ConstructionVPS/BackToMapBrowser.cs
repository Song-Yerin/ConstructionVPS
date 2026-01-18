
// 뒤로가기 입력을 감지하면, 필요한 컴포넌트들을 정리한 뒤 MapBrowser 씬으로 전환
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;


public class BackToMapBrowser : MonoBehaviour
{
    [Header("Move Scene")]
    [Tooltip("뒤로가기 버튼을 탭 할 때 이동할 씬 이름을 적는 자리")]
    [SerializeField] private string mapBrowserSceneName = "MapBrowser";

    [Header("Reset Element")]
    [Tooltip("뒤로가기 버튼을 탭 할 때 리셋 할 컴포넌트를 넣는 자리")]
    [SerializeField] private ARSession arSession;

    [Tooltip("TabPinCreate코드의 상태 초기화 메서드 호출을 위해 TabPinCreate코드를 넣는 자리 ")]
    [SerializeField] private TabPinCreate tabPinCreate;

    [Tooltip("뒤로가기 직전에 멈추고 싶은 컴포넌트들을 넣는 자리")]
    [SerializeField] private MonoBehaviour[] disableOnBack;

    [Header("Android Back Key")]
    [Tooltip("스마트폰에서 뒤로가기 처리 여부 선택 버튼")]
    [SerializeField] private bool handleAndroidBackKey = true;

    // 매 프레임 뒤로가기 입력 감지 > 뒤로 가기 실행
    private void Update()
    {
        if (!handleAndroidBackKey) return;

        if (Input.GetKeyDown(KeyCode.Escape))  // 스마트폰 기본 뒤로가기 버튼도 감지하기 위함
            GoBack();
    }

    // 뒤로 가기 처리 함수
    public void GoBack()
    {
        Debug.Log($"[BackToMapBrowser] GoBack > LoadScene({mapBrowserSceneName})");

        // 뒤로가기 직전에 멈추고 싶은 것들 비활성화
        if (disableOnBack != null)
        {
            foreach (var mb in disableOnBack)
            {
                if (mb) mb.enabled = false;
            }
        }

        // AR 트래킹/세션 상태를 초기화
        if (arSession)
        {
            arSession.Reset();
            Debug.Log("[BackToMapBrowser] ARSession.Reset()");
        }

        // 핀/메모 복원 상태 초기화
        if (tabPinCreate)
        {
            tabPinCreate.ResetRestorationState();
            Debug.Log("[BackToMapBrowser] TabPinCreate.ResetRestorationState()");
        }

        // 씬 이동
        SceneManager.LoadScene(mapBrowserSceneName);
    }
}
