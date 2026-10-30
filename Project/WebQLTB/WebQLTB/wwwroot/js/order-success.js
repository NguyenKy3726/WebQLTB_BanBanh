(() => {
    "use strict";

    const button = document.getElementById("copy-order-code");
    const codeElement = document.getElementById("order-code");
    const status = document.getElementById("copy-order-status");

    if (!button || !codeElement || !status) return;

    button.addEventListener("click", async () => {
        const code = codeElement.dataset.orderCode || codeElement.textContent.trim();

        try {
            if (navigator.clipboard?.writeText) {
                await navigator.clipboard.writeText(code);
            } else {
                const temporaryInput = document.createElement("textarea");
                temporaryInput.value = code;
                temporaryInput.setAttribute("readonly", "");
                temporaryInput.style.position = "fixed";
                temporaryInput.style.opacity = "0";
                document.body.appendChild(temporaryInput);
                temporaryInput.select();
                const copied = document.execCommand("copy");
                temporaryInput.remove();
                if (!copied) throw new Error("Clipboard is unavailable");
            }

            status.textContent = "Đã sao chép mã đơn.";
        } catch {
            status.textContent = "Không sao chép được. Bạn có thể chọn và sao chép mã ở trên.";
        }
    });
})();