// 뉴스레터 설정 화면 (Gubuns_screen.md)
(() => {
  const $ = (id) => document.getElementById(id);
  const list = $('gubuns-list');
  const alertBox = $('gubuns-alert');
  const modal = new bootstrap.Modal($('gubun-add-modal'));
  const nameInput = $('gubun-add-name');
  const hour = $('gubun-add-hour');
  const minute = $('gubun-add-minute');
  const isUse = $('gubun-add-is-use');
  const isUseLabel = $('gubun-add-is-use-label');
  const submit = $('gubun-add-submit');
  const addAlert = $('gubun-add-alert');
  const DEFAULT_SEND_TIME = '16:00';
  let keyword = '';

  async function load() {
    App.renderLoading(list);
    const result = await App.api('GET', `/api/admin/gubuns${App.keywordQuery(keyword, '?')}`);
    if (!result.ok) {
      App.renderLoadError(list, '목록을 불러오지 못했습니다.', load);
      return;
    }
    render(result.data.items);
  }

  function render(items) {
    const table = App.el('table', 'table table-hover align-middle');
    const head = App.el('thead', 'table-light');
    const headRow = App.el('tr');
    ['종류', '발송 시간', '사용 여부'].forEach((label) => headRow.append(App.el('th', '', label)));
    head.append(headRow);
    const body = App.el('tbody');
    if (items.length === 0) {
      body.append(App.emptyRow(3, keyword ? App.SEARCH_EMPTY : "등록된 종류가 없습니다. '종류 추가'로 추가해 주세요."));
    }
    items.forEach((gubun) => {
      const row = App.el('tr', 'clickable-row');
      const nameCell = App.el('td');
      const link = App.el('a', 'text-reset text-decoration-none', gubun.name);
      link.href = `/admin/gubuns/${gubun.id}`;
      nameCell.append(link);
      const hasConfig = gubun.sendTime !== null;
      const timeCell = hasConfig ? App.el('td', '', gubun.sendTime) : App.el('td', 'text-secondary', '설정 없음');
      const useCell = App.el('td', hasConfig ? '' : 'text-secondary', hasConfig ? '' : '설정 없음');
      if (hasConfig) {
        useCell.append(App.el('span', `badge text-bg-${gubun.isUse ? 'success' : 'secondary'}`, gubun.isUse ? '사용' : '사용 안 함'));
      }
      row.addEventListener('click', () => { location.href = link.href; });
      row.append(nameCell, timeCell, useCell);
      body.append(row);
    });
    table.append(head, body);
    list.replaceChildren(table);
  }

  const updateIsUseLabel = () => { isUseLabel.textContent = isUse.checked ? '사용' : '사용 안 함'; };

  $('gubun-add-open').addEventListener('click', () => {
    nameInput.value = '';
    Validation.clearFieldState(nameInput);
    App.setTimeSelects(hour, minute, DEFAULT_SEND_TIME);
    isUse.checked = false;
    updateIsUseLabel();
    App.hideAlert(addAlert);
    submit.disabled = true;
    modal.show();
  });

  nameInput.addEventListener('input', () => {
    const message = Validation.gubunName(nameInput.value);
    Validation.setFieldState(nameInput, message);
    submit.disabled = message !== '';
  });

  isUse.addEventListener('change', updateIsUseLabel);

  submit.addEventListener('click', async () => {
    App.hideAlert(addAlert);
    App.setLoading(submit, true);
    const result = await App.api('POST', '/api/admin/gubuns', {
      name: nameInput.value.trim(),
      sendTime: App.readTimeSelects(hour, minute),
      isUse: isUse.checked
    });
    App.setLoading(submit, false);
    if (!result.ok) {
      if (result.error.code === 'DUPLICATE_GUBUN_NAME') {
        Validation.setFieldState(nameInput, result.error.message);
        return;
      }
      App.showAlert(addAlert, 'danger', result.error.message);
      return;
    }
    modal.hide();
    App.showAlert(alertBox, 'success', '종류를 추가했습니다.', 5000);
    load();
  });

  App.bindSearch($('gubuns-search'), (value) => {
    keyword = value;
    load();
  });

  App.fillTimeSelects(hour, minute, DEFAULT_SEND_TIME);
  load();
})();
