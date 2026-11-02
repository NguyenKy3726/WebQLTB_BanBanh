import { readFileSync } from 'node:fs';
import vm from 'node:vm';
import test from 'node:test';
import assert from 'node:assert/strict';
const source = readFileSync(new URL('../Project/WebQLTB/WebQLTB/wwwroot/js/order-confirmation.js', import.meta.url), 'utf8');
function setup(clipboard, missing = false) {
    let click;
    const input = { value: 'DH261102001', focus() { this.focused = true; }, select() { this.selected = true; }, setSelectionRange(start, end) { this.range = [start, end]; } };
    const status = { textContent: '' };
    const elements = { 'transfer-content': input, 'copy-transfer-status': status, 'copy-transfer-content': { addEventListener(event, handler) { assert.equal(event, 'click'); click = handler; } } };
    vm.runInNewContext(source, {
        document: { addEventListener(event, handler) { assert.equal(event, 'DOMContentLoaded'); handler(); }, getElementById(id) { return missing ? null : elements[id]; } },
        navigator: { clipboard }
    });
    return { input, status, click };
}
test('copies exactly the order code without added prose', async () => {
    let copied;
    const page = setup({ async writeText(text) { copied = text; } });
    await page.click();
    assert.equal(copied, 'DH261102001');
    assert.match(page.status.textContent, /Đã sao chép/);
});
for (const [label, clipboard] of [['HTTP/LAN without clipboard', undefined], ['permission denied', { async writeText() { throw new Error('denied'); } }]]) {
    test(label + ' selects the entire order code for manual copying', async () => {
        const page = setup(clipboard);
        await page.click();
        assert.equal(page.input.focused, true);
        assert.equal(page.input.selected, true);
        assert.deepEqual(page.input.range, [0, page.input.value.length]);
        assert.match(page.status.textContent, /Ctrl\+C/);
    });
}
test('pages without transfer instructions do not attach a handler', () => {
    assert.equal(setup(undefined, true).click, undefined);
});
