
// 메모 작성 완료 후 > 툴팁 타이틀 갱신
using TMPro;
using UnityEngine;

public class MemoEditorUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_InputField titleInput;
    [SerializeField] private TMP_InputField contentInput;

    private MemoPinView currentPin;

    private void Awake()
    {
        if (panelRoot) panelRoot.SetActive(false);
    }

    // 새로 만들 때도 편집 UI를 열 수 있게 공용으로 씀
    public void OpenForEdit(MemoPinView pin)
    {
        currentPin = pin;
        if (!currentPin || currentPin.Data == null) return;

        if (panelRoot) panelRoot.SetActive(true);

        titleInput.text = currentPin.Data.title;
        contentInput.text = currentPin.Data.content;
    }

    public void Close()
    {
        if (panelRoot) panelRoot.SetActive(false);
        currentPin = null;
    }

    // Save 버튼에 연결
    public void OnClickSave()
    {
        if (!currentPin || currentPin.Data == null) { Close(); return; }

        currentPin.SetTitle(titleInput.text);
        currentPin.SetContent(contentInput.text);

        // 저장(파일/JSON)까지 하고 있으면 여기서 Save 호출
        Close();
    }

    // Cancel 버튼에 연결
    public void OnClickCancel()
    {
        Close();
    }
}
