// 발송 설정 상세 화면 (GubunDetail_screen.md)
(() => {
  const $ = (id) => document.getElementById(id);
  const gubunId = Number($('gubun-detail').dataset.gubunId);
  const DEFAULT_SEND_TIME = '16:00';
  const hour = $('config-hour');
  const minute = $('config-minute');
  const isUse = $('config-is-use');
  const isUseLabel = $('config-is-use-label');
  const saveButton = $('config-save');
  const configAlert = $('config-alert');
  const configLoading = $('config-loading');
  const configForm = $('config-form');
  const configEmpty = $('config-empty');
  const todayTitle = $('today-title');
  const todayContent = $('today-content');
  const todayAlert = $('today-alert');
  let isSendingRequest = false;

  const updateIsUseLabel = () => { isUseLabel.textContent = isUse.checked ? '사용' : '사용 안 함'; };

  async function loadConfig() {
    App.renderLoading(configLoading);
    configForm.hidden = true;
    const result = await App.api('GET', `/api/admin/gubuns/${gubunId}`);
    if (!result.ok) {
      App.renderLoadError(configLoading, '불러오지 못했습니다.', loadConfig);
      return;
    }
    const gubun = result.data;
    configLoading.replaceChildren();
    $('gubun-name').textContent = gubun.name;
    const hasConfig = gubun.sendTime !== null;
    configEmpty.hidden = hasConfig;
    App.fillTimeSelects(hour, minute, hasConfig ? gubun.sendTime : DEFAULT_SEND_TIME);
    isUse.checked = hasConfig ? gubun.isUse : false;
    updateIsUseLabel();
    configForm.hidden = false;
  }

  async function loadToday() {
    App.renderLoading(todayContent);
    const result = await App.api('GET', `/api/admin/gubuns/${gubunId}/today`);
    if (!result.ok) {
      App.renderLoadError(todayContent, '불러오지 못했습니다.', loadToday);
      return;
    }
    renderToday(result.data);
  }

  function sendingIndicator() {
    const box = App.el('div', 'd-flex align-items-center gap-2 text-primary');
    box.append(App.el('span', 'spinner-border spinner-border-sm'), App.el('span', '', '보내는 중입니다. 화면을 닫지 마세요.'));
    return box;
  }

  function renderToday(today) {
    todayTitle.textContent = `오늘 뉴스레터 (${today.sendDay})`;
    const nodes = [];
    const isBusy = today.isSending || isSendingRequest;
    if (!today.exists) {
      nodes.push(App.el('p', '', `오늘 만든 뉴스레터가 없습니다. 지금 구독자 ${today.pendingCount}명`));
      if (isBusy) {
        nodes.push(sendingIndicator());
      } else {
        const button = App.el('button', 'btn btn-primary', '지금 만들어 보내기');
        button.type = 'button';
        button.addEventListener('click', () => send('send', today.pendingCount));
        nodes.push(button);
      }
      todayContent.replaceChildren(...nodes);
      return;
    }
    const info = App.el('dl', 'row mb-2');
    info.append(App.el('dt', 'col-2', '제목'), App.el('dd', 'col-10', today.title),
      App.el('dt', 'col-2', '발송 시각'), App.el('dd', 'col-10', App.formatKstTime(today.sendDate)));
    nodes.push(info, App.el('p', '', `받은 회원 ${today.receivedCount}명 / 받지 않은 구독자 ${today.pendingCount}명`));
    if (isBusy) {
      nodes.push(sendingIndicator());
    } else {
      const button = App.el('button', 'btn btn-outline-primary', '받지 않은 구독자에게 다시 보내기');
      button.type = 'button';
      button.disabled = today.pendingCount === 0;
      button.addEventListener('click', () => send('resend', today.pendingCount));
      nodes.push(button);
    }
    todayContent.replaceChildren(...nodes);
  }

  async function send(kind, count) {
    const isConfirmed = kind === 'send'
      ? await App.confirmDialog('지금 만들어 보내기',
        `오늘 뉴스레터를 지금 만들어 구독자 ${count}명에게 보낼까요? 오늘은 자동 발송하지 않습니다.`, '보내기')
      : await App.confirmDialog('다시 보내기', `받지 않은 구독자 ${count}명에게 다시 보낼까요?`, '보내기');
    if (!isConfirmed) {
      return;
    }
    App.hideAlert(todayAlert);
    isSendingRequest = true;
    todayContent.querySelectorAll('button').forEach((button) => { button.disabled = true; });
    todayContent.append(sendingIndicator());
    const result = await App.api('POST', `/api/admin/gubuns/${gubunId}/today/${kind}`);
    isSendingRequest = false;
    if (result.ok) {
      App.showAlert(todayAlert, 'success',
        `${result.data.receivedCount}명에게 보냈습니다. (실패 ${result.data.failedCount}명)`);
    } else {
      App.showAlert(todayAlert, 'danger', result.error.message);
    }
    loadToday();
  }

  isUse.addEventListener('change', updateIsUseLabel);

  saveButton.addEventListener('click', async () => {
    App.hideAlert(configAlert);
    App.setLoading(saveButton, true);
    const result = await App.api('POST', `/api/admin/gubuns/${gubunId}/config`, {
      sendTime: App.readTimeSelects(hour, minute),
      isUse: isUse.checked
    });
    App.setLoading(saveButton, false);
    if (!result.ok) {
      App.showAlert(configAlert, 'danger', result.error.message);
      return;
    }
    configEmpty.hidden = true;
    App.showAlert(configAlert, 'success', '저장했습니다.', 5000);
  });

  loadConfig();
  loadToday();
})();
