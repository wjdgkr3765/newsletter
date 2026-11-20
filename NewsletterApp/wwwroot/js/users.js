// 회원 관리 화면 (Users_screen.md)
(() => {
  const list = document.getElementById('users-list');
  const pagination = document.getElementById('users-pagination');
  const alertBox = document.getElementById('users-alert');
  let currentPage = 1;
  let keyword = '';

  async function load(page) {
    currentPage = page;
    App.renderLoading(list);
    pagination.replaceChildren();
    const result = await App.api('GET', `/api/admin/users?page=${page}${App.keywordQuery(keyword, '&')}`);
    if (!result.ok) {
      App.renderLoadError(list, '목록을 불러오지 못했습니다.', () => load(currentPage));
      return;
    }
    render(result.data);
  }

  function badge(text, color) {
    return App.el('span', `badge text-bg-${color}`, text);
  }

  function render(data) {
    const table = App.el('table', 'table align-middle');
    const head = App.el('thead', 'table-light');
    const headRow = App.el('tr');
    ['아이디', '이름', '권한', '상태', '가입일', '관리'].forEach((label) => headRow.append(App.el('th', '', label)));
    head.append(headRow);
    const body = App.el('tbody');
    if (data.items.length === 0) {
      body.append(App.emptyRow(6, keyword ? App.SEARCH_EMPTY : '회원이 없습니다.'));
    }
    data.items.forEach((user) => {
      const row = App.el('tr');
      const roleCell = App.el('td');
      roleCell.append(user.isAdmin ? badge('관리자', 'primary') : badge('일반', 'secondary'));
      const statusCell = App.el('td');
      statusCell.append(user.isSuspended ? badge('정지', 'danger') : badge('정상', 'success'));
      const actionCell = App.el('td');
      const button = user.isSuspended
        ? App.el('button', 'btn btn-sm btn-outline-secondary', '정지 해제')
        : App.el('button', 'btn btn-sm btn-outline-danger', '정지');
      button.type = 'button';
      if (!user.isSuspended && user.isMe) {
        button.disabled = true;
        button.title = '자기 자신은 정지할 수 없습니다.';
      }
      button.addEventListener('click', () => changeSuspension(user));
      actionCell.append(button);
      row.append(App.el('td', '', user.loginId), App.el('td', '', user.name), roleCell, statusCell,
        App.el('td', '', App.formatKstDate(user.createDate)), actionCell);
      body.append(row);
    });
    table.append(head, body);
    list.replaceChildren(table);
    App.renderPagination(pagination, data.page, data.totalPages, load);
  }

  async function changeSuspension(user) {
    const isSuspended = !user.isSuspended;
    const target = `${user.name}(${user.loginId})`;
    const isConfirmed = isSuspended
      ? await App.confirmDialog('회원 정지',
        `${target} 회원을 정지할까요? 정지하면 다음 로그인부터 로그인할 수 없고 뉴스레터를 받지 않습니다.`, '확인', 'btn-danger')
      : await App.confirmDialog('정지 해제', `${target} 회원의 정지를 풀까요?`, '확인', 'btn-primary');
    if (!isConfirmed) {
      return;
    }
    const result = await App.api('POST', `/api/admin/users/${user.id}/suspension`, { isSuspended });
    if (!result.ok) {
      App.showAlert(alertBox, 'danger', result.error.message, 5000);
      return;
    }
    App.showAlert(alertBox, 'success', isSuspended ? '정지했습니다.' : '정지를 풀었습니다.', 5000);
    load(currentPage);
  }

  App.bindSearch(document.getElementById('users-search'), (value) => {
    keyword = value;
    load(1);
  });

  load(1);
})();
