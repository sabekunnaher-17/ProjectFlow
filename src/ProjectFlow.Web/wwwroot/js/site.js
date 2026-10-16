document.querySelectorAll('.app-sidebar nav a').forEach(link => {
    if (link.pathname.toLowerCase() === window.location.pathname.toLowerCase()) link.setAttribute('aria-current', 'page');
});

document.addEventListener('submit', event => {
    const message = event.target.dataset.confirm;
    if (message && !window.confirm(message)) event.preventDefault();
});

document.addEventListener('submit', async event => {
    const form = event.target;
    if (!form.dataset.taskStatus || event.defaultPrevented) return;
    event.preventDefault();
    const button = form.querySelector('button');
    const feedback = document.getElementById('async-feedback');
    button.disabled = true;
    feedback.textContent = 'Saving status…';
    try {
        const data = new FormData(form);
        const response = await fetch(`/api/v1/tasks/${form.dataset.taskStatus}/status`, {
            method: 'PUT', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': document.querySelector('meta[name="csrf-token"]').content },
            body: JSON.stringify({ status: Number(data.get('Status')), version: data.get('Version') })
        });
        if (!response.ok) {
            const problem = await response.json().catch(() => ({}));
            throw new Error(problem.title || 'Unable to save. Reload the page and try again.');
        }
        window.location.reload();
    } catch (error) {
        feedback.textContent = error.message;
        feedback.className = 'alert alert-danger mt-3';
        button.disabled = false;
    }
});
