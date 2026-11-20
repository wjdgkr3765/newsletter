// 공통 JS: API 호출(CSRF 헤더, 401이면 로그인 화면), 한국 시간 표시, 페이지 버튼, 확인 팝업, 인증 코드 남은 시간
const App = (() => {
  const DEFAULT_ERROR = '일시적인 오류가 발생했습니다. 잠시 뒤에 다시 시도해 주세요.';
  const KST_DATE_TIME = new Intl.DateTimeFormat('sv-SE', {
    timeZone: 'Asia/Seoul', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false
  });
  const KST_DATE = new Intl.DateTimeFormat('sv-SE', { timeZone: 'Asia/Seoul', year: 'numeric', month: '2-digit', day: '2-digit' });
  const KST_TIME = new Intl.DateTimeFormat('sv-SE', { timeZone: 'Asia/Seoul', hour: '2-digit', minute: '2-digit', hour12: false });

  function getCsrfToken() {
    const meta = document.querySelector('meta[name="csrf-token"]');
    return meta ? meta.content : '';
  }

  // 결과: { ok, status, data } 또는 { ok: false, status, error: { code, message } }
  async function api(method, url, body) {
    const options = { method, credentials: 'same-origin', headers: { Accept: 'application/json' } };
    if (method !== 'GET') {
      options.headers['Content-Type'] = 'application/json';
      options.headers.RequestVerificationToken = getCsrfToken();
      options.body = JSON.stringify(body === undefined ? {} : body);
    }
    let response;
    try {
      response = await fetch(url, options);
    } catch {
      return { ok: false, status: 0, error: { code: 'NETWORK', message: DEFAULT_ERROR } };
    }
    if (response.status === 401) {
      location.href = '/login';
      return { ok: false, status: 401, error: { code: 'UNAUTHORIZED', message: '로그인이 필요합니다.' } };
    }
    let json = null;
    try {
      json = await response.json();
    } catch {
      json = null;
    }
    if (response.ok && json && 'data' in json) {
      return { ok: true, status: response.status, data: json.data };
    }
    const error = json && json.error ? json.error : { code: 'INTERNAL_ERROR', message: DEFAULT_ERROR };
    return { ok: false, status: response.status, error };
  }

  function formatKstDateTime(iso) {
    return KST_DATE_TIME.format(new Date(iso));
  }

  function formatKstDate(iso) {
    return KST_DATE.format(new Date(iso));
  }

  function formatKstTime(iso) {
    return KST_TIME.format(new Date(iso));
  }

  // 요소 만들기. 글자는 항상 textContent로 넣는다. (security.md 6번)
  function el(tag, className, text) {
    const element = document.createElement(tag);
    if (className) {
      element.className = className;
    }
    if (text !== undefined && text !== null) {
      element.textContent = String(text);
    }
    return element;
  }

  // 알림 표시. autoHideMs가 있으면 그 뒤에 숨긴다.
  function showAlert(container, type, message, autoHideMs) {
    container.textContent = message;
    container.className = `alert alert-${type} py-2`;
    container.hidden = false;
    if (container.hideTimer) {
      clearTimeout(container.hideTimer);
    }
    if (autoHideMs) {
      container.hideTimer = setTimeout(() => { container.hidden = true; }, autoHideMs);
    }
  }

  function hideAlert(container) {
    container.hidden = true;
  }

  // 버튼 로딩 표시: 비활성화와 작은 스피너
  function setLoading(button, isLoading, loadingText) {
    if (isLoading) {
      button.dataset.originalText = button.textContent;
      button.disabled = true;
      button.replaceChildren(el('span', 'spinner-border spinner-border-sm me-1'), document.createTextNode(loadingText || button.dataset.originalText));
      return;
    }
    button.disabled = false;
    button.textContent = button.dataset.originalText || button.textContent;
  }

  // 목록 상태: 로딩 중 / 빈 목록 / 오류(다시 시도)
  function renderLoading(container) {
    const box = el('div', 'text-center text-secondary py-5');
    box.append(el('div', 'spinner-border mb-2'), el('div', '', '불러오는 중'));
    container.replaceChildren(box);
  }

  function renderLoadError(container, message, onRetry) {
    const alert = el('div', 'alert alert-danger d-flex justify-content-between align-items-center');
    const retry = el('button', 'btn btn-sm btn-outline-danger', '다시 시도');
    retry.type = 'button';
    retry.addEventListener('click', onRetry);
    alert.append(el('span', '', message), retry);
    container.replaceChildren(alert);
  }

  function emptyRow(colspan, message) {
    const row = el('tr');
    const cell = el('td', 'text-center text-secondary py-4', message);
    cell.colSpan = colspan;
    row.append(cell);
    return row;
  }

  // 페이지 버튼: 이전, 번호(최대 10개), 다음. 1페이지뿐이면 숨긴다.
  function renderPagination(container, page, totalPages, onPage) {
    container.replaceChildren();
    if (totalPages <= 1) {
      return;
    }
    const list = el('ul', 'pagination justify-content-center');
    const start = Math.floor((page - 1) / 10) * 10 + 1;
    const end = Math.min(start + 9, totalPages);
    const addItem = (label, target, isDisabled, isActive) => {
      const item = el('li', `page-item${isDisabled ? ' disabled' : ''}${isActive ? ' active' : ''}`);
      const link = el('a', 'page-link', label);
      link.href = '#';
      link.addEventListener('click', (event) => {
        event.preventDefault();
        if (!isDisabled && !isActive) {
          onPage(target);
        }
      });
      item.append(link);
      list.append(item);
    };
    addItem('이전', page - 1, page <= 1, false);
    for (let number = start; number <= end; number++) {
      addItem(String(number), number, false, number === page);
    }
    addItem('다음', page + 1, page >= totalPages, false);
    container.append(list);
  }

  // 확인 팝업. 확인을 누르면 true.
  function confirmDialog(title, message, okText, okClass) {
    const modalElement = document.getElementById('confirm-modal');
    document.getElementById('confirm-modal-title').textContent = title;
    document.getElementById('confirm-modal-message').textContent = message;
    const okButton = document.getElementById('confirm-modal-ok');
    okButton.textContent = okText || '확인';
    okButton.className = `btn ${okClass || 'btn-primary'}`;
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    return new Promise((resolve) => {
      let isConfirmed = false;
      const onOk = () => {
        isConfirmed = true;
        modal.hide();
      };
      okButton.addEventListener('click', onOk, { once: true });
      modalElement.addEventListener('hidden.bs.modal', () => {
        okButton.removeEventListener('click', onOk);
        resolve(isConfirmed);
      }, { once: true });
      modal.show();
    });
  }

  // 인증 코드 남은 시간 표시. 0이 되면 onExpire를 부른다.
  function startCodeTimer(label, seconds, onExpire) {
    let remaining = seconds;
    const render = () => {
      const minutes = String(Math.floor(remaining / 60)).padStart(2, '0');
      const rest = String(remaining % 60).padStart(2, '0');
      label.textContent = `남은 시간 ${minutes}:${rest}`;
    };
    render();
    const timer = setInterval(() => {
      remaining--;
      render();
      if (remaining <= 0) {
        clearInterval(timer);
        onExpire();
      }
    }, 1000);
    return () => clearInterval(timer);
  }

  // 발송 시간 시(00~23)·분(00~59) 선택 상자를 채우고 "HH:mm" 값을 고른다.
  function fillTimeSelects(hourSelect, minuteSelect, value) {
    const fill = (select, count) => {
      select.replaceChildren();
      for (let number = 0; number < count; number++) {
        const text = String(number).padStart(2, '0');
        const option = el('option', '', text);
        option.value = text;
        select.append(option);
      }
    };
    fill(hourSelect, 24);
    fill(minuteSelect, 60);
    setTimeSelects(hourSelect, minuteSelect, value);
  }

  function setTimeSelects(hourSelect, minuteSelect, value) {
    const [hour, minute] = value.split(':');
    hourSelect.value = hour;
    minuteSelect.value = minute;
  }

  function readTimeSelects(hourSelect, minuteSelect) {
    return `${hourSelect.value}:${minuteSelect.value}`;
  }

  function debounce(fn, waitMs) {
    let timer = null;
    return (...args) => {
      clearTimeout(timer);
      timer = setTimeout(() => fn(...args), waitMs);
    };
  }

  // 목록 검색창 (D-012): "검색" 버튼이나 Enter로 앞뒤 공백을 뺀 검색어를 넘긴다.
  function bindSearch(form, onSearch) {
    form.addEventListener('submit', (event) => {
      event.preventDefault();
      onSearch(form.elements.keyword.value.trim());
    });
  }

  // 검색어 쿼리 문자열. 빈 검색어면 붙이지 않는다.
  function keywordQuery(keyword, separator) {
    return keyword ? `${separator}keyword=${encodeURIComponent(keyword)}` : '';
  }

  // 메뉴의 로그아웃 버튼
  document.addEventListener('click', async (event) => {
    const button = event.target.closest('[data-logout]');
    if (!button) {
      return;
    }
    button.disabled = true;
    const result = await api('POST', '/api/auth/logout');
    location.href = result.ok ? result.data.redirectUrl : '/login';
  });

  return {
    DEFAULT_ERROR,
    api,
    formatKstDateTime,
    formatKstDate,
    formatKstTime,
    el,
    showAlert,
    hideAlert,
    setLoading,
    renderLoading,
    renderLoadError,
    emptyRow,
    renderPagination,
    confirmDialog,
    startCodeTimer,
    fillTimeSelects,
    setTimeSelects,
    readTimeSelects,
    debounce,
    SEARCH_EMPTY: '검색 결과가 없습니다.',
    bindSearch,
    keywordQuery
  };
})();
