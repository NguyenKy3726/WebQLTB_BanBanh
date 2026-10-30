document.addEventListener("DOMContentLoaded", () => {
    const picker = document.querySelector("[data-image-picker]");
    const preview = document.querySelector("[data-image-preview]");
    const pathInput = document.querySelector("input[name='DuongDanAnh']");
    if (!picker || !preview) return;

    picker.addEventListener("change", () => {
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
});
