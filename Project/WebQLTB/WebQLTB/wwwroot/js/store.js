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
document.addEventListener("DOMContentLoaded", function () {
    const panel = document.getElementById("catalog-filter-panel");
    const backdrop = document.getElementById("catalog-filter-backdrop");
    const openButton = document.getElementById("open-catalog-filter");
    const closeButton = document.getElementById("close-catalog-filter");
    const applyButton = document.getElementById("apply-catalog-filter");
    const clearButton = document.getElementById("clear-catalog-filter");

    const minInput = document.getElementById("filter-price-min");
    const maxInput = document.getElementById("filter-price-max");
    const noResults = document.getElementById("catalog-no-results");

    const products = Array.from(
        document.querySelectorAll(".product-card[data-category]")
    );

    function openFilterPanel() {
        panel?.classList.add("catalog-filter-panel--open");
        panel?.setAttribute("aria-hidden", "false");

        if (backdrop) backdrop.hidden = false;
        openButton?.setAttribute("aria-expanded", "true");
        document.body.style.overflow = "hidden";
    }

    function closeFilterPanel() {
        panel?.classList.remove("catalog-filter-panel--open");
        panel?.setAttribute("aria-hidden", "true");

        if (backdrop) backdrop.hidden = true;
        openButton?.setAttribute("aria-expanded", "false");
        document.body.style.overflow = "";
    }

    function applyFilters() {
        const selectedCategories = Array.from(
            document.querySelectorAll(".filter-category:checked")
        ).map(input => input.value);

        const category = product.dataset.category;

        const matchesCategory =
            selectedCategories.length === 0
            || selectedCategories.includes(category);

        const minPrice = minInput?.value
            ? Number(minInput.value)
            : null;

        const maxPrice = maxInput?.value
            ? Number(maxInput.value)
            : null;

        let visibleCount = 0;

        products.forEach(product => {
            const category = product.dataset.category;
            const price = product.dataset.price
                ? Number(product.dataset.price)
                : null;

            const matchesCategory =
                selectedCategories.length === 0
                || selectedCategories.includes(category);

            const matchesMinPrice =
                minPrice === null
                || (price !== null && price >= minPrice);

            const matchesMaxPrice =
                maxPrice === null
                || (price !== null && price <= maxPrice);

            const shouldShow =
                matchesCategory && matchesMinPrice && matchesMaxPrice;

            product.hidden = !shouldShow;

            if (shouldShow) visibleCount++;
        });

        if (noResults) {
            noResults.hidden = visibleCount !== 0;
        }

        const count = document.querySelector(".catalog-tools__count");

        if (count) {
            count.textContent = `${visibleCount} mẫu bánh`;
        }

        closeFilterPanel();
    }

    function clearFilters() {
        document.querySelectorAll(".filter-category:checked")
            .forEach(input => {
                input.checked = false;
            });

        if (minInput) minInput.value = "";
        if (maxInput) maxInput.value = "";

        applyFilters();
    }

    openButton?.addEventListener("click", openFilterPanel);
    closeButton?.addEventListener("click", closeFilterPanel);
    applyButton?.addEventListener("click", applyFilters);
    clearButton?.addEventListener("click", clearFilters);
    backdrop?.addEventListener("click", closeFilterPanel);

    document.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            closeFilterPanel();
        }
    });
});