function cakeQuantityValue(value) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? Math.min(50, Math.max(1, Math.trunc(parsed))) : 1;
}
function cakeSelectionDetails(input, quantity) {
    const price = Number(input.dataset.price);
    return {
        price: Number.isFinite(price) ? price : 0,
        total: (Number.isFinite(price) ? price : 0) * quantity,
        available: Math.max(0, Number(input.dataset.available) || 0),
        days: Math.max(0, Number(input.dataset.days) || 0)
    };
}
document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("cake-detail");
    if (!root) return;
    const get = id => document.getElementById(id);
    const image = get("cake-main-image");
    const empty = get("cake-image-empty");
    const zoomButton = get("cake-zoom-open");
    const dialog = get("cake-lightbox");
    const zoomImage = get("cake-zoom-image");
    const thumbnails = Array.from(root.querySelectorAll(".cake-gallery__thumbnail"));
    const money = value => value.toLocaleString("vi-VN") + "đ";

    if (image) {
        function imageLoaded() {
            image.hidden = false;
            empty.hidden = true;
            if (zoomButton && typeof dialog.showModal === "function") zoomButton.hidden = false;
        }
        function imageFailed() {
            image.hidden = true;
            empty.hidden = false;
            empty.textContent = "Chưa tải được ảnh này. Bạn có thể chọn ảnh khác bên dưới.";
            if (zoomButton) zoomButton.hidden = true;
        }
        image.addEventListener("load", imageLoaded);
        image.addEventListener("error", imageFailed);
        if (image.complete) {
            if (image.naturalWidth > 0) imageLoaded();
            else imageFailed();
        }
        thumbnails.forEach(button => button.addEventListener("click", () => {
            thumbnails.forEach(item => item.setAttribute("aria-pressed", String(item === button)));
            image.alt = button.dataset.imageAlt;
            image.src = button.dataset.imageSrc;
        }));
        zoomButton?.addEventListener("click", () => {
            if (image.hidden || typeof dialog.showModal !== "function") return;
            zoomImage.src = image.currentSrc || image.src;
            zoomImage.alt = image.alt;
            dialog.showModal();
        });
        get("cake-zoom-close").addEventListener("click", () => dialog.close());
        dialog.addEventListener("click", event => {
            const rect = dialog.getBoundingClientRect();
            if (event.target === dialog &&
                (event.clientX < rect.left || event.clientX > rect.right ||
                 event.clientY < rect.top || event.clientY > rect.bottom)) dialog.close();
        });
    }

    const form = get("cake-order-form");
    if (!form) return;
    const quantity = get("cake-quantity");
    const minus = get("cake-quantity-minus");
    const plus = get("cake-quantity-plus");
    const addButton = get("cake-add-button");
    const variants = Array.from(form.querySelectorAll('input[name="maBienThe"]'));

    function update() {
        const selected = variants.find(input => input.checked);
        if (!selected) return;
        const validQuantity = quantity.validity.valid;
        const count = validQuantity ? Number(quantity.value) : null;
        const details = cakeSelectionDetails(selected, count ?? 1);
        get("cake-unit-price").textContent = money(details.price);
        get("cake-subtotal").textContent = count === null ? "—" : money(details.total);
        get("cake-availability").textContent = details.available > 0
            ? "Tồn khả dụng tham khảo: " + details.available + " bánh."
            : "Nhận đặt trước — nhân viên sẽ xác nhận khả năng đáp ứng.";
        get("cake-preparation").textContent = details.days > 0
            ? "Thời gian chuẩn bị dự kiến: khoảng " + details.days + " ngày."
            : "Thời gian nhận bánh sẽ được nhân viên xác nhận.";
        minus.disabled = count !== null && count <= 1;
        plus.disabled = count !== null && count >= 50;
    }
    variants.forEach(input => input.addEventListener("change", update));
    quantity.addEventListener("input", update);
    quantity.addEventListener("change", update);
    minus.addEventListener("click", () => {
        quantity.value = String(Math.max(1, cakeQuantityValue(quantity.value) - 1));
        update();
    });
    plus.addEventListener("click", () => {
        quantity.value = String(Math.min(50, cakeQuantityValue(quantity.value) + 1));
        update();
    });
    form.addEventListener("submit", event => {
        if (form.dataset.submitting === "true") { event.preventDefault(); return; }
        // Trình duyệt kiểm tra required/min/max/step trước khi phát sự kiện submit.
        form.dataset.submitting = "true";
        addButton.disabled = true;
        addButton.textContent = "Đang thêm vào giỏ…";
    });
    window.addEventListener("pageshow", () => {
        delete form.dataset.submitting;
        addButton.disabled = false;
        addButton.textContent = "Thêm vào giỏ hàng →";
        update();
    });
    update();
});

