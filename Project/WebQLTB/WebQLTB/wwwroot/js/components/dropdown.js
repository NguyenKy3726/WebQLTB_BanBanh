/* Progressive enhancement: original select remains the source for forms/change events. */
(() => {
    'use strict';
    let sequence = 0, active = null;
    const widgets = new WeakMap();
    function close() {
        if (!active) return;
        active.button.setAttribute('aria-expanded', 'false');
        active.button.removeAttribute('aria-activedescendant');
        active.menu.hidden = true;
        active = null;
    }
    function enhance(select) {
        if (widgets.has(select) || select.multiple || select.size > 1 || select.matches('[data-native-select],.select2-hidden-accessible')) return;
        const wrapper = document.createElement('span');
        wrapper.className = 'wf-select';
        select.before(wrapper);
        wrapper.append(select);
        select.classList.add('wf-select__native');
        select.tabIndex = -1;
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'wf-select__trigger';
        button.setAttribute('role', 'combobox');
        button.setAttribute('aria-haspopup', 'listbox');
        button.setAttribute('aria-expanded', 'false');
        const text = document.createElement('span');
        text.className = 'wf-select__text';
        button.append(text);
        const label = select.labels && select.labels[0];
        const labelCopy = label && label.cloneNode(true);
        if (labelCopy) labelCopy.querySelectorAll('select,.wf-select,button').forEach(el => el.remove());
        button.setAttribute('aria-label', select.getAttribute('aria-label') || (labelCopy && labelCopy.textContent.trim()) || select.name || 'Chọn mục');
        if (select.getAttribute('aria-labelledby')) button.setAttribute('aria-labelledby', select.getAttribute('aria-labelledby'));
        if (select.getAttribute('aria-describedby')) button.setAttribute('aria-describedby', select.getAttribute('aria-describedby'));
        const menu = document.createElement('div');
        menu.id = `wf-select-menu-${++sequence}`;
        menu.className = 'wf-select-menu';
        menu.setAttribute('role', 'listbox');
        menu.setAttribute('aria-label', button.getAttribute('aria-label'));
        menu.hidden = true;
        button.setAttribute('aria-controls', menu.id);
        wrapper.append(button);
        document.body.append(menu);
        const widget = { select, wrapper, button, menu, index: -1, rows: [] };
        widgets.set(select, widget);
        const options = () => Array.from(select.options);
        const enabled = i => options()[i] && !options()[i].disabled && !options()[i].hidden && !options()[i].parentElement.hidden && !(options()[i].parentElement.tagName === 'OPTGROUP' && options()[i].parentElement.disabled);
        function sync() {
            text.textContent = select.selectedOptions[0]?.textContent || select.dataset.placeholder || 'Chọn mục';
            button.disabled = select.disabled;
            button.setAttribute('aria-required', String(select.required));
            if (select.validity.valid) button.removeAttribute('aria-invalid');
            if (active === widget) render();
        }
        function highlight(i) {
            widget.index = i;
            widget.rows.forEach((row, index) => row && row.classList.toggle('is-active', index === i));
            const row = widget.rows[i];
            if (row) { button.setAttribute('aria-activedescendant', row.id); row.scrollIntoView({ block: 'nearest' }); }
        }
        function render() {
            menu.replaceChildren(); widget.rows = [];
            let lastGroup = null;
            options().forEach((option, index) => {
                if (option.hidden || option.parentElement.hidden) return;
                const group = option.parentElement.tagName === 'OPTGROUP' ? option.parentElement : null;
                if (group && group !== lastGroup) {
                    const heading = document.createElement('div'); heading.className = 'wf-select-group'; heading.textContent = group.label; heading.setAttribute('role', 'presentation'); menu.append(heading);
                }
                lastGroup = group;
                const row = document.createElement('div');
                row.id = `${menu.id}-${index}`;
                row.className = 'wf-select-option'; row.textContent = option.textContent;
                row.setAttribute('role', 'option');
                row.setAttribute('aria-selected', String(index === select.selectedIndex));
                row.setAttribute('aria-disabled', String(!enabled(index)));
                row.addEventListener('pointermove', () => { if (enabled(index)) highlight(index); });
                row.addEventListener('click', () => choose(index));
                widget.rows[index] = row; menu.append(row);
            });
            if (!menu.children.length) { const empty = document.createElement('div'); empty.className = 'wf-select-empty'; empty.textContent = 'Chưa có lựa chọn'; menu.append(empty); }
        }
        function open() {
            if (select.disabled) return;
            close(); render(); active = widget;
            menu.hidden = false; button.setAttribute('aria-expanded', 'true');
            const rect = button.getBoundingClientRect();
            const gap = 7, below = window.innerHeight - rect.bottom - 14, above = rect.top - 14;
            const upward = below < 180 && above > below;
            const height = Math.max(40, Math.min(300, upward ? above : below));
            menu.style.width = `${Math.min(rect.width, window.innerWidth - 16)}px`;
            menu.style.left = `${Math.max(8, Math.min(rect.left, window.innerWidth - rect.width - 8))}px`;
            menu.style.maxHeight = `${height}px`;
            menu.style.fontFamily = getComputedStyle(button).fontFamily;
            menu.style.fontSize = getComputedStyle(button).fontSize;
            if (upward) { menu.style.top = 'auto'; menu.style.bottom = `${window.innerHeight - rect.top + gap}px`; }
            else { menu.style.bottom = 'auto'; menu.style.top = `${rect.bottom + gap}px`; }
            highlight(enabled(select.selectedIndex) ? select.selectedIndex : options().findIndex((_, i) => enabled(i)));
        }
        function choose(i) {
            if (!enabled(i)) return;
            const changed = select.selectedIndex !== i;
            select.selectedIndex = i;
            close(); sync(); button.focus();
            if (changed) { select.dispatchEvent(new Event('input', { bubbles: true })); select.dispatchEvent(new Event('change', { bubbles: true })); }
        }
        button.addEventListener('click', () => active === widget ? close() : open());
        let search = '', timer;
        button.addEventListener('keydown', event => {
            if (event.key === 'Tab') { close(); return; }
            if (event.key === 'Escape') { event.preventDefault(); close(); return; }
            if (['ArrowDown', 'ArrowUp', 'Home', 'End', 'Enter', ' '].includes(event.key)) {
                event.preventDefault();
                if (active !== widget) { open(); return; }
                if (event.key === 'Enter' || event.key === ' ') { choose(widget.index); return; }
                const indexes = options().map((_, i) => i).filter(enabled);
                const current = indexes.indexOf(widget.index);
                const next = event.key === 'Home' ? 0 : event.key === 'End' ? indexes.length - 1 : Math.max(0, Math.min(indexes.length - 1, current + (event.key === 'ArrowDown' ? 1 : -1)));
                highlight(indexes[next]); return;
            }
            if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
                event.preventDefault(); if (active !== widget) open();
                clearTimeout(timer); search += event.key.toLocaleLowerCase('vi');
                const index = options().findIndex((option, i) => enabled(i) && option.textContent.trim().toLocaleLowerCase('vi').startsWith(search));
                if (index >= 0) highlight(index);
                timer = setTimeout(() => { search = ''; }, 650);
            }
        });
        select.addEventListener('change', sync);
        select.addEventListener('focus', () => button.focus());
        select.addEventListener('invalid', event => { event.preventDefault(); button.setAttribute('aria-invalid', 'true'); button.focus(); open(); });
        menu.addEventListener('pointerdown', event => event.preventDefault());
        button.addEventListener('blur', () => { if (active === widget) close(); });
        if (select.form) select.form.addEventListener('reset', () => setTimeout(sync, 0));
        new MutationObserver(sync).observe(select, { childList: true, subtree: true, attributes: true, attributeFilter: ['disabled','hidden','selected','label','required'] });
        sync();
    }
    function init(root = document) {
        if (root.matches?.('select')) enhance(root);
        root.querySelectorAll?.('select').forEach(enhance);
    }
    document.addEventListener('pointerdown', event => { if (active && !active.wrapper.contains(event.target) && !active.menu.contains(event.target)) close(); });
    document.addEventListener('scroll', event => { if (active && !active.menu.contains(event.target)) close(); }, true);
    window.addEventListener('resize', close);
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => init()); else init();
    new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(node => { if (node.nodeType === 1) init(node); })) ).observe(document.documentElement, { childList: true, subtree: true });
    window.WhiskfulDropdown = { refresh: (select) => { if (select) select.dispatchEvent(new Event('change', { bubbles: true })); else init(); } };
})();
