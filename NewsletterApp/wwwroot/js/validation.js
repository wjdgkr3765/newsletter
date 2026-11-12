// 공통 입력값 검사 (docs/api.md "입력값 규칙"). 화면 편의용이며 서버 검증을 대신하지 않는다.
// 각 함수는 오류 문구를 돌려주고, 통과하면 빈 문자열을 돌려준다.
const Validation = (() => {
  const LOGIN_ID = /^[a-z0-9]{6,20}$/;
  const EMAIL = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
  const CODE = /^[0-9]{6}$/;

  function loginId(value) {
    return LOGIN_ID.test(value.trim()) ? '' : '영문 소문자와 숫자로 6~20자 입력해 주세요.';
  }

  function password(value) {
    const isValid = value.length >= 8 && value.length <= 20
      && /[A-Za-z]/.test(value) && /[0-9]/.test(value) && /[!-/:-@[-`{-~]/.test(value)
      && /^[!-~]+$/.test(value);
    return isValid ? '' : '8~20자, 영문·숫자·특수문자를 각 1자 이상 넣어 주세요.';
  }

  function passwordConfirm(value, original) {
    return value === original && value.length > 0 ? '' : '비밀번호가 같지 않습니다.';
  }

  function email(value) {
    const trimmed = value.trim();
    return trimmed.length <= 50 && EMAIL.test(trimmed) ? '' : '올바른 이메일을 50자 이내로 입력해 주세요.';
  }

  function userName(value) {
    const length = value.trim().length;
    return length >= 2 && length <= 30 ? '' : '이름을 2~30자로 입력해 주세요.';
  }

  function gubunName(value) {
    const length = value.trim().length;
    return length >= 1 && length <= 20 ? '' : '종류 이름을 1~20자로 입력해 주세요.';
  }

  function code(value) {
    return CODE.test(value) ? '' : '숫자 6자리를 입력해 주세요.';
  }

  // 입력 칸 상태 표시: 오류면 빨간 테두리와 문구, 통과면 초록 테두리와 (있으면) 통과 문구
  function setFieldState(input, message, validMessage) {
    const check = document.getElementById(`${input.id}-check`);
    input.classList.toggle('is-invalid', message.length > 0);
    input.classList.toggle('is-valid', message.length === 0 && validMessage !== undefined);
    if (!check) {
      return;
    }
    if (message.length > 0) {
      check.textContent = message;
      check.className = 'check-text text-danger mt-1';
    } else if (validMessage) {
      check.textContent = validMessage;
      check.className = 'check-text text-success mt-1';
    } else {
      check.textContent = check.dataset.hint || '';
      check.className = 'check-text form-text mt-1';
    }
  }

  function clearFieldState(input) {
    input.classList.remove('is-invalid', 'is-valid');
    const check = document.getElementById(`${input.id}-check`);
    if (check) {
      check.textContent = check.dataset.hint || '';
      check.className = 'check-text form-text mt-1';
    }
  }

  return { loginId, password, passwordConfirm, email, userName, gubunName, code, setFieldState, clearFieldState };
})();
