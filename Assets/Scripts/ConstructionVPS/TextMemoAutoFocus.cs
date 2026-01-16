
// 패널이 켜질 때 키보드 활성화 및 텍스트 입력 필드에 포커스 주기
using System.Collections;
using UnityEngine;
using TMPro;

public class TextMemoAutoFocus : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;

    private void OnEnable()
    {
        // 패널 활성화 프레임에 UI 레이아웃이 아직 안 잡힌 경우가 있어 1프레임 대기 후 포커스
        StartCoroutine(FocusNextFrame());
    }

    private IEnumerator FocusNextFrame()
    {
        yield return null;

        if (!inputField) yield break;

        inputField.interactable = true;

        // 포커스 + 커서 활성화 (대부분의 안드로이드에서 여기서 키보드가 자동으로 뜸)
        inputField.Select();
        inputField.ActivateInputField();

        // 그래도 키보드가 안 뜨는 기기/설정 대비(옵션)
        if (TouchScreenKeyboard.isSupported && TouchScreenKeyboard.visible == false)
        {
            TouchScreenKeyboard.Open(
                inputField.text,
                TouchScreenKeyboardType.Default,
                autocorrection: true,
                multiline: inputField.lineType != TMP_InputField.LineType.SingleLine
            );
        }
    }
}
