// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
window.addEventListener('load', function () {
    const cookiePopup = document.getElementById('cookiePopup');
    const acceptButton = document.getElementById('acceptCookies');

    if (!localStorage.getItem('cookiesAccepted')) {
        cookiePopup.style.display = 'flex';
    }

    acceptButton.addEventListener('click', function () {
        localStorage.setItem('cookiesAccepted', 'true');
        cookiePopup.style.display = 'none';
    });
});
