using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class MapListItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private Button button;

    // 내부 저장값(디버그/추후 확장용)
    private int _mapIdInt;
    private string _mapIdStr;
    private string _mapName;

    private Action _onClickSimple;
    private Action<string, string> _onClickWithStrings;

    /// <summary>
    /// (권장) MapBrowserManager(수정본)과 호환되는 바인드:
    /// Bind(int id, string name, Action onClick)
    /// </summary>
    public void Bind(int mapId, string mapName, Action onClick)
    {
        _mapIdInt = mapId;
        _mapIdStr = mapId.ToString();
        _mapName = mapName ?? "";
        _onClickSimple = onClick;
        _onClickWithStrings = null;

        ApplyTexts(_mapName, _mapIdStr);
        WireButton(() =>
        {
            Debug.Log($"[MapListItemUI] Clicked (int) id={_mapIdInt}, name={_mapName}");
            _onClickSimple?.Invoke();
        });
    }

    /// <summary>
    /// (호환 유지) 기존 코드에서 호출하던 바인드:
    /// Bind(string id, string name, Action<string,string> onClick)
    /// </summary>
    public void Bind(string mapId, string mapName, Action<string, string> onClick)
    {
        _mapIdStr = mapId ?? "";
        _mapName = mapName ?? "";
        _onClickWithStrings = onClick;
        _onClickSimple = null;

        // int로 파싱 가능하면 저장(필수 아님)
        if (!int.TryParse(_mapIdStr, out _mapIdInt))
            _mapIdInt = -1;

        ApplyTexts(_mapName, _mapIdStr);
        WireButton(() =>
        {
            Debug.Log($"[MapListItemUI] Clicked (string) id={_mapIdStr}, name={_mapName}");
            _onClickWithStrings?.Invoke(_mapIdStr, _mapName);
        });
    }

    private void ApplyTexts(string name, string idStr)
    {
        if (titleText) titleText.text = string.IsNullOrEmpty(name) ? "(Unnamed Map)" : name;
        if (subText) subText.text = $"Map ID: {idStr}";
    }

    private void WireButton(Action clickAction)
    {
        if (!button) button = GetComponent<Button>();

        if (!button)
        {
            Debug.LogError("[MapListItemUI] Button reference is missing. Inspector에 Button을 넣거나 같은 오브젝트에 Button 컴포넌트를 추가하세요.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => clickAction?.Invoke());
    }
}
