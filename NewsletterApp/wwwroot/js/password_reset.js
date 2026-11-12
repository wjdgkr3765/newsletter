// 비밀번호 찾기 화면 (PasswordReset_screen.md)
(() => {
  const $ = (id) => document.getElementById(id);
  const loginId = $('reset-login-id');
  const email = $('reset-email');
  const code = $('reset-code');
  const newPassword = $('reset-new-password');
  const newPasswordConfirm = $('reset-new-password-confirm');
  const sendCodeButton = $('reset-send-code');
  const verifyButton = $('reset-verify-code');
  const submit = $('reset-submit');
  const codeArea = $('reset-code-area');
  const codeTimer = $('reset-code-timer');
  const codeCheck = $('reset-code-check');
  const step2 = $('reset-step2');
  const errorBox = $('reset-error');

  const state = { isVerified: false, stopTimer: null };

  const setCodeMessage = (message, isSuccess) => {
    codeCheck.textContent = message;
    codeCheck.className = `check-text mt-1 ${isSuccess ? 'text-success' : 'text-danger'}`;
  };

  const target = () => ({ loginId: loginId.value.trim(), email: email.value.trim() });

  const updateSendButton = () => {
    sendCodeButton.disabled = state.isVerified
      || Validation.loginId(loginId.value) !== '' || Validation.email(email.value) !== '';
  };

  const updateSubmit = () => {
    submit.disabled = Validation.password(newPassword.value) !== ''
      || Validation.passwordConfirm(newPasswordConfirm.value, newPassword.value) !== '';
  };

  loginId.addEventListener('input', () => {
    Validation.setFieldState(loginId, Validation.loginId(loginId.value));
    updateSendButton();
  });

  email.addEventListener('input', () => {
    Validation.setFieldState(email, Validation.email(email.value));
    updateSendButton();
  });

  code.addEventListener('input', () => {
    verifyButton.disabled = Validation.code(code.value) !== '' || state.isVerified;
  });

  newPassword.addEventListener('input', () => {
    Validation.setFieldState(newPassword, Validation.password(newPassword.value), '');
    if (newPasswordConfirm.value) {
      Validation.setFieldState(newPasswordConfirm,
        Validation.passwordConfirm(newPasswordConfirm.value, newPassword.value), '');
    }
    updateSubmit();
  });

  newPasswordConfirm.addEventListener('input', () => {
    Validation.setFieldState(newPasswordConfirm,
      Validation.passwordConfirm(newPasswordConfirm.value, newPassword.value), '');
    updateSubmit();
  });

  sendCodeButton.addEventListener('click', async () => {
    App.hideAlert(errorBox);
    App.setLoading(sendCodeButton, true);
    const result = await App.api('POST', '/api/auth/password-reset/codes', target());
    App.setLoading(sendCodeButton, false);
    if (!result.ok) {
      App.showAlert(errorBox, 'danger', result.error.message);
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
    App.hideAlert(errorBox);
    App.setLoading(verifyButton, true);
    const result = await App.api('POST', '/api/auth/password-reset/codes/verify', { ...target(), code: code.value });
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
    loginId.readOnly = true;
    email.readOnly = true;
    code.readOnly = true;
    sendCodeButton.disabled = true;
    verifyButton.disabled = true;
    step2.hidden = false;
    newPassword.focus();
  });

  submit.addEventListener('click', async () => {
    App.hideAlert(errorBox);
    App.setLoading(submit, true);
    const result = await App.api('POST', '/api/auth/password-reset', {
      ...target(),
      newPassword: newPassword.value,
      newPasswordConfirm: newPasswordConfirm.value
    });
    if (result.ok) {
      alert('비밀번호를 바꿨습니다. 로그인해 주세요.');
      location.href = result.data.redirectUrl;
      return;
    }
    App.setLoading(submit, false);
    App.showAlert(errorBox, 'danger', result.error.message);
  });
})();
