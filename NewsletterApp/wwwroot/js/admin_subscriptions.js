// 구독 화면 (AdminSubscriptions_screen.md)
(() => {
  const dropdown = document.getElementById('admin-subscriptions-gubun');
  const list = document.getElementById('admin-subscriptions-list');
  const pagination = document.getElementById('admin-subscriptions-pagination');
  const alertBox = document.getElementById('admin-subscriptions-alert');
  let currentPage = 1;

  const selectedGubunId = () => (dropdown.value ? Number(dropdown.value) : null);

  async function loadGubuns() {
    const result = await App.api('GET', '/api/admin/gubuns');
    if (!result.ok) {
      App.showAlert(alertBox, 'danger', result.error.message, 5000);
      return;
    }
    result.data.items.forEach((gubun) => {
      const option = App.el('option', '', gubun.name);
      option.value = String(gubun.id);
      dropdown.append(option);
    });
  }

  async function load(page) {
    currentPage = page;
    App.renderLoading(list);
    pagination.replaceChildren();
    const gubunId = selectedGubunId();
    const query = gubunId === null ? `page=${page}` : `gubunId=${gubunId}&page=${page}`;
    const result = await App.api('GET', `/api/admin/subscriptions?${query}`);
    if (!result.ok) {
      App.renderLoadError(list, '목록을 불러오지 못했습니다.', () => load(currentPage));
      return;
    }
    render(result.data, gubunId);
  }

  function render(data, gubunId) {
    const hasCheck = gubunId !== null;
    const table = App.el('table', 'table align-middle');
    const head = App.el('thead', 'table-light');
    const headRow = App.el('tr');
    if (hasCheck) {
      headRow.append(App.el('th', 'check-col', '구독'));
    }
    headRow.append(App.el('th', '', '이름'), App.el('th', '', '이메일'));
    head.append(headRow);
    const body = App.el('tbody');
    if (data.items.length === 0) {
      body.append(App.emptyRow(hasCheck ? 3 : 2, '회원이 없습니다.'));
    }
    data.items.forEach((user) => {
      const row = App.el('tr');
      if (hasCheck) {
        const cell = App.el('td');
        const input = App.el('input', 'form-check-input');
        input.type = 'checkbox';
        input.setAttribute('aria-label', `${user.name} 구독`);
        input.checked = user.isSubscribed === true;
        input.addEventListener('change', () => toggle(user.userId, gubunId, input));
        cell.append(input);
        row.append(cell);
      }
      row.append(App.el('td', '', user.name), App.el('td', '', user.email));
      body.append(row);
    });
    table.append(head, body);
    list.replaceChildren(table);
    App.renderPagination(pagination, data.page, data.totalPages, load);
  }

  async function toggle(userId, gubunId, input) {
    const isSubscribed = input.checked;
    input.disabled = true;
    const result = await App.api('POST', '/api/admin/subscriptions', { userId, gubunId, isSubscribed });
    input.disabled = false;
    if (!result.ok) {
      input.checked = !isSubscribed;
      App.showAlert(alertBox, 'danger', result.error.message, 5000);
    }
  }

  dropdown.addEventListener('change', () => load(1));

  (async () => {
    await loadGubuns();
    await load(1);
  })();
})();
