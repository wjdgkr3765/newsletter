// 내 구독 화면 (MySubscriptions_screen.md)
(() => {
  const list = document.getElementById('my-subscriptions-list');
  const alertBox = document.getElementById('my-subscriptions-alert');

  async function load() {
    App.renderLoading(list);
    const result = await App.api('GET', '/api/me/subscriptions');
    if (!result.ok) {
      App.renderLoadError(list, '목록을 불러오지 못했습니다.', load);
      return;
    }
    render(result.data.items);
  }

  function render(items) {
    const table = App.el('table', 'table align-middle');
    const head = App.el('thead', 'table-light');
    const headRow = App.el('tr');
    headRow.append(App.el('th', '', '종류'), App.el('th', '', '발송 시간'), App.el('th', 'check-col', '구독'));
    head.append(headRow);
    const body = App.el('tbody');
    if (items.length === 0) {
      body.append(App.emptyRow(3, '등록된 뉴스레터 종류가 없습니다.'));
    }
    items.forEach((item) => {
      const row = App.el('tr');
      const isSending = item.sendTime !== null && item.isUse === true;
      const timeCell = isSending
        ? App.el('td', '', `매일 ${item.sendTime}`)
        : App.el('td', 'text-secondary', '지금은 발송하지 않음');
      const switchCell = App.el('td');
      const wrapper = App.el('div', 'form-check form-switch');
      const input = App.el('input', 'form-check-input');
      input.type = 'checkbox';
      input.setAttribute('role', 'switch');
      input.setAttribute('aria-label', `${item.name} 구독`);
      input.checked = item.isSubscribed;
      input.addEventListener('change', () => toggle(item.gubunId, input));
      wrapper.append(input);
      switchCell.append(wrapper);
      row.append(App.el('td', '', item.name), timeCell, switchCell);
      body.append(row);
    });
    table.append(head, body);
    list.replaceChildren(table);
  }

  async function toggle(gubunId, input) {
    const isSubscribed = input.checked;
    input.disabled = true;
    const result = await App.api('POST', '/api/me/subscriptions', { gubunId, isSubscribed });
    input.disabled = false;
    if (!result.ok) {
      input.checked = !isSubscribed;
      App.showAlert(alertBox, 'danger', result.error.message, 5000);
    }
  }

  load();
})();
