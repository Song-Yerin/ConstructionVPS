
// Pin에 저장될 메모 데이터
using UnityEngine;

public class MemoData : MonoBehaviour
{
    [Header("Identity")]
    public string id;

    [Header("Text")]
    public string title;

    // TabPinCreate가 쓰는 필드명
    public string body;

    // 기존 코드가 content를 쓰는 경우 대비(호환용)
    public string content;

    private void OnValidate()
    {
        // 둘 중 하나만 쓰더라도 데이터가 엇갈리지 않게 동기화
        if (string.IsNullOrEmpty(body) && !string.IsNullOrEmpty(content))
            body = content;
        else if (string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(body))
            content = body;
    }
}
