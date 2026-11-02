document.addEventListener("DOMContentLoaded", () => {
    const picker = document.querySelector("[data-image-picker]");
    const preview = document.querySelector("[data-image-preview]");
    const pathInput = document.querySelector("input[name='DuongDanAnh']");
    if (picker && preview) picker.addEventListener("change", () => {
        const file = picker.files?.[0];
        if (!file) {
            preview.hidden = true;
            preview.removeAttribute("src");
            return;
        }
        preview.src = URL.createObjectURL(file);
        preview.hidden = false;
        if (pathInput) pathInput.value = "";
    });

    const productSelect = document.querySelector("[data-image-product]");
    const orderInput = document.querySelector("[data-image-order]");
    if (productSelect && orderInput) {
        productSelect.addEventListener("change", () => {
            const selectedOption = productSelect.selectedOptions[0];
            orderInput.value = selectedOption?.dataset.nextOrder || "1";
        });
    }
});
