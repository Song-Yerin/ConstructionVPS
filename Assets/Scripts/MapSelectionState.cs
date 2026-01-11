using UnityEngine;

[CreateAssetMenu(menuName = "App/State/Map Selection State", fileName = "MapSelectionState")]
public class MapSelectionState : ScriptableObject
{
    [Header("Selected Map")]
    [SerializeField] private string selectedMapId;
    [SerializeField] private string selectedMapName;

    [Header("Downloaded Local File Path")]
    [SerializeField] private string downloadedMapFilePath;

    public string SelectedMapId
    {
        get => selectedMapId;
        set => selectedMapId = value;
    }

    public string SelectedMapName
    {
        get => selectedMapName;
        set => selectedMapName = value;
    }

    // ✅ MapBrowserManager가 찾는 이름이 이거다 (대소문자 포함)
    public string DownloadedMapFilePath
    {
        get => downloadedMapFilePath;
        set => downloadedMapFilePath = value;
    }
}
