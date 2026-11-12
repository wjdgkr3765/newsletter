// 로그인 화면 (Login_screen.md)
(() => {
  const form = document.getElementById('login-form');
  const loginId = document.getElementById('login-id');
  const password = document.getElementById('login-password');
  const submit = document.getElementById('login-submit');
  const errorBox = document.getElementById('login-error');

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    App.hideAlert(errorBox);
    const idMessage = loginId.value.trim().length === 0 ? '아이디를 입력해 주세요.' : '';
    const passwordMessage = password.value.length === 0 ? '비밀번호를 입력해 주세요.' : '';
    Validation.setFieldState(loginId, idMessage);
    Validation.setFieldState(password, passwordMessage);
    if (idMessage || passwordMessage) {
      return;
    }
    App.setLoading(submit, true, '로그인 중');
    const result = await App.api('POST', '/api/auth/login', { loginId: loginId.value.trim(), password: password.value });
    if (result.ok) {
      location.href = result.data.redirectUrl;
      return;
    }
    App.setLoading(submit, false);
    App.showAlert(errorBox, 'danger', result.error.message);
    if (result.error.code === 'LOGIN_FAILED') {
      password.value = '';
    }
  });

  [loginId, password].forEach((input) => input.addEventListener('input', () => Validation.clearFieldState(input)));
})();
