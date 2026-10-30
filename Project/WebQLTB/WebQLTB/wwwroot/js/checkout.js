(() => {
    "use strict";

    const form = document.getElementById("checkout-form");
    if (!form) return;

    const note = document.getElementById("checkout-note");
    const count = document.getElementById("checkout-note-count");
    const submit = document.getElementById("checkout-submit");

    const updateNoteCount = () => {
        if (note && count) count.textContent = String(note.value.length);
    };

    note?.addEventListener("input", updateNoteCount);
    updateNoteCount();

    form.addEventListener("submit", (event) => {
        if (!form.checkValidity()) {
            event.preventDefault();
            form.reportValidity();
            return;
        }

        if (submit) {
            submit.disabled = true;
            submit.setAttribute("aria-busy", "true");
            submit.innerHTML = "Đang gửi đơn…";
        }
    });

    // Cho phép thử gửi lại nếu trình duyệt quay lại trang qua lịch sử.
    window.addEventListener("pageshow", () => {
        if (!submit) return;
        submit.disabled = false;
        submit.removeAttribute("aria-busy");
        submit.innerHTML = 'Gửi đơn đặt bánh <span aria-hidden="true">→</span>';
    });
})();
