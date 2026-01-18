
// 탭으로 AR 화면에 핀 생성, 맵별 파일(pins_{mapId}.json)형태로 저장/복원/삭제
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

public class TabPinCreate : MonoBehaviour
{
    [Header("Runtime Debug HUD (Mobile)")]
    [SerializeField] private TMP_Text debugHudText;
    [SerializeField] private bool showRuntimeDebugHud = true;


    [Header("Tooltip Debug (Temporary)")]
    [SerializeField] private bool debugForceTooltipInFrontOfCamera = false;

    [SerializeField] private float debugTooltipForwardMeters = 0.6f;


    [Header("Tooltip Visibility Fix")]
    [Tooltip("툴팁을 핀 위치에서 카메라쪽으로 살짝 당겨 Near Clip 잘림을 피함")]
    [SerializeField] private float tooltipPullTowardCamera = 0.12f;

    [Tooltip("툴팁을 약간 위로 올리는 오프셋(m)")]
    [SerializeField] private float tooltipUpOffset = 0.06f;

    [Tooltip("툴팁이 켜질 때 카메라를 바라보게 회전")]
    [SerializeField] private bool tooltipBillboardToCamera = true;

    [Header("Tooltip Render Fix (Layer/Sorting)")]
    [Tooltip("툴팁이 켜질 때 TooltipCanvas의 레이어를 핀 루트 레이어로 재귀 통일")]
    [SerializeField] private bool forceTooltipLayerToPinLayer = true;

    [Tooltip("툴팁 캔버스를 항상 위로 올려 가려짐을 줄임(overrideSorting)")]
    [SerializeField] private bool forceTooltipSorting = true;

    [Tooltip("forceTooltipSorting=true일 때 적용할 sortingOrder 값")]
    [SerializeField] private int tooltipSortingOrder = 5000;


    [Header("ARRaycastManager")]
    [Tooltip("ARRaycastManager 컴포넌트를 넣는 자리")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Pins Transform")]
    [Tooltip("Pins(핀 부모 오브젝트) Transform을 넣는 자리")]
    [SerializeField] private Transform pinsTransform;

    [Header("Pin Prefab")]
    [Tooltip("탭 시 만들 Pin Prefab을 넣는 자리")]
    [SerializeField] private GameObject pinPrefab;

    [Header("Memo UI (BottomBar Controller)")]
    [Tooltip("Canvas > SafeArea > MemoUI 에 붙어있는 MemoUIController를 넣는 자리")]
    [SerializeField] private MemoUIController memoUI;

    // 핀 선택(탭) 및 툴팁 거리표시
    [Header("AR Camera (Pin Select / Tooltip)")]
    [Tooltip("AR Camera를 넣는 자리 (핀 탭 선택 / 툴팁 거리표시에 사용)")]
    [SerializeField] private Camera arCamera;

    [Header("Pin Tap Select")]
    [Tooltip("Pin 프리팹에 설정한 Layer를 선택 (비워두면 모든 레이어에서 레이캐스트)")]
    [SerializeField] private LayerMask pinLayerMask;

    [Tooltip("핀 선택 Raycast 거리")]
    [SerializeField] private float pinRayDistance = 30f;

    // (추가) 아이콘/툴팁 표시 규칙을 TabPinCreate에서 직접 관리
    [Header("Icon / Tooltip Rule (Distance Based)")]
    [Tooltip("true면 TabPinCreate가 거리 기반으로 IconCanvas/TooltipCanvas를 직접 토글")]
    [SerializeField] private bool autoToggleIconTooltip = true;

    [Tooltip("카메라와 이 거리 이하면 툴팁(그리고 편집 가능), 멀면 아이콘")]
    [SerializeField] private float tooltipDistanceMeters = 1.2f;

    [Tooltip("핀 프리팹 내부에서 아이콘 캔버스 오브젝트 이름(기본: IconCanvas)")]
    [SerializeField] private string iconCanvasObjectName = "IconCanvas";

    [Tooltip("핀 프리팹 내부에서 툴팁 캔버스 오브젝트 이름(기본: TooltipCanvas)")]
    [SerializeField] private string tooltipCanvasObjectName = "TooltipCanvas";

    [Tooltip("TooltipCanvas 하위에서 타이틀 텍스트를 찾을 때, 우선으로 찾을 오브젝트 이름(비우면 첫 TMP_Text 사용)")]
    [SerializeField] private string tooltipTitleObjectName = ""; // 예: "TitleText"

    // ✅ (추가) PinVisualRefs 우선 사용 옵션
    [Header("Pin Visual Refs (Recommended)")]
    [Tooltip("PinVisualRefs가 프리팹에 있으면 이름 찾기보다 우선 사용")]
    [SerializeField] private bool preferPinVisualRefs = true;

    // (추가) “근처에는 새 핀 생성 금지”
    [Header("Create Block (Near Existing Pin)")]
    [Tooltip("이 거리(m) 안에 기존 핀이 있으면 새 핀 생성하지 않음")]
    [SerializeField] private float preventCreateNearDistance = 0.6f;

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

        // 메모 데이터 저장
        public string id;              // 핀/메모 고유 ID
        public string title;           // 텍스트 메모 타이틀
        public string body;            // 텍스트 메모 내용
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

    // 현재 선택된 핀 캐시 (편집 저장에 사용)
    private GameObject currentSelectedPin;

    // ✅ (추가) PinVisualRefs 캐시 (매 프레임 Find 비용 줄임)
    private readonly Dictionary<int, PinVisualRefs> pinVisualCache = new Dictionary<int, PinVisualRefs>();


    // mapId 결정/로드/복원
    private void Awake()
    {
        // 카메라 자동 채우기(안 넣었을 때 대비)
        if (!arCamera) arCamera = Camera.main;

        if (verboseDebug)
        {
            Debug.Log($"[TabPinCreate] Awake() start");
            Debug.Log($"[TabPinCreate] arCamera={(arCamera ? arCamera.name : "null")}");
            Debug.Log($"[TabPinCreate] raycastManager={(raycastManager ? raycastManager.name : "null")}, pinsTransform={(pinsTransform ? pinsTransform.name : "null")}, pinPrefab={(pinPrefab ? pinPrefab.name : "null")}");
            Debug.Log($"[TabPinCreate] pinCreateTimeLimit={pinCreateTimeLimit}, limitQuality={limitQuality}, pinCreateAfterAlignment={pinCreateAfterAlignment}");
            Debug.Log($"[TabPinCreate] pinLayerMask.value={pinLayerMask.value}, pinRayDistance={pinRayDistance}");
            Debug.Log($"[TabPinCreate] preferPinVisualRefs={preferPinVisualRefs}");
        }

        // PlayerPrefs에서 mapId를 읽어 pinMapId를 결정
        ResolveMapId();
        // 현재 맵에 저장된 핀 목록을 메모리(pinDB)에 로드
        LoadPinsForCurrentMap();

        // 정합 후 복원 모드가 아니면 바로 복원
        if (!pinCreateAfterAlignment)
        {
            if (verboseDebug) Debug.Log("[TabPinCreate] pinCreateAfterAlignment=false -> RestorePinsForThisMap() immediately");
            RestorePinsForThisMap();
            restorationOnce = true;
        }

        if (verboseDebug)
            Debug.Log($"[TabPinCreate] Awake mapId={pinMapId}, savePath={pinSavePath}, pinsLoaded={(pinDB?.pins?.Count ?? 0)}");
    }

    // (추가) 앱이 내려가거나 오브젝트가 비활성화될 때, 최신 DB를 파일에 한 번 더 저장(안전장치)
    private void OnDisable()
    {
        SavePinsForCurrentMap();
    }


    // 맵 변경 감지/복원, 탭 감지 시 핀 생성
    private void Update()
    {
        // UI가 열려있을 때는 핀 생성/선택 탭 로직을 아예 막는다 (입력필드 탭이 뺏기는 문제 해결)
        if (memoUI != null && memoUI.IsUIBlockingWorldInput())
        {
            // 그래도 아이콘/툴팁 자동 토글은 계속 돌리고 싶으면
            if (autoToggleIconTooltip) UpdatePinsIconTooltipByDistance();
            return;
        }

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
                    if (verboseDebug) Debug.Log("[TabPinCreate] pinCreateAfterAlignment=false -> RestorePinsForThisMap() after map change");
                    RestorePinsForThisMap();
                    restorationOnce = true;
                }
            }
        }

        // 정합 상태 체크 후 복원
        if (pinCreateAfterAlignment && !restorationOnce && IsLocalizedEnough())
        {
            if (verboseDebug) Debug.Log("[TabPinCreate] Localized enough -> RestorePinsForThisMap()");
            RestorePinsForThisMap();
            restorationOnce = true;
        }

        // (추가) 거리 기반으로 아이콘/툴팁 토글 + 타이틀 동기화
        if (autoToggleIconTooltip)
        {
            UpdatePinsIconTooltipByDistance();
        }

        // 탭 감지 시 핀 생성 시도
        if (TryGetTapPosition(out Vector2 screenPos))
        {
            if (verboseDebug) Debug.Log($"[TabPinCreate] Tap detected screenPos={screenPos}");

            // 먼저 핀을 탭했는지 확인(핀 탭이면 생성하지 않고 선택/편집 모드)
            bool selected = TrySelectExistingPin(screenPos);
            if (verboseDebug) Debug.Log($"[TabPinCreate] TrySelectExistingPin result={selected}");

            if (selected)
                return;

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

        // 캐시도 비움(맵 전환 시 기존 인스턴스들 무효)
        pinVisualCache.Clear();

        if (verboseDebug) Debug.Log($"[TabPinCreate] LoadPinsForCurrentMap() path={pinSavePath}");

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
            {
                if (verboseDebug) Debug.Log("[TabPinCreate] Tap ignored: pointer over UI");
                return false;
            }

            // 탭 판단 시 탭 위치 전달
            if (t.phase == TouchPhase.Began)
            {
                screenPos = t.position;
                return true;
            }
        }

        // 에디터/PC 환경에서 마우스로 테스트할 수 있게 지원
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            // UI 클릭이면 핀 생성 막기
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (verboseDebug) Debug.Log("[TabPinCreate] Tap ignored: pointer over UI (mouse)");
                return false;
            }

            screenPos = Input.mousePosition;
            return true;
        }
#endif

        return false;
    }

    // AR레이캐스트 위치에 핀 생성과 DB 저장 (정합전 생성 제한 시 X)
    private void TryCreatePin(Vector2 screenPos)
    {
        if (verboseDebug) Debug.Log($"[TabPinCreate] TryCreatePin start screenPos={screenPos}");

        // 핀 생성 조건 판단
        if (pinCreateTimeLimit && !IsLocalizedEnough())
        {
            if (verboseDebug) Debug.Log("[TabPinCreate] TryCreatePin blocked: not localized enough (pinCreateTimeLimit=true)");
            return;
        }

        if (raycastManager == null || pinsTransform == null || pinPrefab == null)
        {
            if (verboseDebug)
                Debug.LogWarning($"[TabPinCreate] TryCreatePin blocked: missing refs raycastManager={(raycastManager ? "OK" : "NULL")}, pinsTransform={(pinsTransform ? "OK" : "NULL")}, pinPrefab={(pinPrefab ? "OK" : "NULL")}");
            return;
        }

        // 평면이나 특징점 중 맞는 곳을 핀 위치로 쓰기
        TrackableType types = TrackableType.PlaneWithinInfinity | TrackableType.FeaturePoint;

        if (!raycastManager.Raycast(screenPos, hits, types))
        {
            if (verboseDebug) Debug.Log("[TabPinCreate] ARRaycast FAILED (no plane/feature hit)");
            return;
        }

        Pose hitPose = hits[0].pose;            // 가장 가까운 hit 좌표에 핀 생성 위함

        if (verboseDebug) Debug.Log($"[TabPinCreate] ARRaycast OK hitPose.position={hitPose.position} rot={hitPose.rotation.eulerAngles}");

        // (추가) 근처에 기존 핀이 있으면 새로 만들지 않기
        if (TryBlockCreateNearExisting(hitPose.position, out GameObject nearPin))
        {
            if (verboseDebug) Debug.Log("[TabPinCreate] Create blocked: near existing pin");

            // 근처 핀을 “선택” 처리(단, 편집 UI는 툴팁 거리에서만 열기)
            if (nearPin != null)
            {
                currentSelectedPin = nearPin;

                if (memoUI != null && arCamera != null)
                {
                    float camDist = Vector3.Distance(arCamera.transform.position, nearPin.transform.position);
                    if (camDist <= tooltipDistanceMeters)
                    {
                        memoUI.OnMemoSelected(nearPin); // 저장된 내용 로드해서 편집
                    }
                }
            }
            return;
        }

        // 부모 오브젝트 하위에 핀 생성
        GameObject pin = Instantiate(pinPrefab);
        if (verboseDebug) Debug.Log($"[TabPinCreate] Instantiate pin={pin.name} (activeSelf={pin.activeSelf})");

        pin.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
        pin.transform.SetParent(pinsTransform, worldPositionStays: true);

        if (verboseDebug)
        {
            Renderer r = pin.GetComponentInChildren<Renderer>(true);
            Canvas c = pin.GetComponentInChildren<Canvas>(true);
            Collider col = pin.GetComponentInChildren<Collider>(true);

            Debug.Log($"[TabPinCreate] Pin parent={pinsTransform.name}, worldPos={pin.transform.position}, localPos={pin.transform.localPosition}");
            Debug.Log($"[TabPinCreate] Pin components: Renderer={(r ? r.name : "null")}, Canvas={(c ? c.name : "null")}, Collider={(col ? col.name : "null")}");
            Debug.Log($"[TabPinCreate] Pin layer={LayerMask.LayerToName(pin.layer)}({pin.layer})");
        }

        // (추가) 메모 데이터 컴포넌트 보장 + 고유 ID 생성
        MemoData memo = pin.GetComponent<MemoData>();
        if (memo == null) memo = pin.AddComponent<MemoData>();

        memo.id = Guid.NewGuid().ToString("N");
        memo.title = "";
        memo.body = "";
        memo.content = memo.body; // 호환 유지

        if (verboseDebug) Debug.Log($"[TabPinCreate] MemoData assigned id={memo.id}");

        // 생성 직후는 아이콘만 보여야 함
        SetPinVisual(pin.transform, showIcon: true, showTooltip: false);

        // 툴팁 타이틀 텍스트도 동기화(지금은 빈 값)
        ApplyTooltipTitle(pin.transform, memo.title);

        // 현재 선택된 핀 갱신 (편집 대상)
        currentSelectedPin = pin;

        // 메모 부착(생성) 순간에 하단바를 띄우기
        if (memoUI != null)
        {
            memoUI.OnMemoPlaced(pin);
            if (verboseDebug) Debug.Log("[TabPinCreate] memoUI.OnMemoPlaced called");
        }
        else
        {
            if (verboseDebug)
                Debug.LogWarning("[TabPinCreate] memoUI is null. Assign MemoUIController in inspector.");
        }

        // 핀을 현재 mapId로 저장
        PinData data = new PinData
        {
            pinMapId = pinMapId,                       // 현재 맵 ID
            localPos = pin.transform.localPosition,    // pinsTransform 기준 로컬 좌표
            localRot = pin.transform.localRotation,    // pinsTransform 기준 로컬 회전

            // 메모 데이터 저장
            id = memo.id,
            title = memo.title,
            body = memo.body
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

        if (verboseDebug) Debug.Log("[TabPinCreate] RestorePinsForThisMap start");

        // 기존 핀 제거 (뒤에서부터)
        ClearScenePins();

        // 복원 시 캐시도 비움(씬 인스턴스 새로 만들어짐)
        pinVisualCache.Clear();

        bool needSaveBecauseMissingId = false;

        // 현재 맵의 핀만 복원
        int restored = 0;                     // 복원된 핀 개수 세기 위함
        foreach (PinData p in pinDB.pins)
        {
            // (추가) 파일 내부에 다른 mapId가 섞였을 때 대비: 현재 맵만 복원
            if (p.pinMapId != pinMapId) continue;

            GameObject pin = Instantiate(pinPrefab, pinsTransform);  // 생성과 동시에 부모까지 지정하기 위함
            pin.transform.localPosition = p.localPos;
            pin.transform.localRotation = p.localRot;

            if (verboseDebug)
                Debug.Log($"[TabPinCreate] Restore pin idx={restored} localPos={p.localPos} localRot={p.localRot.eulerAngles}");

            // 메모 데이터 복원
            MemoData memo = pin.GetComponent<MemoData>();
            if (memo == null) memo = pin.AddComponent<MemoData>();

            // 구버전 파일(id 없음) 대비
            if (string.IsNullOrWhiteSpace(p.id))
            {
                p.id = Guid.NewGuid().ToString("N");
                needSaveBecauseMissingId = true;
                if (verboseDebug) Debug.Log($"[TabPinCreate] Missing id found -> generated id={p.id}");
            }

            memo.id = p.id;
            memo.title = p.title ?? "";
            memo.body = p.body ?? "";
            memo.content = memo.body; // 호환 유지

            // 툴팁 타이틀 텍스트 동기화
            ApplyTooltipTitle(pin.transform, memo.title);

            // 복원 직후 현재 거리 기준으로 아이콘/툴팁 상태 세팅
            UpdateOnePinVisualByDistance(pin.transform, memo);

            restored++;
        }

        // id가 없던 데이터를 채웠으면 저장해서 다음부터 안정화
        if (needSaveBecauseMissingId)
        {
            if (verboseDebug) Debug.Log("[TabPinCreate] needSaveBecauseMissingId=true -> SavePinsForCurrentMap()");
            SavePinsForCurrentMap();
        }

        if (verboseDebug)
            Debug.Log($"[TabPinCreate] RestorePins mapId={pinMapId}, restored={restored}, file={pinSavePath}");
    }

    // 현재 맵의 핀들만 삭제 하는 함수
    public void ClearPinsThisMap()
    {
        if (verboseDebug) Debug.Log("[TabPinCreate] ClearPinsThisMap()");
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
        if (verboseDebug) Debug.Log("[TabPinCreate] ResetAllpinsAndAnchors()");

        // 핀 오브젝트 삭제
        ClearScenePins();

        // 메모리 안  DB 파일 삭제
        pinDB.pins.Clear();

        // "모든 맵" 의미에 맞게 persistentDataPath의 pins_*.json 전부 삭제
        DeleteAllPinFiles();

        restorationOnce = false;
        loadedMapId = int.MinValue;

        pinVisualCache.Clear();
    }

    // 정합 상태 판단 함수
    private bool IsLocalizedEnough()
    {
        // trackingAnalyzer 참조 여부 판단
        if (trackingAnalyzer == null)
        {
            if (verboseDebug) Debug.LogWarning("[TabPinCreate] IsLocalizedEnough: trackingAnalyzer is null");
            return false;
        }

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
            if (verboseDebug) Debug.Log($"[TabPinCreate] ClearScenePins childCount={pinsTransform.childCount}");
            for (int i = pinsTransform.childCount - 1; i >= 0; i--)
                Destroy(pinsTransform.GetChild(i).gameObject);
        }
        else
        {
            if (verboseDebug) Debug.LogWarning("[TabPinCreate] ClearScenePins: pinsTransform is null");
        }
    }

    // 모든 맵 파일 삭제(pins_*.json)
    private void DeleteAllPinFiles()
    {
        try
        {
            string dir = Application.persistentDataPath;
            if (!Directory.Exists(dir)) return;

            string pattern = $"{pinFilePrefix}*.json";
            string[] files = Directory.GetFiles(dir, pattern);

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    File.Delete(files[i]);
                    if (verboseDebug) Debug.Log($"[TabPinCreate] Deleted pin file: {files[i]}");
                }
                catch (Exception inner)
                {
                    if (verboseDebug) Debug.LogWarning($"[TabPinCreate] Delete file failed: {files[i]} / {inner}");
                }
            }
        }
        catch (Exception e)
        {
            if (verboseDebug) Debug.LogWarning($"[TabPinCreate] DeleteAllPinFiles failed: {e}");
        }
    }

    // 핀 탭 선택(버튼처럼 탭해서 다시 수정)
    private bool TrySelectExistingPin(Vector2 screenPos)
    {
        if (!arCamera)
        {
            if (verboseDebug) Debug.LogWarning("[TabPinCreate] TrySelectExistingPin: arCamera is null");
            return false;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);

        // pinLayerMask가 0이면(미설정) 마스크 없이 Raycast
        bool hitSomething;
        RaycastHit hit;

        if (pinLayerMask.value == 0)
            hitSomething = Physics.Raycast(ray, out hit, pinRayDistance);
        else
            hitSomething = Physics.Raycast(ray, out hit, pinRayDistance, pinLayerMask);

        if (verboseDebug)
        {
            Debug.Log($"[TabPinCreate] PinSelect Raycast hit={hitSomething} distMax={pinRayDistance} maskValue={pinLayerMask.value}");
            if (hitSomething)
                Debug.Log($"[TabPinCreate] PinSelect hit collider={hit.collider.name} hitObjLayer={LayerMask.LayerToName(hit.collider.gameObject.layer)}({hit.collider.gameObject.layer})");
        }

        if (!hitSomething) return false;

        MemoData memo = hit.collider.GetComponentInParent<MemoData>();
        if (memo == null)
        {
            if (verboseDebug) Debug.Log("[TabPinCreate] PinSelect hit but MemoData not found in parent");
            return false;
        }

        // (핀을 맞췄으면 무조건 선택으로 소비해서 새 핀 생성 루트로 못 가게 한다
        currentSelectedPin = memo.gameObject;

        // 툴팁 상태일 때만 탭 편집 가능 규칙 적용
        // 단, 여기서 return false를 하면 새 핀 생성으로 넘어가서 “빈 창” 문제가 다시 생김
        // 그래서 편집을 열지 못해도 return true로 소비한다
        float dist = Vector3.Distance(arCamera.transform.position, memo.transform.position);
        bool canEditNow = dist <= tooltipDistanceMeters;

        if (canEditNow && memoUI != null)
        {
            memoUI.OnMemoSelected(currentSelectedPin); // 저장된 값 로드 후 편집
            if (verboseDebug) Debug.Log("[TabPinCreate] memoUI.OnMemoSelected called");
        }
        else
        {
            if (verboseDebug) Debug.Log($"[TabPinCreate] PinSelect consumed but edit blocked (dist={dist:F2}, limit={tooltipDistanceMeters:F2})");
        }

        return true;
    }

    //  UI에서 호출: 특정 핀(id)의 텍스트 메모 저장(JSON 갱신)
    public void SaveTextMemoById(string id, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            if (verboseDebug) Debug.LogWarning("[TabPinCreate] SaveTextMemoById: id is null/empty");
            return;
        }

        for (int i = 0; i < pinDB.pins.Count; i++)
        {
            if (pinDB.pins[i].id == id)
            {
                pinDB.pins[i].title = title ?? "";
                pinDB.pins[i].body = body ?? "";
                if (verboseDebug) Debug.Log($"[TabPinCreate] SaveTextMemoById: updated id={id} titleLen={(pinDB.pins[i].title?.Length ?? 0)} bodyLen={(pinDB.pins[i].body?.Length ?? 0)}");
                SavePinsForCurrentMap();

                // DB만 바꾸면 씬에 떠있는 핀의 텍스트는 그대로일 수 있으므로, 씬 오브젝트도 함께 갱신
                UpdateScenePinMemo(id, title, body);

                return;
            }
        }

        if (verboseDebug) Debug.LogWarning($"[TabPinCreate] SaveTextMemoById: id not found in DB: {id}");
    }

    // 씬에 떠있는 핀(MemoData)도 같이 갱신해서 UI/툴팁 표시를 동기화
    private void UpdateScenePinMemo(string id, string title, string body)
    {
        if (pinsTransform == null) return;

        for (int i = 0; i < pinsTransform.childCount; i++)
        {
            Transform child = pinsTransform.GetChild(i);

            MemoData memo = child.GetComponentInChildren<MemoData>(true);
            if (memo == null) continue;

            if (memo.id != id) continue;

            memo.title = title ?? "";
            memo.body = body ?? "";
            memo.content = memo.body; // 호환 유지

            // 툴팁 타이틀 텍스트 동기화
            ApplyTooltipTitle(child, memo.title);

            // 저장 완료 후에는 거리 기반으로 아이콘/툴팁 상태가 즉시 반영되게 함
            UpdateOnePinVisualByDistance(child, memo);

            if (verboseDebug) Debug.Log($"[TabPinCreate] UpdateScenePinMemo applied to scene pin={child.name}");
            return;
        }
    }

    // 매 프레임: 모든 핀을 거리 기반으로 Icon/Tooltip 토글 + 타이틀 동기화
    private void UpdatePinsIconTooltipByDistance()
    {
        if (!arCamera || pinsTransform == null) return;

        for (int i = 0; i < pinsTransform.childCount; i++)
        {
            Transform pin = pinsTransform.GetChild(i);
            MemoData memo = pin.GetComponentInChildren<MemoData>(true);
            if (memo == null) continue;

            // 타이틀 동기화 (메모가 바뀌어도 자동 반영)
            ApplyTooltipTitle(pin, memo.title);

            UpdateOnePinVisualByDistance(pin, memo);
        }
    }
    // 핀 1개: 거리 기반 Icon/Tooltip 토글 규칙
    private void UpdateOnePinVisualByDistance(Transform pin, MemoData memo)
    {
        if (!arCamera || pin == null || memo == null) return;

        // (디버그 추가) 규칙이 실제로 돌고 있는지 / title / dist 확인
        if (verboseDebug)
        {
            float d = (arCamera ? Vector3.Distance(arCamera.transform.position, pin.position) : -1f);
            Debug.Log($"[TabPinCreate] VisualRule pin={pin.name} title='{memo.title}' dist={d:F2} limit={tooltipDistanceMeters:F2}");
        }

        // 작성 전: 아이콘만
        if (string.IsNullOrWhiteSpace(memo.title))
        {
            SetPinVisual(pin, showIcon: true, showTooltip: false);
            return;
        }

        float dist = Vector3.Distance(arCamera.transform.position, pin.position);
        bool showTooltip = dist <= tooltipDistanceMeters;

        // 가까우면 툴팁, 멀면 아이콘
        SetPinVisual(pin, showIcon: !showTooltip, showTooltip: showTooltip);

        WriteDebugHud(pin, memo, showTooltip);

    }

    // PinVisualRefs 찾기/캐시 (없으면 null)
    private PinVisualRefs GetPinVisualRefs(Transform pinRoot)
    {
        if (!preferPinVisualRefs || pinRoot == null) return null;

        int id = pinRoot.GetInstanceID();
        if (pinVisualCache.TryGetValue(id, out var cached) && cached != null)
            return cached;

        // 보통 루트에 붙이지만, 실수 대비해 children도 검색
        var refs = pinRoot.GetComponent<PinVisualRefs>();
        if (refs == null) refs = pinRoot.GetComponentInChildren<PinVisualRefs>(true);

        // 캐시 저장(없어도 저장해두면 매 프레임 GetComponent 비용 줄임)
        pinVisualCache[id] = refs;

        if (verboseDebug)
            Debug.Log($"[TabPinCreate] GetPinVisualRefs pin={pinRoot.name} found={(refs != null)}");

        return refs;
    }

    // 핀 프리팹 내부 IconCanvas/TooltipCanvas를 찾아 활성/비활성 처리
    private void SetPinVisual(Transform pinRoot, bool showIcon, bool showTooltip)
    {
        if (pinRoot == null) return;

        // 1순위: PinVisualRefs가 있으면 그걸 우선 사용
        GameObject iconGO = null;
        GameObject tipGO = null;
        Transform tipT = null;

        var refs = GetPinVisualRefs(pinRoot);
        if (refs != null)
        {
            iconGO = refs.iconCanvas;
            tipGO = refs.tooltipCanvas;
            tipT = (tipGO != null) ? tipGO.transform : null;
        }

        // 2순위: 없으면 이름으로 찾기(기존 방식)
        if (iconGO == null)
        {
            Transform iconTr = FindDeepChild(pinRoot, iconCanvasObjectName);
            if (iconTr != null) iconGO = iconTr.gameObject;
        }

        if (tipGO == null)
        {
            Transform tipTr = FindDeepChild(pinRoot, tooltipCanvasObjectName);
            if (tipTr != null)
            {
                tipGO = tipTr.gameObject;
                tipT = tipTr;
            }
        }

        // 빈 화면 방지: Tooltip을 못 찾았으면 아이콘은 유지
        if (showTooltip && tipGO == null)
        {
            if (iconGO != null) iconGO.SetActive(true);
            if (verboseDebug)
                Debug.LogWarning($"[TabPinCreate] TooltipCanvas를 찾지 못해 아이콘을 유지함. pinRoot={pinRoot.name}");
            return;
        }

        // 정상 토글
        if (iconGO != null) iconGO.SetActive(showIcon);
        if (tipGO != null) tipGO.SetActive(showTooltip);

        // Tooltip이 켜졌을 때만 “강제 표시 보정”
        if (!showTooltip || tipT == null) return;

        // (A) 다른 코드가 죽여놔도 다시 살리기: Canvas/CanvasGroup/Graphic
        {
            // Canvas 켜기
            var canv = tipT.GetComponent<Canvas>();
            if (canv != null) canv.enabled = true;

            // CanvasGroup이 있으면 alpha=1로 강제
            var cgs = tipT.GetComponentsInChildren<CanvasGroup>(true);
            for (int i = 0; i < cgs.Length; i++)
                cgs[i].alpha = 1f;

            // Graphic(Image/TMP 등) 강제 enable + alpha=1
            var graphics = tipT.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].enabled = true;
                var col = graphics[i].color;
                col.a = 1f;
                graphics[i].color = col;
            }

            var tmps = tipT.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                tmps[i].enabled = true;
                var col = tmps[i].color;
                col.a = 1f;
                tmps[i].color = col;
            }
        }

        // (B) World Space일 때만 위치/회전 보정
        var tipCanvas = tipT.GetComponent<Canvas>();
        bool isWorldSpace = (tipCanvas == null) || (tipCanvas.renderMode == RenderMode.WorldSpace);

        if (isWorldSpace && arCamera != null)
        {
            Vector3 camPos = arCamera.transform.position;

            // 핀 기준 위치(핀 루트 기준 + 약간 위)
            Vector3 basePos = pinRoot.position + Vector3.up * tooltipUpOffset;

            // 카메라 쪽으로 당기되, 카메라 near clip 안으로는 못 들어가게 clamp
            Vector3 dirToCam = camPos - basePos;
            float len = dirToCam.magnitude;

            if (len > 0.0001f)
            {
                Vector3 pullDir = dirToCam / len;

                float safeFromCam = Mathf.Max(arCamera.nearClipPlane + 0.06f, 0.10f);
                float pull = Mathf.Min(tooltipPullTowardCamera, Mathf.Max(0f, len - safeFromCam));

                tipT.position = basePos + pullDir * pull;
            }
            else
            {
                tipT.position = basePos;
            }

            // 빌보드 + 정면 뒤집힘 보정
            if (tooltipBillboardToCamera)
            {
                Vector3 toCam = camPos - tipT.position;
                if (toCam.sqrMagnitude > 0.0001f)
                {
                    // 1) 카메라를 향하게 회전
                    tipT.rotation = Quaternion.LookRotation(toCam, Vector3.up);

                    // 2) 만약 “정면이 반대”라면 180도 뒤집기
                    // (TMP/이미지 셰이더가 한쪽면만 그릴 때 안 보이는 문제 해결용)
                    float dot = Vector3.Dot(tipT.forward, toCam.normalized);
                    if (dot < 0f)
                        tipT.Rotate(0f, 180f, 0f, Space.Self);
                }
            }
        }
    }



    // TooltipCanvas 안의 TMP_Text에 타이틀 적용 (인스펙터 드래그 연결 없이도 동작)
    private void ApplyTooltipTitle(Transform pinRoot, string title)
    {
        if (pinRoot == null) return;

        // 1순위: PinVisualRefs.titleText 사용
        var refs = GetPinVisualRefs(pinRoot);
        if (refs != null && refs.titleText != null)
        {
            refs.titleText.enableWordWrapping = false;
            refs.titleText.overflowMode = TextOverflowModes.Ellipsis;

            string newText = title ?? "";
            if (refs.titleText.text != newText)
                refs.titleText.text = newText;

            return;
        }

        // 2순위: 기존 방식(TooltipCanvas 아래에서 TMP_Text 찾아 적용)
        Transform tipT = FindDeepChild(pinRoot, tooltipCanvasObjectName);
        if (tipT == null) return;

        TMP_Text target = null;

        // 이름 지정이 있으면 우선 찾기
        if (!string.IsNullOrWhiteSpace(tooltipTitleObjectName))
        {
            Transform t = FindDeepChild(tipT, tooltipTitleObjectName);
            if (t != null) target = t.GetComponent<TMP_Text>();
        }

        // 없으면 TooltipCanvas 아래에서 첫 TMP_Text 사용
        if (target == null)
            target = tipT.GetComponentInChildren<TMP_Text>(true);

        if (target != null)
        {
            // 한 줄 + ... 처리 (TMP 설정이 안 되어도 강제)
            target.enableWordWrapping = false;
            target.overflowMode = TextOverflowModes.Ellipsis;

            string newText = title ?? "";
            if (target.text != newText)
                target.text = newText;
        }
    }

    // 이름으로 자식 오브젝트를 재귀 탐색
    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrWhiteSpace(name)) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c.name == name) return c;

            Transform r = FindDeepChild(c, name);
            if (r != null) return r;
        }

        return null;
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (!root) return;
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }

    // 근처 생성 금지 판단
    private bool TryBlockCreateNearExisting(Vector3 worldPos, out GameObject nearPin)
    {
        nearPin = null;
        if (!pinsTransform) return false;
        if (preventCreateNearDistance <= 0f) return false;

        float best = preventCreateNearDistance;
        Transform bestT = null;

        for (int i = 0; i < pinsTransform.childCount; i++)
        {
            Transform t = pinsTransform.GetChild(i);
            float d = Vector3.Distance(worldPos, t.position);
            if (d <= best)
            {
                best = d;
                bestT = t;
            }
        }

        if (bestT != null)
        {
            nearPin = bestT.gameObject;
            return true;
        }

        return false;
    }


    // 툴팁이 "켜졌는데도 안 보이는" 케이스 강제 복구
    private void ForceMakeTooltipVisible(GameObject tipGO)
    {
        if (!tipGO) return;

        // Canvas 강제 활성 + 정렬 강제
        var canvas = tipGO.GetComponent<Canvas>();
        if (canvas)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000; // 충분히 크게
            canvas.worldCamera = arCamera; // World Space라도 지정해두면 안전
        }

        // CanvasRenderer가 cull 중이면 다시 풀기
        var renderers = tipGO.GetComponentsInChildren<CanvasRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i]) renderers[i].cull = false;
        }

        // Graphic(이미지/TMP 포함) 전부 enable + 알파 복구
        var graphics = tipGO.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            var g = graphics[i];
            if (!g) continue;
            g.enabled = true;

            var c = g.color;
            if (c.a < 0.99f) c.a = 1f;
            g.color = c;

            g.raycastTarget = false;
        }

        // 스케일이 너무 작으면 “보이는 수준”까지 임시로 키움
        var t = tipGO.transform;
        if (t.localScale.x < 0.001f) t.localScale = Vector3.one * 0.005f;
    }


    // 실제로 tooltip이 active 상태인지 확인 (모바일 로그 최소화용)
    private void LogTooltipStateOnce(Transform pinRoot, GameObject tipGO, bool showTooltip)
    {
        if (!verboseDebug) return;
        if (!pinRoot || !tipGO) { Debug.Log($"[TabPinCreate] TooltipState pin={pinRoot?.name} tipGO=null showTooltip={showTooltip}"); return; }
        Debug.Log($"[TabPinCreate] TooltipState pin={pinRoot.name} showTooltip={showTooltip} tipActiveSelf={tipGO.activeSelf} tipActiveInHierarchy={tipGO.activeInHierarchy} scale={tipGO.transform.localScale}");
    }

    private void WriteDebugHud(Transform pin, MemoData memo, bool wantTooltip)
    {
        if (!showRuntimeDebugHud || debugHudText == null || arCamera == null || pin == null) return;

        // refs 찾기
        var refs = GetPinVisualRefs(pin);

        bool iconActive = false;
        bool tipActive = false;

        if (refs != null)
        {
            if (refs.iconCanvas != null) iconActive = refs.iconCanvas.activeInHierarchy;
            if (refs.tooltipCanvas != null) tipActive = refs.tooltipCanvas.activeInHierarchy;
        }

        // 툴팁 오브젝트/캔버스 정보
        Transform tipT = null;
        Canvas tipCanvas = null;

        if (refs != null && refs.tooltipCanvas != null)
        {
            tipT = refs.tooltipCanvas.transform;
            tipCanvas = refs.tooltipCanvas.GetComponent<Canvas>();
        }

        float dist = Vector3.Distance(arCamera.transform.position, pin.position);

        float dot = -999f;
        if (tipT != null)
        {
            Vector3 toCam = (arCamera.transform.position - tipT.position).normalized;
            dot = Vector3.Dot(tipT.forward.normalized, toCam);
        }

        debugHudText.text =
            $"[PIN DEBUG]\n" +
            $"title='{(memo != null ? memo.title : "null")}'\n" +
            $"dist={dist:F2} / limit={tooltipDistanceMeters:F2}\n" +
            $"wantTooltip={wantTooltip}\n" +
            $"iconActive={iconActive}\n" +
            $"tipActive={tipActive}\n" +
            $"tipCanvas={(tipCanvas ? "YES" : "NO")}\n" +
            $"overrideSorting={(tipCanvas ? tipCanvas.overrideSorting.ToString() : "-")}\n" +
            $"sortingOrder={(tipCanvas ? tipCanvas.sortingOrder.ToString() : "-")}\n" +
            $"worldCamera={(tipCanvas && tipCanvas.worldCamera ? tipCanvas.worldCamera.name : "null")}\n" +
            $"dot(tipForward,toCam)={dot:F2}\n";
    }


}
