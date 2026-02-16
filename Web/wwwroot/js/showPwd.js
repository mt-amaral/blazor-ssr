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