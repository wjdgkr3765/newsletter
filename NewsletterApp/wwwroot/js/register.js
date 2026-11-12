// 회원가입 화면 (Register_screen.md)
(() => {
  const $ = (id) => document.getElementById(id);
  const loginId = $('register-login-id');
  const password = $('register-password');
  const passwordConfirm = $('register-password-confirm');
  const email = $('register-email');
  const name = $('register-name');
  const code = $('register-code');
  const sendCodeButton = $('register-send-code');
  const verifyButton = $('register-verify-code');
  const submit = $('register-submit');
  const codeArea = $('register-code-area');
  const codeTimer = $('register-code-timer');
  const codeCheck = $('register-code-check');
  const emailCheck = $('register-email-check');
  const errorBox = $('register-error');

  const state = { isLoginIdAvailable: false, isEmailAvailable: false, isVerified: false, stopTimer: null };

  const setCodeMessage = (message, isSuccess) => {
    codeCheck.textContent = message;
    codeCheck.className = `check-text mt-1 ${isSuccess ? 'text-success' : 'text-danger'}`;
  };

  const updateSubmit = () => {
    const isValid = state.isLoginIdAvailable
      && !Validation.password(password.value)
      && !Validation.passwordConfirm(passwordConfirm.value, password.value)
      && state.isEmailAvailable
      && state.isVerified
      && !Validation.userName(name.value);
    submit.disabled = !isValid;
  };

  const checkLoginIdAvailability = App.debounce(async (value) => {
    const result = await App.api('GET', `/api/auth/availability?loginId=${encodeURIComponent(value)}`);
    if (value !== loginId.value.trim()) {
      return;
    }
    state.isLoginIdAvailable = result.ok && result.data.available;
    Validation.setFieldState(loginId,
      result.ok ? (result.data.available ? '' : '이미 사용 중인 아이디입니다.') : result.error.message,
      '사용할 수 있는 아이디입니다.');
    updateSubmit();
  }, 400);

  const checkEmailAvailability = App.debounce(async (value) => {
    const result = await App.api('GET', `/api/auth/availability?email=${encodeURIComponent(value)}`);
    if (value !== email.value.trim()) {
      return;
    }
    state.isEmailAvailable = result.ok && result.data.available;
    Validation.setFieldState(email,
      result.ok ? (result.data.available ? '' : '이미 사용 중인 이메일입니다.') : result.error.message, '');
    sendCodeButton.disabled = !state.isEmailAvailable;
    updateSubmit();
  }, 400);

  loginId.addEventListener('input', () => {
    state.isLoginIdAvailable = false;
    const message = Validation.loginId(loginId.value);
    Validation.setFieldState(loginId, message);
    if (!message) {
      checkLoginIdAvailability(loginId.value.trim());
    }
    updateSubmit();
  });

  password.addEventListener('input', () => {
    Validation.setFieldState(password, Validation.password(password.value), '');
    if (passwordConfirm.value) {
      Validation.setFieldState(passwordConfirm, Validation.passwordConfirm(passwordConfirm.value, password.value), '');
    }
    updateSubmit();
  });

  passwordConfirm.addEventListener('input', () => {
    Validation.setFieldState(passwordConfirm, Validation.passwordConfirm(passwordConfirm.value, password.value), '');
    updateSubmit();
  });

  email.addEventListener('input', () => {
    state.isEmailAvailable = false;
    sendCodeButton.disabled = true;
    const message = Validation.email(email.value);
    Validation.setFieldState(email, message);
    if (!message) {
      checkEmailAvailability(email.value.trim());
    }
    updateSubmit();
  });

  name.addEventListener('input', () => {
    Validation.setFieldState(name, Validation.userName(name.value), '');
    updateSubmit();
  });

  code.addEventListener('input', () => {
    verifyButton.disabled = Validation.code(code.value) !== '' || state.isVerified;
  });

  sendCodeButton.addEventListener('click', async () => {
    App.setLoading(sendCodeButton, true);
    const result = await App.api('POST', '/api/auth/email-codes', { email: email.value.trim() });
    App.setLoading(sendCodeButton, false);
    if (!result.ok) {
      emailCheck.textContent = result.error.message;
      emailCheck.className = 'check-text text-danger mt-1';
      return;
    }
    sendCodeButton.textContent = '다시 받기';
    codeArea.hidden = false;
    code.value = '';
    code.focus();
    verifyButton.disabled = true;
    setCodeMessage('', true);
    if (state.stopTimer) {
      state.stopTimer();
    }
    state.stopTimer = App.startCodeTimer(codeTimer, result.data.expiresInSeconds, () => {
      setCodeMessage('인증 코드가 만료되었습니다. 다시 받아 주세요.', false);
      verifyButton.disabled = true;
    });
  });

  verifyButton.addEventListener('click', async () => {
    App.setLoading(verifyButton, true);
    const result = await App.api('POST', '/api/auth/email-codes/verify', { email: email.value.trim(), code: code.value });
    App.setLoading(verifyButton, false);
    if (!result.ok) {
      setCodeMessage(result.error.message, false);
      return;
    }
    state.isVerified = true;
    if (state.stopTimer) {
      state.stopTimer();
    }
    codeTimer.textContent = '';
    setCodeMessage('인증되었습니다.', true);
    email.readOnly = true;
    code.readOnly = true;
    sendCodeButton.disabled = true;
    verifyButton.disabled = true;
    updateSubmit();
  });

  submit.addEventListener('click', async () => {
    App.hideAlert(errorBox);
    App.setLoading(submit, true);
    const result = await App.api('POST', '/api/auth/register', {
      loginId: loginId.value.trim(),
      password: password.value,
      passwordConfirm: passwordConfirm.value,
      email: email.value.trim(),
      name: name.value.trim()
    });
    if (result.ok) {
      alert('가입되었습니다. 로그인해 주세요.');
      location.href = '/login';
      return;
    }
    App.setLoading(submit, false);
    App.showAlert(errorBox, 'danger', result.error.message);
    if (result.error.code === 'DUPLICATE_LOGIN_ID') {
      state.isLoginIdAvailable = false;
      Validation.setFieldState(loginId, result.error.message);
    }
    if (result.error.code === 'DUPLICATE_EMAIL') {
      Validation.setFieldState(email, result.error.message);
    }
    updateSubmit();
  });
})();
