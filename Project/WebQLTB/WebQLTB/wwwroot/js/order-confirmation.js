document.addEventListener("DOMContentLoaded", () => {
    const button = document.getElementById("copy-transfer-content");
    const input = document.getElementById("transfer-content");
    const status = document.getElementById("copy-transfer-status");
    if (!button || !input || !status) return;
    button.addEventListener("click", async () => {
        try {
            if (!navigator.clipboard?.writeText) throw new Error("Clipboard unavailable");
            await navigator.clipboard.writeText(input.value);
            status.textContent = "Đã sao chép mã đơn. Hãy dán vào nội dung chuyển khoản.";
        } catch {
            // HTTP/LAN có thể không hỗ trợ Clipboard API: chọn sẵn đúng mã để khách Ctrl+C.
            input.focus();
            input.select();
            input.setSelectionRange(0, input.value.length);
            status.textContent = "Hãy nhấn Ctrl+C hoặc chạm giữ để sao chép mã đơn đã chọn.";
        }
    });
});
