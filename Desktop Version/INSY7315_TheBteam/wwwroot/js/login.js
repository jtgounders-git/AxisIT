// Add focus animations
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.input-field').forEach(field => {
        field.addEventListener('focus', function () {
            this.parentElement.style.transform = 'scale(1.02)';
        });

        field.addEventListener('blur', function () {
            this.parentElement.style.transform = 'scale(1)';
        });
    });

    // Add submit button animation
    const submitBtn = document.querySelector('.submit-btn');
    if (submitBtn) {
        submitBtn.addEventListener('click', function (e) {
            this.style.transform = 'scale(0.95)';
            setTimeout(() => {
                this.style.transform = 'scale(1)';
            }, 150);
        });
    }
});