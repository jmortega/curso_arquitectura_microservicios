// wwwroot/swagger-auth.js
window.addEventListener('load', () => {
    setTimeout(async () => {
        const topbar = document.querySelector('.topbar-wrapper');
        if (!topbar) return;

        const container = document.createElement('div');
        container.style.cssText = 'display:flex; align-items:center; gap:12px; margin-left:auto;';

        const statusEl = document.createElement('span');
        statusEl.className = 'auth-status';
        statusEl.textContent = 'Comprobando sesión...';

        const loginBtn = document.createElement('button');
        loginBtn.className = 'github-login-btn';

        try {
            const res  = await fetch('/auth/status');
            const data = await res.json();

            if (data.authenticated) {
                statusEl.textContent = `✓ ${data.username}`;
                loginBtn.textContent = 'Cerrar sesión';
                loginBtn.onclick = () => window.location.href = '/auth/logout';
            } else {
                statusEl.textContent = 'No autenticado';
                loginBtn.textContent = 'Login con GitHub';
                loginBtn.onclick = () => window.location.href = '/auth/login';
            }
        } catch {
            statusEl.textContent = 'Error de conexión';
        }

        container.appendChild(statusEl);
        container.appendChild(loginBtn);
        topbar.appendChild(container);
    }, 500);
});