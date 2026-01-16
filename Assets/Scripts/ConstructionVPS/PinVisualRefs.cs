
// 핀 프리팹 안의 아이콘,툴팁,타이틀을 인스펙터에서 직접 연결해,확실하게 토글/텍스트 갱신을 하도록 참조
using TMPro;
using UnityEngine;

public class PinVisualRefs : MonoBehaviour
{
    [Header("Direct refs (assign in prefab)")]
    [Tooltip("아이콘만 보여주는 Canvas(또는 루트 오브젝트)")]
    public GameObject iconCanvas;

    [Tooltip("툴팁을 보여주는 Canvas(또는 루트 오브젝트)")]
    public GameObject tooltipCanvas;

    [Tooltip("TooltipCanvas 안의 타이틀 TMP_Text (없으면 TabPinCreate가 자동으로 찾음)")]
    public TMP_Text titleText;
}
