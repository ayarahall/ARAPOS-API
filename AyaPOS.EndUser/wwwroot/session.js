window.ayaposSession = {
    get: function (key) {
        const raw = window.localStorage.getItem(key);
        return raw ? JSON.parse(raw) : null;
    },
    set: function (key, value) {
        window.localStorage.setItem(key, JSON.stringify(value));
    },
    remove: function (key) {
        window.localStorage.removeItem(key);
    }
};

window.ayaposUi = {
    applyLanguage: function (language) {
        const normalized = language === "en" ? "en" : "ar";
        document.documentElement.lang = normalized;
        document.documentElement.dir = normalized === "ar" ? "rtl" : "ltr";
        document.body.classList.toggle("ui-rtl", normalized === "ar");
        document.body.classList.toggle("ui-ltr", normalized !== "ar");
    }
};

window.ayaposPrint = {
    printCurrentPage: function () {
        window.print();
    }
};
