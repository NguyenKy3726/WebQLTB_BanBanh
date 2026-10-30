(() => {
    const scriptUrl = document.currentScript?.src || window.location.href;
    const fallbackUrl = new URL("../assets/images/image-unavailable.svg", scriptUrl).href;

    function useFallback(image) {
        if (!(image instanceof HTMLImageElement) || image.dataset.fallbackApplied === "true") return;
        image.dataset.fallbackApplied = "true";
        image.classList.add("image-fallback");
        image.alt = "Hình ảnh bánh đang được cập nhật";
        image.src = fallbackUrl;
    }

    document.addEventListener("error", event => useFallback(event.target), true);

    document.querySelectorAll("img").forEach(image => {
        if (image.complete && image.naturalWidth === 0) useFallback(image);
    });
})();
