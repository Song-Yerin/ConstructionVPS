
// 탭으로 AR 화면에 핀 생성, 맵별 파일(pins_{mapId}.json)형태로 저장/복원/삭제
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TabPinCreate : MonoBehaviour
{
    [Header("ARRaycastManager")]
    [Tooltip("ARRaycastManager 컴포넌트를 넣는 자리")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Pins Transform")]
    [Tooltip("Pins(핀 부모 오브젝트) Transform을 넣는 자리")]
    [SerializeField] private Transform pinsTransform;

    [Header("Pin Prefab")]
    [Tooltip("탭 시 만들 Pin Prefab을 넣는 자리")]
    [SerializeField] private GameObject pinPrefab;

    [Header("Map First Name")]
    [Tooltip("맵별로 저장될 파일 앞 이름 적는 자리")]
    [SerializeField] private string pinFilePrefix = "pins_";

    [Header("Map Id Source")]
    [Tooltip("mapId(PlayerPrefs의) 자동 세팅 사용 여부 체크")]
    [SerializeField] private bool useSelectedMapIdFromPrefs = true;

    [Tooltip("PlayerPrefs(임시 저장소)에서 읽을 키 이름")]
    [SerializeField] private string selectedMapIdPrefKey = "IMMERSAL_SELECTED_MAP_ID";

    [Header("Multimap Pin Number")]
    [Tooltip("여러 맵을 사용할 때 pin의 소속 번호(자동세팅 사용 시 무시될 수 있음)")]
    [SerializeField] private int pinMapId = 0;

    [Header("Pin Create Time Limit")]
    [Tooltip("Immersal TrackingAnalyzer 컴포넌트를 넣는 자리")]
    [SerializeField] private MonoBehaviour trackingAnalyzer;

    [SerializeField] private bool pinCreateTimeLimit = true;   // true면 정합 성공 전에는 핀 생성 금지
    [SerializeField] private int limitQuality = 1;             // 정합 성공 판단 퀄리티 기준

    [Header("Pin Restoration Timing")]
    [Tooltip("true면 앱 시작 시 정합된 뒤 복원, false면 바로 복원")]
    [SerializeField] private bool pinCreateAfterAlignment = true;

    [Header("Debug")]
    [Tooltip("디버그 로그를 자세히 찍을지 여부 체크")]
    [SerializeField] private bool verboseDebug = false;


    // pin 1개 저장 정보 구조
    [Serializable]              // Unity JsonUtility가 이 타입을 JSON으로 변환 가능하게 하기 위함
    public class PinData
    {
        public int pinMapId;           // 핀이 속한 맵 ID
        public Vector3 localPos;       // pinsTransform 기준 로컬 좌표
        public Quaternion localRot;    // pinsTransform 기준 로컬 회전
    }

    // pin 여러개 저장 정보 구조
    [Serializable]
    public class PinDB
    {
        public List<PinData> pins = new List<PinData>();
    }

    // raycast 결과 저장
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>(); // 메모리/GC 줄이기 위함

    // 현재 맵의 핀 DB 메모리
    private PinDB pinDB = new PinDB();

    // 복원 여부 체크
    private bool restorationOnce = false;
    private int loadedMapId = int.MinValue;

    // 현재 맵에 맞는 핀 저장 파일 경로를 만들기
    private string pinSavePath => Path.Combine(Application.persistentDataPath, $"{pinFilePrefix}{pinMapId}.json");


    // mapId 결정/로드/복원
    private void Awake()
    {
        // PlayerPrefs에서 mapId를 읽어 pinMapId를 결정
        ResolveMapId();
        // 현재 맵에 저장된 핀 목록을 메모리(pinDB)에 로드
        LoadPinsForCurrentMap();

        // 정합 후 복원 모드가 아니면 바로 복원
        if (!pinCreateAfterAlignment)
        {
            RestorePinsForThisMap();
            restorationOnce = true;
        }

        if (verboseDebug)
            Debug.Log($"[TabPinCreate] Awake mapId={pinMapId}, savePath={pinSavePath}, pinsLoaded={(pinDB?.pins?.Count ?? 0)}");
    }


    // 맵 변경 감지/복원, 탭 감지 시 핀 생성
    private void Update()
    {
        // PlayerPrefs의 pinMapId 변경 자동 감지 후 복원
        if (useSelectedMapIdFromPrefs)
        {
            int current = PlayerPrefs.GetInt(selectedMapIdPrefKey, pinMapId);
            if (current != pinMapId)
            {
                if (verboseDebug) Debug.Log($"[TabPinCreate] MapId changed {pinMapId} -> {current}");
                pinMapId = current;
                restorationOnce = false;
                LoadPinsForCurrentMap();

                if (!pinCreateAfterAlignment)
                {
                    RestorePinsForThisMap();
                    restorationOnce = true;
                }
            }
        }

        // 정합 상태 체크 후 복원
        if (pinCreateAfterAlignment && !restorationOnce && IsLocalizedEnough())
        {
            RestorePinsForThisMap();
            restorationOnce = true;
        }

        // 탭 감지 시 핀 생성 시도
        if (TryGetTapPosition(out Vector2 screenPos))
        {
            TryCreatePin(screenPos);
        }
    }

    // PlayerPrefs에서 mapId를 읽어 pinMapId를 결정 함수 (자동 세팅용)
    private void ResolveMapId()
    {
        if (!useSelectedMapIdFromPrefs) return;

        int id = PlayerPrefs.GetInt(selectedMapIdPrefKey, pinMapId);
        pinMapId = id;

        if (verboseDebug)
            Debug.Log($"[TabPinCreate] ResolveMapId prefKey={selectedMapIdPrefKey}, mapId={pinMapId}");
    }

    // 현재 맵의 핀 목록을 메모리(pinDB)에 로드 하는 함수
    private void LoadPinsForCurrentMap()
    {
        // 현재 mapId로 로드했는지 체크(중복 로드 방지)
        if (loadedMapId == pinMapId) return;
        loadedMapId = pinMapId;

        // 맵 바뀌면 메모 DB 자체를 교체
        pinDB = new PinDB();

        // 파일에서 로드 시도
        try
        {
            if (!File.Exists(pinSavePath))
            {
                if (verboseDebug) Debug.Log($"[TabPinCreate] No pin file for mapId={pinMapId}: {pinSavePath}");
                return;
            }

            string json = File.ReadAllText(pinSavePath);
            pinDB = JsonUtility.FromJson<PinDB>(json) ?? new PinDB();

            if (verboseDebug)
                Debug.Log($"[TabPinCreate] Loaded pins: mapId={pinMapId}, count={(pinDB?.pins?.Count ?? 0)} from {pinSavePath}");
        }
        catch (Exception e)
        {
            if (verboseDebug) Debug.LogWarning($"[TabPinCreate] LoadPins failed: {e}");
            pinDB = new PinDB();
        }
    }

    // 탭 판단과 2D 위치 전달 함수
    private bool TryGetTapPosition(out Vector2 screenPos) // 탭 위치 내보내기 위함
    {
        // screenPos 초기화
        screenPos = default;

        // 탭 판단 (멀티 탭 무시)
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            // UI 터치면 핀 생성 막기
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                return false;

            // 탭 판단 시 탭 위치 전달
            if (t.phase == TouchPhase.Began)
            {
                screenPos = t.position;
                return true;
            }
        }

        return false;
    }

    // AR레이캐스트 위치에 핀 생성과 DB 저장 (정합전 생성 제한 시 X)
    private void TryCreatePin(Vector2 screenPos)
    {
        // 핀 생성 조건 판단
        if (pinCreateTimeLimit && !IsLocalizedEnough()) return;
        if (raycastManager == null || pinsTransform == null || pinPrefab == null) return;

        // 평면이나 특징점 중 맞는 곳을 핀 위치로 쓰기
        TrackableType types = TrackableType.PlaneWithinInfinity | TrackableType.FeaturePoint;
        if (!raycastManager.Raycast(screenPos, hits, types)) return;
        Pose hitPose = hits[0].pose;            // 가장 가까운 hit 좌표에 핀 생성 위함

        // 부모 오브젝트 하위에 핀 생성
        GameObject pin = Instantiate(pinPrefab);
        pin.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
        pin.transform.SetParent(pinsTransform, worldPositionStays: true);

        // 핀을 현재 mapId로 저장
        PinData data = new PinData
        {
            pinMapId = pinMapId,                       // 현재 맵 ID
            localPos = pin.transform.localPosition,    // pinsTransform 기준 로컬 좌표
            localRot = pin.transform.localRotation     // pinsTransform 기준 로컬 회전
        };

        // 새 핀 데이터를 메모리 목록에 등록
        pinDB.pins.Add(data);
        SavePinsForCurrentMap();

        if (verboseDebug)
            Debug.Log($"[TabPinCreate] CreatePin mapId={pinMapId}, totalPins={pinDB.pins.Count}");
    }

    // 현재 맵의 핀들만 씬에 복원 하는 함수
    private void RestorePinsForThisMap()
    {
        if (pinsTransform == null || pinPrefab == null) return;

        // 기존 핀 제거 (뒤에서부터)
        ClearScenePins();

        // 현재 맵의 핀만 복원
        int restored = 0;                     // 복원된 핀 개수 세기 위함
        foreach (PinData p in pinDB.pins)
        {
            GameObject pin = Instantiate(pinPrefab, pinsTransform);  // 생성과 동시에 부모까지 지정하기 위함
            pin.transform.localPosition = p.localPos;
            pin.transform.localRotation = p.localRot;
            restored++;
        }

        if (verboseDebug)
            Debug.Log($"[TabPinCreate] RestorePins mapId={pinMapId}, restored={restored}, file={pinSavePath}");
    }

    // 현재 맵의 핀들만 삭제 하는 함수
    public void ClearPinsThisMap()
    {
        pinDB.pins.Clear();
        SavePinsForCurrentMap();   // 삭제된 상태 저장 위함
        RestorePinsForThisMap();   // 씬에서 핀들도 제거 위함 (동기화)
    }

    // 현재 맵의 핀들만 파일에 JSON으로 저장 하는 함수
    private void SavePinsForCurrentMap()
    {
        try
        {
            string json = JsonUtility.ToJson(pinDB, true);
            File.WriteAllText(pinSavePath, json);

            if (verboseDebug)
                Debug.Log($"[TabPinCreate] SavePins mapId={pinMapId}, count={pinDB.pins.Count}, path={pinSavePath}");
        }
        catch (Exception e)
        {
            if (verboseDebug) Debug.LogWarning($"[TabPinCreate] SavePins failed: {e}");
        }
    }

    // 모든 맵의 핀과 DB를 삭제 하는 함수
    public void ResetAllpinsAndAnchors()
    {
        // 핀 오브젝트 삭제
        ClearScenePins();

        // 메모리 안  DB 파일 삭제
        pinDB.pins.Clear();

        // 기기 저장소의 DB 파일 삭제
        DeletePinFileIfExists("[TabPinCreate] Delete pin file failed: ");

        restorationOnce = false;
    }

    // 정합 상태 판단 함수
    private bool IsLocalizedEnough()
    {
        // trackingAnalyzer 참조 여부 판단
        if (trackingAnalyzer == null) return false;

        try
        {
            // trackingAnalyzer 안에서 TrackingStatus값 꺼내기
            object trackingStatus = GetMemberValue(trackingAnalyzer, "TrackingStatus");
            if (trackingStatus == null)
            {
                if (verboseDebug) Debug.LogWarning("[TabPinCreate] TrackingStatus is null (trackingAnalyzer mismatch?)");
                return false;
            }

            // 정합 성공 횟수와 퀄리티 값 꺼내기 > 판단
            int succ = ToInt(GetMemberValue(trackingStatus, "LocalizationSuccessCount"));
            int qual = ToInt(GetMemberValue(trackingStatus, "TrackingQuality"));

            if (verboseDebug)
                Debug.Log($"[TabPinCreate] LocalizationSuccessCount={succ}, TrackingQuality={qual}, limitQuality={limitQuality}");

            return succ > 0 && qual >= limitQuality;
        }
        catch (Exception e)
        {
            if (verboseDebug) Debug.LogWarning($"[TabPinCreate] IsLocalizedEnough failed: {e}");
            return false;
        }
    }

    // Object의 Name의 변수/프로퍼티(값을 읽기/쓰기 시 처리 내용) 값 꺼내기 함수
    private static object GetMemberValue(object obj, string name)
    {
        if (obj == null) return null;

        Type t = obj.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        PropertyInfo p = t.GetProperty(name, flags);
        if (p != null) return p.GetValue(obj);

        FieldInfo f = t.GetField(name, flags);
        if (f != null) return f.GetValue(obj);

        return null;
    }

    // Object를 int로 변환하는 함수
    private static int ToInt(object v)
    {
        // Object 판단
        if (v == null) return 0;
        if (v is int i) return i;

        // enum을 int로 변환
        Type vt = v.GetType();
        if (vt.IsEnum) return (int)v;

        try { return Convert.ToInt32(v); }
        catch { return 0; }
    }

    // 현재 맵 파일/DB/씬 핀 초기화 함수
    public void ResetAllPins()
    {
        ResetAllpinsAndAnchors();
    }

    // 복원 상태 초기화 함수
    public void ResetRestorationState()
    {
        restorationOnce = false;
    }

    // (중복 제거용) 핀 오브젝트 삭제
    private void ClearScenePins()
    {
        if (pinsTransform != null)
        {
            for (int i = pinsTransform.childCount - 1; i >= 0; i--)
                Destroy(pinsTransform.GetChild(i).gameObject);
        }
    }

    // (중복 제거용) 현재 맵 파일 삭제
    private void DeletePinFileIfExists(string verbosePrefix)
    {
        try
        {
            if (File.Exists(pinSavePath))
                File.Delete(pinSavePath);
        }
        catch (Exception e)
        {
            if (verboseDebug) Debug.LogWarning($"{verbosePrefix}{e}");
        }
    }
}
