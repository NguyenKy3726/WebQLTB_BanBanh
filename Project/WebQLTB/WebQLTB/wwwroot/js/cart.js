function cartQuantity(value) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? Math.max(1, Math.min(50, Math.trunc(parsed))) : 1;
}
function cartPreview(items) {
    const valid = items.every(item => item.valid);
    return {
        valid,
        dirty: items.some(item => item.quantity !== item.original),
        quantity: valid ? items.reduce((sum, item) => sum + item.quantity, 0) : null,
        total: valid ? items.filter(item => item.available).reduce((sum, item) => sum + item.price * item.quantity, 0) : null
    };
}
document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("cart-page");
    const form = document.getElementById("cart-update-form");
    if (!root || !form) return;
    const rows = Array.from(root.querySelectorAll(".cart-line"));
    const checkout = document.getElementById("cart-checkout");
    const saveFirst = document.getElementById("cart-save-first");
    const pending = document.getElementById("cart-pending");
    const money = value => value.toLocaleString("vi-VN") + "đ";

    function refresh() {
        const items = rows.map(row => {
            const input = row.querySelector(".cart-quantity-input");
            const quantity = Number(input.value);
            const valid = input.validity.valid;
            const available = row.dataset.available === "true";
            const price = Number(row.dataset.unitPrice);
            row.querySelector(".cart-line-total").textContent =
                available && valid ? money(price * quantity) : "—";
            row.querySelector('[data-quantity-change="-1"]').disabled = valid && quantity <= 1;
            row.querySelector('[data-quantity-change="1"]').disabled = valid && quantity >= 50;
            return {quantity, valid, available, price, original: Number(input.dataset.original)};
        });
        const preview = cartPreview(items);
        document.getElementById("cart-subtotal").textContent = preview.valid ? money(preview.total) : "—";
        document.getElementById("cart-item-count").textContent = preview.valid ? String(preview.quantity) : "—";
        const unsaved = preview.dirty || !preview.valid;
        pending.hidden = !unsaved;
        if (checkout) {
            checkout.hidden = unsaved;
            saveFirst.hidden = !unsaved;
        }
    }

    rows.forEach(row => {
        const input = row.querySelector(".cart-quantity-input");
        input.addEventListener("input", refresh);
        input.addEventListener("change", refresh);
        row.querySelectorAll("[data-quantity-change]").forEach(button =>
            button.addEventListener("click", () => {
                input.value = String(Math.max(1, Math.min(50, cartQuantity(input.value) + Number(button.dataset.quantityChange))));
                refresh();
            }));
    });

    root.querySelectorAll(".cart-line__image img").forEach(img => {
        const fail = () => {
            img.hidden = true;
            img.parentElement.querySelector(".cart-image-fallback").hidden = false;
        };
        img.addEventListener("error", fail);
        if (img.complete && img.naturalWidth === 0) fail();
    });
    document.getElementById("cart-clear-form").addEventListener("submit", event => {
        if (!window.confirm("Bạn muốn xóa toàn bộ bánh trong giỏ hàng?")) event.preventDefault();
    });
    form.addEventListener("submit", event => {
        if (form.dataset.submitting === "true") { event.preventDefault(); return; }
        form.dataset.submitting = "true";
        const button = document.getElementById("cart-update-button");
        button.disabled = true;
        button.textContent = "Đang lưu…";
    });
    window.addEventListener("pageshow", () => {
        delete form.dataset.submitting;
        const button = document.getElementById("cart-update-button");
        button.disabled = false;
        button.textContent = "Cập nhật giỏ hàng";
        refresh();
    });
    refresh();
});

