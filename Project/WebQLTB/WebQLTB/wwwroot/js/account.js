(() => {
    "use strict";

    document.querySelectorAll("[data-password-toggle]").forEach((button) => {
        button.addEventListener("click", () => {
            const targetId = button.getAttribute("data-password-toggle");
            const input = targetId ? document.getElementById(targetId) : null;
            if (!input) return;

            const showPassword = input.type === "password";
            input.type = showPassword ? "text" : "password";
            button.textContent = showPassword ? "Ẩn" : "Hiện";
            button.setAttribute("aria-label", showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu");
        });
    });
})();