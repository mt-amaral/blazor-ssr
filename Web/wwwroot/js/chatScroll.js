window.chatScroll = window.chatScroll || {};

window.chatScroll.getTop = (el) => el ? el.scrollTop : 999999;

window.chatScroll.toBottom = (el) => {
    if (!el) return;
    el.scrollTop = el.scrollHeight;
};