
// 핀에 저장될 메모 데이터(ID, 제목, 본문, 내용)를 담는 스크립트
using UnityEngine;

public class MemoData : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("메모의 고유 ID")]
    public string id;

    [Header("Text")]
    [Tooltip("메모의 제목")]
    public string title;

    [Tooltip("메모의 본문")]
    public string body;

    [Tooltip("메모의 내용 (본문과 동일)")]
    public string content; // 호환성 유지를 위함함

    // 본문과 내용 동기화 함수
    private void OnValidate()
    {

        if (string.IsNullOrEmpty(body) && !string.IsNullOrEmpty(content))
            body = content;
        else if (string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(body))
            content = body;
    }
}
