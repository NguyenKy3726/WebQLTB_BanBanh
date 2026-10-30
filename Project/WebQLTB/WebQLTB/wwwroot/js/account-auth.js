(() => {
    const shell = document.querySelector("[data-auth-shell]");
    if (!shell) return;

    const registerPanel = shell.querySelector("#register-panel");
    const registerButtons = shell.querySelectorAll("[data-show-register]");
    const loginButtons = shell.querySelectorAll("[data-show-login]");

    function showRegister() {
        shell.classList.add("account-auth-shell--register-mode");
        registerPanel?.setAttribute("aria-hidden", "false");
        registerButtons.forEach(button => button.setAttribute("aria-expanded", "true"));
        shell.querySelector("#register-title")?.focus();
    }

    function showLogin() {
        shell.classList.remove("account-auth-shell--register-mode");
        registerPanel?.setAttribute("aria-hidden", "true");
        registerButtons.forEach(button => button.setAttribute("aria-expanded", "false"));
        shell.querySelector("#login-title")?.focus();
    }

    registerButtons.forEach(button => button.addEventListener("click", showRegister));
    loginButtons.forEach(button => button.addEventListener("click", showLogin));
})();
