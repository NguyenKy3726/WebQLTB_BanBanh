document.addEventListener("DOMContentLoaded", function () {
    const burger = document.querySelector(".header__burger");
    const nav = document.querySelector(".header__nav");

    function setMenuOpen(open) {
        nav.classList.toggle("header__nav--open", open);

        burger.setAttribute("aria-expanded", String(open));
        burger.setAttribute(
            "aria-label",
            open ? "Đóng menu" : "Mở menu"
        );
    }

    if (burger && nav) {
        burger.addEventListener("click", function () {
            const open = !nav.classList.contains("header__nav--open");
            setMenuOpen(open);
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape"
                && nav.classList.contains("header__nav--open")) {
                setMenuOpen(false);
                burger.focus();
            }
        });
    }

    const year = document.getElementById("footer-year");

    if (year) {
        year.textContent = new Date().getFullYear();
    }
});

