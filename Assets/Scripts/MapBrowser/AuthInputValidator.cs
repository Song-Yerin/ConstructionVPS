// 로그인/회원가입 입력 검증 스크립트
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuthInputValidator : MonoBehaviour
{
    [Header("Input Fields")]
    [Tooltip("이메일 입력 필드")]
    [SerializeField] private TMP_InputField emailInput;

    [Tooltip("비밀번호 입력 필드")]
    [SerializeField] private TMP_InputField passwordInput;

    [Tooltip("비밀번호 확인 입력 필드 (회원가입 전용)")]
    [SerializeField] private TMP_InputField passwordConfirmInput;

    [Header("Complete Button")]
    [Tooltip("완료 버튼")]
    [SerializeField] private Button completeButton;

    [Header("Auto Focus")]
    [Tooltip("화면 활성화 시 자동으로 이메일 필드에 포커스")]
    [SerializeField] private bool autoFocusOnEnable = true;

    private void Awake()
    {
        // 입력 변화 감지
        if (emailInput) emailInput.onValueChanged.AddListener(OnInputChanged);
        if (passwordInput) passwordInput.onValueChanged.AddListener(OnInputChanged);
        if (passwordConfirmInput) passwordConfirmInput.onValueChanged.AddListener(OnInputChanged);

        // Enter 키 처리
        if (emailInput) emailInput.onSubmit.AddListener(_ => OnSubmitEmail());
        if (passwordInput) passwordInput.onSubmit.AddListener(_ => OnSubmitPassword());
        if (passwordConfirmInput) passwordConfirmInput.onSubmit.AddListener(_ => OnSubmitPasswordConfirm());

        // 초기 상태 확인
        OnInputChanged("");
    }

    private void OnEnable()
    {
        // 패널이 활성화될 때 자동으로 이메일 필드에 포커스
        if (autoFocusOnEnable && emailInput != null)
        {
            emailInput.Select();
            emailInput.ActivateInputField();
        }
    }

    private void OnDestroy()
    {
        if (emailInput)
        {
            emailInput.onValueChanged.RemoveListener(OnInputChanged);
            emailInput.onSubmit.RemoveListener(_ => OnSubmitEmail());
        }
        if (passwordInput)
        {
            passwordInput.onValueChanged.RemoveListener(OnInputChanged);
            passwordInput.onSubmit.RemoveListener(_ => OnSubmitPassword());
        }
        if (passwordConfirmInput)
        {
            passwordConfirmInput.onValueChanged.RemoveListener(OnInputChanged);
            passwordConfirmInput.onSubmit.RemoveListener(_ => OnSubmitPasswordConfirm());
        }
    }

    private void Update()
    {
        // Enter 키로 완료 버튼 실행
        if (Input.GetKeyDown(KeyCode.Return) && completeButton != null && completeButton.interactable)
        {
            completeButton.onClick.Invoke();
        }
    }

    private void OnInputChanged(string value)
    {
        bool isValid = ValidateInputs();

        if (completeButton)
            completeButton.interactable = isValid;
    }

    // Enter 키 입력 시 다음 필드로 이동
    private void OnSubmitEmail()
    {
        if (passwordInput != null)
        {
            passwordInput.Select();
            passwordInput.ActivateInputField();
        }
    }

    private void OnSubmitPassword()
    {
        // 회원가입 화면이면 비밀번호 확인으로 이동
        if (passwordConfirmInput != null && passwordConfirmInput.gameObject.activeInHierarchy)
        {
            passwordConfirmInput.Select();
            passwordConfirmInput.ActivateInputField();
        }
        // 로그인 화면이면 완료 버튼 실행
        else if (completeButton != null && completeButton.interactable)
        {
            completeButton.onClick.Invoke();
        }
    }

    private void OnSubmitPasswordConfirm()
    {
        // 완료 버튼 실행
        if (completeButton != null && completeButton.interactable)
        {
            completeButton.onClick.Invoke();
        }
    }

    private bool ValidateInputs()
    {
        // 이메일과 비밀번호는 필수
        bool hasEmail = emailInput != null && !string.IsNullOrWhiteSpace(emailInput.text);
        bool hasPassword = passwordInput != null && !string.IsNullOrWhiteSpace(passwordInput.text);

        // 회원가입인 경우 비밀번호 확인도 체크
        bool hasPasswordConfirm = true;
        if (passwordConfirmInput != null && passwordConfirmInput.gameObject.activeInHierarchy)
        {
            hasPasswordConfirm = !string.IsNullOrWhiteSpace(passwordConfirmInput.text);
        }

        return hasEmail && hasPassword && hasPasswordConfirm;
    }
}