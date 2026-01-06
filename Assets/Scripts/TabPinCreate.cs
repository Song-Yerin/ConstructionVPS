using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using Unity.VisualScripting;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class TabPinCreate : MonoBehaviour
{
    [Header("Reset (Optional)")]
    [Tooltip("ARSession 오브젝트 넣는 자리")]
    [SerializeField] private ARSession arSession;

    [Header("AR Foundation")]
    [Tooltip("ARRaycastManager 컴포넌트를 넣는 자리")]
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Pins Transform")]
    [Tooltip("Pins Transform을 넣는 자리")]
    [SerializeField] private Transform pinsTransform;

    [Header("Pin Prefab")]
    [Tooltip("Pin Prefab을 넣는 자리")]
    [SerializeField] private GameObject pinPrefab;

    [Header("Save")]
    [Tooltip("저장할 pin들의 기본 이름을 적는 자리")]
    [SerializeField] private string pinBaseName = "pins.json";

    [Header("Multimap Pin Number")]
    [Tooltip("여러 맵을 사용할 때 pin의 소속 번호를 적는 자리")]
    [SerializeField] private int pinMapId = 0;

    [Header("Pin Create Time Limit")]
    [Tooltip("Immersal TrackingAnalyzer 컴포넌트를 넣는 자리")]
    [SerializeField] private MonoBehaviour trackingAnalyzer;
    [SerializeField] private bool pinCreateTimeLimit = true;              //true면 정합 성공 전에는 핀 생성 금지 위함
    [SerializeField] private int limitQuality = 1;                        //정합 성공 판단 퀄리티 판단 기준 설정 위함

    [Header("Pin Restoration Timing")]
    [Tooltip("true면 앱 시작 시 정합된 뒤 복원, false면 바로 복원")]
    [SerializeField] private bool pinCreateAfterAlignment = true;         //true면 정합 되기 전에는 핀 복원 금지 위함

    //pin 1개 저장 정보 구조
    [Serializable]
    public class PinData
    {
        public int pinMapId;
        public Vector3 localPos;
        public Quaternion localRot;
    }

    //pin 여러개 저장 정보 구조
    [Serializable]
    public class PinDB
    {
        public List<PinData> pins = new List<PinData>();
    }

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();                  //레이캐스트 히트 정보를 담기 위함
    private PinDB pinDB = new PinDB();                                                             //핀 데이터를 실제로 담기 위함
    private bool restorationOnce = false;                                                          //복원 1회 실행 위함
    private string pinSavePath => Path.Combine(Application.persistentDataPath, pinBaseName);       //핀 저장 경로 지정 위함

    private void Awake()
    {
        LoadPins();
        if (!pinCreateAfterAlignment)
        {
            RestorePinsForThisMap();
            restorationOnce = true;   //복원 1회 실행 위함
        }
    }

    // 종합 조건 판단 후 복원 및 핀 생성
    private void Update()
    {
        if (pinCreateAfterAlignment && !restorationOnce && IsLocalizedEnough())
        {
            RestorePinsForThisMap();
            restorationOnce = true;
        }
        if (TryGetTapPosition(out Vector2 screenPos))
        {
            TryCreatePin(screenPos);
        }
    }

    // 탭 판단과 2D 위치 전달
    private bool TryGetTapPosition(out Vector2 screenPos)
    {
        screenPos = default;

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            // UI 터치면 핀 생성 막기
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                return false;

            if (t.phase == TouchPhase.Began)
            {
                screenPos = t.position;
                return true;
            }
        }

        return false;
    }

    // 핀 생성과 저장
    private void TryCreatePin(Vector2 screenPos)
    {
        // 핀 생성 조건 판단
        if (pinCreateTimeLimit && !IsLocalizedEnough()) return;
        if (raycastManager == null || pinsTransform == null || pinPrefab == null) return;

        TrackableType types = TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint; // 타겟으로 평면과 특징점을 허용하기 위함

        if (!raycastManager.Raycast(screenPos, hits, types)) return;

        // 가장 가까운 hit 좌표에 핀 생성
        Pose hitPose = hits[0].pose;
        GameObject pin = Instantiate(pinPrefab);
        pin.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
        pin.transform.SetParent(pinsTransform, worldPositionStays: true);              //ARSpace의 자식으로 설정하되, 월드좌표는 고정하기 위함

        // 새로운 핀 데이터 저장
        PinData data = new PinData
        {
            pinMapId = pinMapId,
            localPos = pin.transform.localPosition,
            localRot = pin.transform.localRotation
        };
        pinDB.pins.Add(data);     //DB에 저장하기 위함
        SavePins();               //JSON 파일에 저장하기 위함
    }

    // 저장한 핀들을 씬에 복원함
    private void RestorePinsForThisMap()
    {
        // 핀 복원 조건 판단
        if (pinsTransform == null || pinPrefab == null) return;

        for (int i = pinsTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(pinsTransform.GetChild(i).gameObject);
        }

        foreach (PinData p in pinDB.pins)
        {
            if (p.pinMapId != pinMapId) continue;

            // 핀 복원
            GameObject pin = Instantiate(pinPrefab, pinsTransform);
            pin.transform.localPosition = p.localPos;
            pin.transform.localRotation = p.localRot;
        }
    }

    // 맵 핀 초기화
    public void ClearPinsThisMap()    // 실제 삭제하기 위함
    {
        pinDB.pins.RemoveAll(p => p.pinMapId == pinMapId);
        SavePins();                   // 삭제 결과를 파일에 반영하기 위함
        RestorePinsForThisMap();      // 삭제 결과를 씬에 반영하기 위함
    }

    // 핀 데이터 json 파일로 저장
    private void SavePins()
    {
        string json = JsonUtility.ToJson(pinDB, true);  // 핀 데이터를 json으로 변환하기 위함
        File.WriteAllText(pinSavePath, json);           // pinSavePath 위치에 json 파일로 저장하기 위함
    }

    // 제거 버튼 탭 시 전체 핑 + 저장 파일 초기화
    public void ResetAllpinsAndAnchors()
    {
        // 씬에 있는 핀 삭제
        if (pinsTransform != null)
        {
            for (int i = pinsTransform.childCount - 1; i >= 0; i--) 
            {
                Destroy(pinsTransform.GetChild(i).gameObject);
            }
        }

        // 저장된 핀 데이터 삭제
        pinDB.pins.Clear();


        // 핀 저장 파일 삭제
        if (File.Exists(pinSavePath))
        { 
          File.Delete(pinSavePath);
        }

        // 이후 복원 시도 가능하도록 상태 초기화
        restorationOnce = false;

    }

    // 저장 파일이 있는지 유무 판단 후 pinDB 채움
    private void LoadPins()
    {
        if (!File.Exists(pinSavePath))
        {
            pinDB = new PinDB();
            return;
        }

        //json 파일에서 pin 데이터 읽기
        string json = File.ReadAllText(pinSavePath);
        pinDB = JsonUtility.FromJson<PinDB>(json) ?? new PinDB();
    }



    // 정합 상태 판단
    private bool IsLocalizedEnough()
    {
        // 조건 판단
        if (trackingAnalyzer == null) return false;


        try
        {
            Type taType = trackingAnalyzer.GetType();
            var trackingStatusProp = taType.GetProperty("TrackingStatus");
            object trackingStatus = trackingStatusProp?.GetValue(trackingAnalyzer, null);
            if (trackingStatus == null) return false;
            Type tsType = trackingStatus.GetType();
            var succProp = tsType.GetProperty("LocalizationSuccessCount");    // 정합 성공 횟수 찾기 위함
            var qualProp = tsType.GetProperty("TrackingQuality");             // 정합 퀄리티 찾기 위함
            int succ = succProp != null ? (int)succProp.GetValue(trackingStatus, null) : 0;
            int qual = qualProp != null ? (int)qualProp.GetValue(trackingStatus, null) : 0;
            return succ > 0 && qual >= limitQuality;
        }
        catch
        {
            return false;
        }
    }

    // 핀 전체 초기화 (저장 파일까지 모두)
    public void ResetAllPins()
    {
        pinDB = new PinDB();

        // 저장 파일 삭제
        if (File.Exists(pinSavePath))
        {
            File.Delete(pinSavePath);
        }

        // 씬 내 핀 삭제
        if (pinsTransform != null)
        {
            for (int i = pinsTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(pinsTransform.GetChild(i).gameObject);
            }
        }
        restorationOnce = false;  // 다음 정합 타이밍에 복원 로직이 다시 돌 수 있게 하기 위함
    }
}
