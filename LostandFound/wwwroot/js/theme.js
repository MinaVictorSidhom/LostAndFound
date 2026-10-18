(() => {
    const storageKey = "lost-and-found-theme";
    const root = document.documentElement;

    let savedTheme = null;
    try {
        savedTheme = localStorage.getItem(storageKey);
    } catch {
        // The toggle still works for this page if browser storage is unavailable.
    }

    const initialTheme = savedTheme === "light" || savedTheme === "dark"
        ? savedTheme
        : (window.matchMedia?.("(prefers-color-scheme: dark)").matches ? "dark" : "light");

    function applyTheme(theme, save = false) {
        const isDark = theme === "dark";
        root.dataset.theme = isDark ? "dark" : "light";
        document.querySelectorAll("[data-theme-toggle]").forEach(button => {
            button.setAttribute("aria-label", `Switch to ${isDark ? "light" : "dark"} theme`);
            button.setAttribute("aria-pressed", String(isDark));
            const icon = button.querySelector(".theme-toggle-icon");
            const label = button.querySelector(".theme-toggle-label");
            if (icon) icon.textContent = isDark ? "☀" : "☾";
            if (label) label.textContent = isDark ? "Light mode" : "Dark mode";
        });

        if (save) {
            try {
                localStorage.setItem(storageKey, theme);
            } catch {
                // Keep the current-page preference even when browser storage is unavailable.
            }
        }
    }

    applyTheme(initialTheme);
    document.querySelectorAll("[data-theme-toggle]").forEach(button => {
        button.addEventListener("click", () => {
            applyTheme(root.dataset.theme === "dark" ? "light" : "dark", true);
        });
    });
})();
