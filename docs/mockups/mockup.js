// 시안 공통 스크립트. 상태 전환 막대와 왼쪽 메뉴를 그린다. 실제 기능은 없다.
(function () {
  const MENUS = [
    { key: 'history', label: '뉴스레터 수신 내역', href: 'history.html', isAdminOnly: false },
    { key: 'my_subscriptions', label: '내 구독', href: 'my_subscriptions.html', isAdminOnly: false },
    { key: 'users', label: '회원 관리', href: 'users.html', isAdminOnly: true },
    { key: 'admin_subscriptions', label: '구독', href: 'admin_subscriptions.html', isAdminOnly: true },
    { key: 'gubuns', label: '뉴스레터 설정', href: 'gubuns.html', isAdminOnly: true },
    { key: 'profile', label: '내 정보', href: 'profile.html', isAdminOnly: false }
  ];

  function createElement(tag, className, text) {
    const element = document.createElement(tag);
    if (className) {
      element.className = className;
    }
    if (text) {
      element.textContent = text;
    }
    return element;
  }

  function renderSidebar(role) {
    const nav = document.querySelector('[data-sidebar]');
    if (!nav) {
      return;
    }
    const isAdmin = role === 'admin';
    const active = nav.dataset.active;
    nav.replaceChildren();

    nav.appendChild(createElement('div', 'sidebar-brand mb-1', '뉴스레터'));
    nav.appendChild(createElement('div', 'text-secondary small mb-3', isAdmin ? '관리자 님' : '홍길동 님'));

    const list = createElement('ul', 'nav nav-pills flex-column gap-1');
    MENUS.filter(function (menu) { return isAdmin || !menu.isAdminOnly; }).forEach(function (menu) {
      const item = createElement('li', 'nav-item');
      const link = createElement('a', 'nav-link' + (menu.key === active ? ' active' : ''), menu.label);
      link.href = menu.href;
      item.appendChild(link);
      list.appendChild(item);
    });
    nav.appendChild(list);

    const logout = createElement('a', 'btn btn-link text-secondary sidebar-logout text-start ps-0', '로그아웃');
    logout.href = 'login.html';
    nav.appendChild(logout);
  }

  function showState(sections, buttons, index) {
    sections.forEach(function (section, i) {
      section.hidden = i !== index;
    });
    buttons.forEach(function (button, i) {
      button.className = 'btn btn-sm ' + (i === index ? 'btn-light' : 'btn-outline-light');
    });
    const role = sections[index].dataset.role;
    if (role) {
      renderSidebar(role);
    }
  }

  function init() {
    const nav = document.querySelector('[data-sidebar]');
    if (nav) {
      renderSidebar(nav.dataset.role);
    }

    const sections = Array.from(document.querySelectorAll('[data-state]'));
    const bar = document.querySelector('.mock-bar');
    if (!bar || sections.length === 0) {
      return;
    }
    bar.appendChild(createElement('span', 'mock-bar-title', '시안 상태:'));
    const buttons = sections.map(function (section, index) {
      const button = createElement('button', '', section.dataset.state);
      button.type = 'button';
      button.addEventListener('click', function () { showState(sections, buttons, index); });
      bar.appendChild(button);
      return button;
    });
    showState(sections, buttons, 0);
  }

  document.addEventListener('DOMContentLoaded', init);
})();
