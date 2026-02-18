window.chatScroll = {
    toBottom: function (el) {
        if (!el) return;
        el.scrollTop = el.scrollHeight;
    }
};