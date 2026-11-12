// 내 정보 화면 (Profile_screen.md)
(() => {
  const $ = (id) => document.getElementById(id);
  const content = $('profile-content');
  const loadingArea = $('profile-loading');
  const name = $('profile-name');
  const email = $('profile-email');
  const code = $('profile-code');
  const sendCodeButton = $('profile-send-code');
  const verifyButton = $('profile-verify-code');
  const saveButton = $('profile-save');
  const codeArea = $('profile-code-area');
  const codeTimer = $('profile-code-timer');
  const codeCheck = $('profile-code-check');
  const infoAlert = $('profile-info-alert');
  const currentPassword = $('profile-current-password');
  const newPassword = $('profile-new-password');
  const newPasswordConfirm = $('profile-new-password-confirm');
  const changePasswordButton = $('profile-change-password');
  const passwordAlert = $('profile-password-alert');
  const withdrawModal = new bootstrap.Modal($('withdraw-modal'));
  const withdrawPassword = $('withdraw-password');
  const withdrawSubmit = $('withdraw-submit');

  const state = { savedEmail: '', isVerified: false, stopTimer: null };

  const isEmailChanged = () => email.value.trim().toLowerCase() !== state.savedEmail;

  const setCodeMessage = (message, isSuccess) => {
    codeCheck.textContent = message;
    codeCheck.className = `check-text mt-1 ${isSuccess ? 'text-success' : 'text-danger'}`;
  };

  function resetEmailVerification() {
    state.isVerified = false;
    if (state.stopTimer) {
      state.stopTimer();
      state.stopTimer = null;
    }
    codeArea.hidden = true;
    code.value = '';
    code.readOnly = false;
    email.readOnly = false;
    codeTimer.textContent = '';
    setCodeMessage('', true);
    sendCodeButton.textContent = '인증 코드 받기';
  }

  function updateInfoControls() {
    const nameMessage = Validation.userName(name.value);
    const emailMessage = Validation.email(email.value);
    const isChanged = isEmailChanged();
    sendCodeButton.hidden = !isChanged || emailMessage !== '';
    sendCodeButton.disabled = state.isVerified;
    saveButton.disabled = nameMessage !== '' || emailMessage !== '' || (isChanged && !state.isVerified);
  }

  async function load() {
    App.renderLoading(loadingArea);
    const result = await App.api('GET', '/api/me');
    if (!result.ok) {
      App.renderLoadError(loadingArea, '불러오지 못했습니다.', load);
      return;
    }
    const me = result.data;
    loadingArea.replaceChildren();
    content.hidden = false;
    $('profile-login-id').textContent = me.loginId;
    $('profile-create-date').textContent = App.formatKstDate(me.createDate);
    name.value = me.name;
    email.value = me.email;
    state.savedEmail = me.email;
    $('profile-withdraw-user').hidden = me.isAdmin;
    $('profile-withdraw-admin').hidden = !me.isAdmin;
    updateInfoControls();
  }

  name.addEventListener('input', () => {
    Validation.setFieldState(name, Validation.userName(name.value));
    updateInfoControls();
  });

  email.addEventListener('input', () => {
    resetEmailVerification();
    Validation.setFieldState(email, Validation.email(email.value));
    updateInfoControls();
  });

  code.addEventListener('input', () => {
    verifyButton.disabled = Validation.code(code.value) !== '' || state.isVerified;
  });

  sendCodeButton.addEventListener('click', async () => {
    App.hideAlert(infoAlert);
    App.setLoading(sendCodeButton, true);
    const result = await App.api('POST', '/api/me/email-codes', { email: email.value.trim() });
    App.setLoading(sendCodeButton, false);
    if (!result.ok) {
      Validation.setFieldState(email, result.error.message);
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
    const result = await App.api('POST', '/api/me/email-codes/verify', { email: email.value.trim(), code: code.value });
    App.setLoading(verifyButton, false);
    if (!result.ok) {
      setCodeMessage(result.error.message, false);
      return;
    }
    state.isVerified = true;
    if (state.stopTimer) {
      state.stopTimer();
      state.stopTimer = null;
    }
    codeTimer.textContent = '';
    setCodeMessage('인증되었습니다.', true);
    email.readOnly = true;
    code.readOnly = true;
    verifyButton.disabled = true;
    updateInfoControls();
  });

  saveButton.addEventListener('click', async () => {
    App.hideAlert(infoAlert);
    App.setLoading(saveButton, true);
    const result = await App.api('POST', '/api/me/profile', { name: name.value.trim(), email: email.value.trim() });
    App.setLoading(saveButton, false);
    if (!result.ok) {
      App.showAlert(infoAlert, 'danger', result.error.message);
      updateInfoControls();
      return;
    }
    name.value = result.data.name;
    email.value = result.data.email;
    state.savedEmail = result.data.email;
    $('sidebar-user-name').textContent = result.data.name;
    resetEmailVerification();
    Validation.clearFieldState(name);
    Validation.clearFieldState(email);
    App.showAlert(infoAlert, 'success', '저장했습니다.', 5000);
    updateInfoControls();
  });

  newPassword.addEventListener('input', () => {
    Validation.setFieldState(newPassword, Validation.password(newPassword.value), '');
    if (newPasswordConfirm.value) {
      Validation.setFieldState(newPasswordConfirm,
        Validation.passwordConfirm(newPasswordConfirm.value, newPassword.value), '');
    }
  });

  newPasswordConfirm.addEventListener('input', () => {
    Validation.setFieldState(newPasswordConfirm,
      Validation.passwordConfirm(newPasswordConfirm.value, newPassword.value), '');
  });

  currentPassword.addEventListener('input', () => Validation.clearFieldState(currentPassword));

  changePasswordButton.addEventListener('click', async () => {
    App.hideAlert(passwordAlert);
    const currentMessage = currentPassword.value ? '' : '현재 비밀번호를 입력해 주세요.';
    const newMessage = Validation.password(newPassword.value);
    const confirmMessage = Validation.passwordConfirm(newPasswordConfirm.value, newPassword.value);
    Validation.setFieldState(currentPassword, currentMessage);
    Validation.setFieldState(newPassword, newMessage, '');
    Validation.setFieldState(newPasswordConfirm, confirmMessage, '');
    if (currentMessage || newMessage || confirmMessage) {
      return;
    }
    App.setLoading(changePasswordButton, true);
    const result = await App.api('POST', '/api/me/password', {
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
      newPasswordConfirm: newPasswordConfirm.value
    });
    App.setLoading(changePasswordButton, false);
    if (!result.ok) {
      if (result.error.code === 'WRONG_PASSWORD') {
        Validation.setFieldState(currentPassword, result.error.message);
        return;
      }
      App.showAlert(passwordAlert, 'danger', result.error.message);
      return;
    }
    [currentPassword, newPassword, newPasswordConfirm].forEach((input) => {
      input.value = '';
      Validation.clearFieldState(input);
    });
    App.showAlert(passwordAlert, 'success', '비밀번호를 바꿨습니다.', 5000);
  });

  $('profile-withdraw').addEventListener('click', () => {
    withdrawPassword.value = '';
    Validation.clearFieldState(withdrawPassword);
    withdrawModal.show();
  });

  withdrawSubmit.addEventListener('click', async () => {
    if (!withdrawPassword.value) {
      Validation.setFieldState(withdrawPassword, '비밀번호를 입력해 주세요.');
      return;
    }
    App.setLoading(withdrawSubmit, true);
    const result = await App.api('POST', '/api/me/withdraw', { password: withdrawPassword.value });
    if (result.ok) {
      alert('탈퇴했습니다.');
      location.href = result.data.redirectUrl;
      return;
    }
    App.setLoading(withdrawSubmit, false);
    Validation.setFieldState(withdrawPassword, result.error.message);
  });

  load();
})();
