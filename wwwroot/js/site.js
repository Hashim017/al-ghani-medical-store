(function () {
    const $ = (s, r = document) => r.querySelector(s);
    const $$ = (s, r = document) => [...r.querySelectorAll(s)];
    const root = document.documentElement;

    // Dark mode
    $('#themeBtn')?.addEventListener('click', () => {
        const next = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        root.setAttribute('data-bs-theme', next);
        try { localStorage.setItem('theme', next); } catch { }
    });

    // Mobile menu
    const side = $('#sidebar');
    // Keep the sidebar scroll position between pages
    window.addEventListener('pagehide', () => {
        try { sessionStorage.setItem('sideScroll', side.scrollTop); } catch { }
    });
    $('#menuBtn')?.addEventListener('click', e => { e.stopPropagation(); side.classList.toggle('open'); });
    document.addEventListener('click', e => {
        if (side && side.classList.contains('open') && !side.contains(e.target)) side.classList.remove('open');
    });

    // Greeting and clock
    function tick() {
        const d = new Date(), h = d.getHours();
        const g = $('#greet');
        if (g) g.textContent = h < 12 ? 'Good morning' : h < 17 ? 'Good afternoon' : 'Good evening';
        const c = $('#clock');
        if (c) c.textContent =
            d.toLocaleDateString('en-GB', { weekday: 'long', day: '2-digit', month: 'short', year: 'numeric' }) +
            ' · ' + d.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
    }
    tick();
    setInterval(tick, 30000);

    // Count up numbers
    $$('[data-count]').forEach(el => {
        const target = parseFloat(el.dataset.count) || 0;
        const dec = parseInt(el.dataset.dec || '0');
        const t0 = performance.now(), dur = 900;
        const fmt = v => v.toLocaleString('en-US', { minimumFractionDigits: dec, maximumFractionDigits: dec });
        (function step(now) {
            const p = Math.min((now - t0) / dur, 1);
            el.textContent = fmt(target * (1 - Math.pow(1 - p, 3)));
            if (p < 1) requestAnimationFrame(step);
        })(t0);
    });

    // Tables: hover and row entrance
    $$('table.table').forEach(t => {
        t.classList.add('table-hover');
        $$('tbody tr', t).slice(0, 25).forEach((r, i) => r.style.setProperty('--i', i));
    });

    // Toasts
    window.toast = (msg, type) => {
        let w = $('.toast-wrap');
        if (!w) { w = document.createElement('div'); w.className = 'toast-wrap'; document.body.appendChild(w); }
        const t = document.createElement('div');
        t.className = 'toast-x ' + (type || '');
        t.textContent = msg;
        w.appendChild(t);
        setTimeout(() => { t.classList.add('out'); setTimeout(() => t.remove(), 300); }, 3500);
    };
    $$('[data-toast]').forEach(e => window.toast(e.dataset.toast, e.dataset.type));

    // Command palette
    const back = $('#palette'), input = $('#palInput'), list = $('#palList');
    if (back && input && list) {
        const links = $$('.sidebar a.nav-item-link').map(a => ({ text: a.textContent.trim(), href: a.href }));
        let items = [], sel = 0;

        const render = () => {
            const q = input.value.trim().toLowerCase();
            items = links.filter(l => l.text.toLowerCase().includes(q));
            sel = Math.min(sel, Math.max(items.length - 1, 0));
            list.innerHTML = items.map((l, i) =>
                `<a class="pal-item${i === sel ? ' sel' : ''}" href="${l.href}">${l.text}</a>`).join('')
                || '<div class="empty">Nothing found</div>';
        };
        const open = () => { back.classList.add('open'); input.value = ''; sel = 0; render(); input.focus(); };
        const close = () => back.classList.remove('open');

        $('#palBtn')?.addEventListener('click', open);
        back.addEventListener('click', e => { if (e.target === back) close(); });
        input.addEventListener('input', () => { sel = 0; render(); });
        input.addEventListener('keydown', e => {
            if (e.key === 'ArrowDown') { sel = Math.min(sel + 1, items.length - 1); render(); e.preventDefault(); }
            else if (e.key === 'ArrowUp') { sel = Math.max(sel - 1, 0); render(); e.preventDefault(); }
            else if (e.key === 'Enter' && items[sel]) location.href = items[sel].href;
        });
        document.addEventListener('keydown', e => {
            const typing = /INPUT|TEXTAREA|SELECT/.test(document.activeElement.tagName);
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); open(); }
            else if (e.key === '/' && !typing) { e.preventDefault(); open(); }
            else if (e.key === 'Escape') close();
        });
    }
})();