// 뉴스레터 수신 내역 화면 (History_screen.md)
(() => {
  const list = document.getElementById('history-list');
  const pagination = document.getElementById('history-pagination');
  const alertBox = document.getElementById('history-alert');
  const modalElement = document.getElementById('newsletter-modal');
  const modalTitle = document.getElementById('newsletter-modal-title');
  const modalInfo = document.getElementById('newsletter-modal-info');
  const modalBody = document.getElementById('newsletter-modal-body');
  const modal = new bootstrap.Modal(modalElement);
  let currentPage = 1;

  async function load(page) {
    currentPage = page;
    App.renderLoading(list);
    pagination.replaceChildren();
    const result = await App.api('GET', `/api/newsletters?page=${page}`);
    if (!result.ok) {
      App.renderLoadError(list, '목록을 불러오지 못했습니다.', () => load(currentPage));
      return;
    }
    render(result.data);
  }

  function render(data) {
    const table = App.el('table', 'table table-hover align-middle');
    const head = App.el('thead', 'table-light');
    const headRow = App.el('tr');
    const columns = data.isAdmin ? ['제목', '종류', '발송 시각', '구독자'] : ['제목', '종류', '발송 시각'];
    columns.forEach((label) => headRow.append(App.el('th', '', label)));
    head.append(headRow);
    const body = App.el('tbody');
    if (data.items.length === 0) {
      body.append(App.emptyRow(columns.length, data.isAdmin ? '발송된 뉴스레터가 없습니다.' : '받은 뉴스레터가 없습니다.'));
    }
    data.items.forEach((item) => {
      const row = App.el('tr');
      const titleCell = App.el('td');
      const link = App.el('a', '', item.title);
      link.href = '#';
      link.addEventListener('click', (event) => {
        event.preventDefault();
        openDetail(item.id);
      });
      titleCell.append(link);
      row.append(titleCell, App.el('td', '', item.gubunName), App.el('td', '', App.formatKstDateTime(item.sendDate)));
      if (data.isAdmin) {
        row.append(App.el('td', '', `${item.subscriberCount}명`));
      }
      body.append(row);
    });
    table.append(head, body);
    list.replaceChildren(table);
    App.renderPagination(pagination, data.page, data.totalPages, load);
  }

  async function openDetail(id) {
    App.hideAlert(alertBox);
    modalTitle.textContent = '';
    modalInfo.textContent = '';
    const loading = App.el('div', 'text-center text-secondary py-5');
    loading.append(App.el('div', 'spinner-border'));
    modalBody.replaceChildren(loading);
    modal.show();
    const result = await App.api('GET', `/api/newsletters/${id}`);
    if (!result.ok) {
      modal.hide();
      App.showAlert(alertBox, 'danger',
        result.error.code === 'NOT_FOUND' ? '뉴스레터를 찾을 수 없습니다.' : result.error.message, 5000);
      return;
    }
    const detail = result.data;
    modalTitle.textContent = detail.title;
    modalInfo.textContent = `${detail.gubunName} · ${App.formatKstDateTime(detail.sendDate)}`;
    // 본문 HTML은 스크립트를 막은 iframe 안에서만 보여준다.
    const frame = App.el('iframe', 'newsletter-frame');
    frame.setAttribute('sandbox', '');
    frame.title = '뉴스레터 본문';
    frame.srcdoc = detail.content;
    modalBody.replaceChildren(frame);
  }

  load(1);
})();
