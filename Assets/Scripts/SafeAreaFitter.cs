
// UI 상단 Safe Area 확보 
using UnityEngine;

public class SafeAreaFitter : MonoBehaviour
{
    // 화면 데이터 저장 장소
    private RectTransform rt;                     
    private Rect lastSafeArea;                    
    private ScreenOrientation lastOrientation;    


    // 초기 Safe Area 적용
    private void Awake()
    {
        rt = GetComponent<RectTransform>();       
        Apply();
    }

    // 변화 감지 > Safe Area 적용
    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.orientation != lastOrientation)
            Apply();
    }

    // Safe Area 적용 함수
    private void Apply()
    {
        // 기기의 Safe Area 가져오기
        Rect sa = Screen.safeArea;        // OS에서 제공하는 Safe Area 정보 얻기 위함

        // Safe Area 비교 기준 만들기
        lastSafeArea = sa;
        lastOrientation = Screen.orientation;

        // Safe Area 픽셀 좌표 얻기
        Vector2 anchorMin = sa.position;
        Vector2 anchorMax = sa.position + sa.size;

        // 앵커 % 값 얻기
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Safe Area 설정
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;

        // 유니티에서 자동으로 만드는 오프셋 제거
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
