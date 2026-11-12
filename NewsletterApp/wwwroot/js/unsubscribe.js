// 수신 거부 화면 (Unsubscribe_screen.md). 버튼을 눌러야 해지된다.
(() => {
  const submit = document.getElementById('unsubscribe-submit');
  if (!submit) {
    return;
  }
  const errorBox = document.getElementById('unsubscribe-error');

  submit.addEventListener('click', async () => {
    App.hideAlert(errorBox);
    App.setLoading(submit, true);
    const result = await App.api('POST', '/api/unsubscribe', { token: submit.dataset.token });
    if (result.ok) {
      document.getElementById('unsubscribe-confirm').hidden = true;
      document.getElementById('unsubscribe-done-message').textContent =
        `[${result.data.gubunName}] 뉴스레터 수신을 거부했습니다. 다시 받으려면 로그인한 뒤 '내 구독'에서 켜 주세요.`;
      document.getElementById('unsubscribe-done').hidden = false;
      return;
    }
    if (result.error.code === 'INVALID_LINK') {
      document.getElementById('unsubscribe-confirm').hidden = true;
      document.getElementById('unsubscribe-invalid').hidden = false;
      return;
    }
    App.setLoading(submit, false);
    App.showAlert(errorBox, 'danger', result.error.message);
  });
})();
