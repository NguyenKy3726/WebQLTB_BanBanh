// Các hàm dữ liệu dùng chung cho tìm kiếm, lọc và sắp xếp.
function normalizeCatalogText(value) {
    return String(value ?? "").normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d").replace(/Đ/g, "D")
        .toLocaleLowerCase("vi").replace(/\s+/g, " ").trim();
}

function selectCatalogProducts(products, state) {
    const words = normalizeCatalogText(state.query).split(" ").filter(Boolean);
    const result = products.filter(product =>
        words.every(word => product.search.includes(word))
        && (!state.categories.length || state.categories.includes(product.category))
        && (state.min === null || (product.price !== null && product.price >= state.min))
        && (state.max === null || (product.price !== null && product.price <= state.max))
    );
    return result.sort((a, b) => {
        let compare = 0;
        if (state.sort === "name-asc" || state.sort === "name-desc") {
            compare = a.name.localeCompare(b.name, "vi", { sensitivity: "base", numeric: true });
            if (state.sort === "name-desc") compare *= -1;
        } else if (state.sort === "price-asc" || state.sort === "price-desc") {
            // Bánh không có giá luôn đứng cuối, kể cả khi sắp xếp giảm dần.
            if (a.price === null && b.price !== null) return 1;
            if (b.price === null && a.price !== null) return -1;
            if (a.price !== null && b.price !== null) {
                compare = a.price - b.price;
                if (state.sort === "price-desc") compare *= -1;
            }
        }
        return compare || a.index - b.index;
    });
}

document.addEventListener("DOMContentLoaded", () => {
    const root = document.getElementById("catalog");
    if (!root) return;

    const get = id => document.getElementById(id);
    const grid = get("catalog-grid");
    const panel = get("catalog-filter-panel");
    const backdrop = get("catalog-filter-backdrop");
    const openButton = get("open-catalog-filter");
    const closeButton = get("close-catalog-filter");
    const filterForm = get("catalog-filter-form");
    const searchForm = get("catalog-search-form");
    const searchInput = get("catalog-search-input");
    const sortInput = get("catalog-sort");
    const minInput = get("filter-price-min");
    const maxInput = get("filter-price-max");
    const error = get("catalog-price-error");
    const activeFilters = get("catalog-active-filters");
    const pagination = get("catalog-pagination");
    const checkboxes = Array.from(panel.querySelectorAll(".filter-category"));
    const chips = Array.from(root.querySelectorAll(".catalog-chip"));
    const pageSize = Math.max(1, Number(root.dataset.pageSize) || 10);
    const money = value => value.toLocaleString("vi-VN") + "đ";
    const state = { query: "", categories: [], min: null, max: null, sort: "default", page: 1 };
    const products = Array.from(grid.querySelectorAll(".product-card")).map((element, index) => {
        const name = element.querySelector(".product-card__name")?.textContent.trim() ?? "";
        const description = element.querySelector(".product-card__description")?.textContent ?? "";
        const category = (element.dataset.category ?? "").trim();
        const rawPrice = (element.dataset.price ?? "").trim();
        const price = rawPrice === "" ? null : Number(rawPrice);
        return {
            element, index, name, category,
            price: Number.isFinite(price) ? price : null,
            search: normalizeCatalogText(name + " " + description + " " + category)
        };
    });

    let isOpen = false;
    let previousOverflow = "";

    function syncDraft() {
        checkboxes.forEach(input => { input.checked = state.categories.includes(input.value.trim()); });
        minInput.value = state.min ?? "";
        maxInput.value = state.max ?? "";
        error.hidden = true;
    }

    function openPanel() {
        syncDraft();
        isOpen = true;
        previousOverflow = document.body.style.overflow;
        panel.inert = false;
        panel.setAttribute("aria-hidden", "false");
        panel.classList.add("catalog-filter-panel--open");
        backdrop.hidden = false;
        openButton.setAttribute("aria-expanded", "true");
        document.body.style.overflow = "hidden";
        closeButton.focus();
    }

    function closePanel() {
        if (!isOpen) return;
        isOpen = false;
        panel.classList.remove("catalog-filter-panel--open");
        panel.setAttribute("aria-hidden", "true");
        panel.inert = true;
        backdrop.hidden = true;
        openButton.setAttribute("aria-expanded", "false");
        document.body.style.overflow = previousOverflow;
        openButton.focus();
    }

    function makeButton(label, action, className = "catalog-chip") {
        const button = document.createElement("button");
        button.type = "button";
        button.className = className;
        button.textContent = label;
        button.addEventListener("click", action);
        return button;
    }

    function update() {
        state.page = 1;
        render();
    }

    function render() {
        const matches = selectCatalogProducts(products, state);
        const pages = Math.ceil(matches.length / pageSize);
        state.page = Math.max(1, Math.min(state.page, pages || 1));
        const start = (state.page - 1) * pageSize;
        const end = Math.min(start + pageSize, matches.length);

        products.forEach(product => { product.element.hidden = true; });
        // Sắp xếp chính các phần tử hiện có, giữ nguyên link chi tiết và nội dung thẻ.
        matches.forEach((product, index) => {
            grid.append(product.element);
            product.element.hidden = index < start || index >= end;
        });

        get("catalog-no-results").hidden = matches.length !== 0;
        get("catalog-filter-count").textContent = matches.length + " mẫu bánh";
        root.querySelector(".catalog-tools__count").textContent =
            matches.length + " mẫu bánh" + (matches.length ? " · Hiển thị " + (start + 1) + "–" + end : "");
        chips.forEach(chip => {
            const selected = state.categories.includes(chip.dataset.category.trim());
            chip.classList.toggle("catalog-chip--selected", selected);
            chip.setAttribute("aria-pressed", String(selected));
        });

        activeFilters.replaceChildren();
        const addTag = (label, remove) => {
            const button = makeButton(label + " ×", () => { remove(); syncDraft(); update(); });
            button.setAttribute("aria-label", "Xóa điều kiện: " + label);
            activeFilters.append(button);
        };
        if (state.query) addTag('Từ khóa: "' + state.query + '"', () => {
            state.query = ""; searchInput.value = "";
        });
        state.categories.forEach(category => addTag(category, () => {
            state.categories = state.categories.filter(value => value !== category);
        }));
        if (state.min !== null || state.max !== null) {
            const label = state.min !== null && state.max !== null
                ? money(state.min) + " – " + money(state.max)
                : state.min !== null ? "Từ " + money(state.min) : "Đến " + money(state.max);
            addTag(label, () => { state.min = null; state.max = null; });
        }
        if (activeFilters.childElementCount) {
            activeFilters.append(makeButton("Xóa mọi điều kiện", () => {
                state.query = ""; state.categories = []; state.min = null; state.max = null;
                searchInput.value = ""; syncDraft(); update();
            }, "filter-clear"));
        }

        pagination.replaceChildren();
        pagination.hidden = pages <= 1;
        if (pages > 1) {
            const addPage = (label, page, disabled = false) => {
                const button = makeButton(label, () => {
                    state.page = page; render();
                    const heading = root.querySelector(".catalog-tools__title");
                    heading.focus({ preventScroll: true });
                    heading.scrollIntoView({ behavior: "smooth", block: "center" });
                }, "catalog-page");
                button.disabled = disabled;
                button.setAttribute("aria-label", label === "Trước" || label === "Sau"
                    ? "Trang " + label.toLowerCase() : "Trang " + page);
                if (String(page) === label && page === state.page) button.setAttribute("aria-current", "page");
                pagination.append(button);
            };
            addPage("Trước", state.page - 1, state.page === 1);
            const first = Math.max(1, Math.min(state.page - 2, pages - 4));
            for (let page = first; page <= Math.min(first + 4, pages); page++) addPage(String(page), page);
            addPage("Sau", state.page + 1, state.page === pages);
        }
    }

    searchForm.addEventListener("submit", event => {
        event.preventDefault();
        state.query = searchInput.value.trim();
        update();
    });
    // Khi xóa hết nội dung ô tìm kiếm, trả lại kết quả theo các bộ lọc còn lại.
    searchInput.addEventListener("input", () => {
        if (searchInput.value.trim() === "" && state.query) {
            state.query = ""; update();
        }
    });
    sortInput.addEventListener("change", () => { state.sort = sortInput.value; update(); });
    chips.forEach(chip => chip.addEventListener("click", () => {
        const category = chip.dataset.category.trim();
        state.categories = state.categories.includes(category)
            ? state.categories.filter(value => value !== category)
            : [...state.categories, category];
        syncDraft(); update();
    }));

    filterForm.addEventListener("submit", event => {
        event.preventDefault();
        const min = minInput.value === "" ? null : Number(minInput.value);
        const max = maxInput.value === "" ? null : Number(maxInput.value);
        if (min !== null && max !== null && min > max) {
            error.textContent = "Giá từ phải nhỏ hơn hoặc bằng giá đến.";
            error.hidden = false;
            minInput.focus();
            return;
        }
        state.categories = checkboxes.filter(input => input.checked).map(input => input.value.trim());
        state.min = min; state.max = max;
        update(); closePanel();
    });
    [minInput, maxInput].forEach(input => input.addEventListener("input", () => { error.hidden = true; }));
    get("clear-catalog-filter").addEventListener("click", () => {
        // Xóa danh mục/giá đang áp dụng; giữ nguyên từ khóa tìm kiếm.
        state.categories = []; state.min = null; state.max = null;
        syncDraft(); update();
    });
    openButton.addEventListener("click", openPanel);
    closeButton.addEventListener("click", closePanel);
    backdrop.addEventListener("click", closePanel);
    document.addEventListener("keydown", event => {
        if (!isOpen) return;
        if (event.key === "Escape") { event.preventDefault(); closePanel(); }
        if (event.key === "Tab") {
            const controls = Array.from(panel.querySelectorAll("button, input, select, a[href]"))
                .filter(element => !element.disabled && !element.hidden);
            const first = controls[0], last = controls[controls.length - 1];
            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault(); last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault(); first.focus();
            }
        }
    });
    render();
});

