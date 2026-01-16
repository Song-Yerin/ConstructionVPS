
// TooltipCanvas를 강제로 보이게 하기 위해 설정을 강제
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipOverlayForceDebug : MonoBehaviour
{
    private void Awake()
    {
        // Canvas/RectTransform 강제 정상화
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition3D = Vector3.zero;
        }

        var c = GetComponent<Canvas>();
        if (c != null)
        {
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 5000;
        }

        // 자식도 강제로 켬 + 알파 보정
        gameObject.SetActive(true);

        foreach (var cg in GetComponentsInChildren<CanvasGroup>(true))
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        foreach (var g in GetComponentsInChildren<Graphic>(true))
        {
            var col = g.color;
            col.a = 1f;
            g.color = col;
            g.enabled = true;
        }

        foreach (var t in GetComponentsInChildren<TMP_Text>(true))
        {
            if (string.IsNullOrEmpty(t.text)) t.text = "TEST";
            t.enabled = true;
        }
    }
}
