let timeoutId;

function showPassword(inputElement, button) {
    if (inputElement.type === 'password') {
        inputElement.type = 'text';
        clearTimeout(timeoutId);
        timeoutId = setTimeout(function () {
            inputElement.type = 'password';
        }, 5000);
    } else {
        inputElement.type = 'password';
        clearTimeout(timeoutId);
    }
}

const form = document.getElementById('login-form');

if (form) {
    form.addEventListener('submit', function () {

        const btn = form.querySelector('button[type="submit"]');
        if (btn) {
            btn.disabled = true;
            btn.innerText = "...";
        }
    });
}

(function () {
    const errorDiv = document.getElementById('error-message-container');
    if (errorDiv) {
        setTimeout(() => {
            errorDiv.classList.add('hide-drawer');
            
            setTimeout(() => {
                errorDiv.remove();
            }, 600);
        }, 5000); 
    }
})();