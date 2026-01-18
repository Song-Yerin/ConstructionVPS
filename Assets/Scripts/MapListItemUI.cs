
// 맵 목록 리스트 UI프리팹의 텍스트 생성 및 저장
// MapBrowserManager코드의 AddItemToUI 메서드를 UI 프립팹에 연결
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class MapListItemUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("맵 이름이 표시될 텍스트 컴포넌트를 넣는 자리")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private Button button;

    // 생성된 프리팹에 저장되는 값
    private int _mapIdInt;            // 서버와의 맵 ID 비교 위함
    private string _mapIdStr;         // 씬 내 맵 ID 저장 위함
    private string _mapName;          // 씬 내 맵 이름 저장 위함

    // 클릭시 실행할 함수를 저장 - 인자 명시 없음/있음 두 가지 버전 지원 위함
    private Action _onClickSimple;
    private Action<string, string> _onClickWithStrings;

    // MapBrowserManager가 UI 프리팹에 데이터 넣을 때 호출하는 메서드
    // 클릭 함수 1 (인자 명시 없음)
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

    // 클릭 함수 2 (인자 명시 있음)
    public void Bind(string mapId, string mapName, Action<string, string> onClick)
    {
        _mapIdStr = mapId ?? "";
        _mapName = mapName ?? "";
        _onClickWithStrings = onClick;
        _onClickSimple = null;

        // int로 파싱 가능하면 저장
        if (!int.TryParse(_mapIdStr, out _mapIdInt))
            _mapIdInt = -1;

        // UI 텍스트 갱신 및 실행 동작 전달
        ApplyTexts(_mapName, _mapIdStr);
        WireButton(() =>
        {
            Debug.Log($"[MapListItemUI] Clicked (string) id={_mapIdStr}, name={_mapName}");
            _onClickWithStrings?.Invoke(_mapIdStr, _mapName);
        });
    }

    // UI 텍스트 갱신 함수
    private void ApplyTexts(string name, string idStr)
    {
        if (titleText) titleText.text = string.IsNullOrEmpty(name) ? "(Unnamed Map)" : name;
        if (subText) subText.text = $"Map ID: {idStr}";
    }

    // 버튼 클릭 동작 연결 함수
    private void WireButton(Action clickAction)
    {
        if (!button) button = GetComponent<Button>(); // 버튼 컴포넌트 자동 참조 위함

        if (!button) 
        {
            Debug.LogError("[MapListItemUI] Button reference is missing. Inspector에 Button을 넣거나 같은 오브젝트에 Button 컴포넌트를 추가하세요.");
            return;
        }

        // 클릭 리스너 초기화 후 새 동작 연결
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => clickAction?.Invoke());
    }
}
