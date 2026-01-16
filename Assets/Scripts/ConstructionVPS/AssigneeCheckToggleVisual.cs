
// 탭 시 토글의 박스와 아이콘 색상 변환
using UnityEngine;
using UnityEngine.UI;

public class AssigneeCheckToggleVisual : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Toggle toggle;

    [Tooltip("체크박스 배경(박스) Image")]
    [SerializeField] private Image boxImage;

    [Tooltip("체크 아이콘 Image")]
    [SerializeField] private Image checkIconImage;

    [Header("Colors")]
    [Tooltip("기본(OFF) 박스 색상")]
    [SerializeField] private Color boxOffColor = Color.white;

    [Tooltip("기본(OFF) 아이콘 색상(검정)")]
    [SerializeField] private Color iconOffColor = Color.black;

    [Tooltip("ON일 때 박스 색상(하늘색)")]
    [SerializeField] private Color boxOnColor = new Color(0.45f, 0.80f, 1.00f, 1.00f);

    [Tooltip("ON일 때 아이콘 색상(흰색)")]
    [SerializeField] private Color iconOnColor = Color.white;

    private void Reset()
    {
        toggle = GetComponent<Toggle>();
        if (toggle)
        {
            // 흔한 기본 구조를 최대한 자동으로 찾아줌
            var bg = toggle.transform.Find("Background");
            if (bg) boxImage = bg.GetComponent<Image>();
            var ck = toggle.transform.Find("Background/Checkmark");
            if (ck) checkIconImage = ck.GetComponent<Image>();
        }
    }

    private void Awake()
    {
        if (!toggle) toggle = GetComponent<Toggle>();
        if (toggle)
        {
            toggle.onValueChanged.RemoveListener(Apply);
            toggle.onValueChanged.AddListener(Apply);
        }

        Apply(toggle != null && toggle.isOn);
    }

    private void Apply(bool isOn)
    {
        if (boxImage) boxImage.color = isOn ? boxOnColor : boxOffColor;
        if (checkIconImage)
        {
            checkIconImage.color = isOn ? iconOnColor : iconOffColor;

            // OFF 상태에서도 "검은 체크 아이콘이 안에 있다" 요구사항 반영:
            // 체크마크 자체를 끄지 않고 항상 보여준다.
            checkIconImage.enabled = true;
        }
    }
}
