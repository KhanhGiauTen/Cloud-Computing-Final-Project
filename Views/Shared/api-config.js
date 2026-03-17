window.CloudContactConfig = {
    apiBaseUrl: (localStorage.getItem('cloudContactApiBaseUrl') || 'http://3.27.250.40').replace(/\/$/, ''),
    tokenStorageKey: 'cloudContactJwtToken',
    parseJwt(token) {
        try {
            const part = token.split('.')[1];
            if (!part) {
                return null;
            }

            const base64 = part.replace(/-/g, '+').replace(/_/g, '/');
            const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
            return JSON.parse(atob(padded));
        } catch {
            return null;
        }
    },
    isTokenExpired(token) {
        const payload = this.parseJwt(token);
        if (!payload || !payload.exp) {
            return true;
        }

        const now = Math.floor(Date.now() / 1000);
        return payload.exp <= now;
    },
    getToken() {
        return localStorage.getItem(this.tokenStorageKey) || '';
    },
    isAuthenticated() {
        const token = this.getToken();
        if (!token) {
            return false;
        }

        if (this.isTokenExpired(token)) {
            localStorage.removeItem(this.tokenStorageKey);
            return false;
        }

        return true;
    },
    logout(redirectUrl) {
        localStorage.removeItem(this.tokenStorageKey);
        if (redirectUrl) {
            window.location.href = redirectUrl;
        }
    },
    requireAuth(loginUrl) {
        if (!this.isAuthenticated()) {
            window.location.href = loginUrl;
            return false;
        }
        return true;
    },
    enableQuickLogout(loginUrl) {
        if (!this.isAuthenticated()) {
            return;
        }

        if (document.getElementById('quickLogoutBtn')) {
            return;
        }

        const button = document.createElement('button');
        button.id = 'quickLogoutBtn';
        button.type = 'button';
        button.innerHTML = '⎋ Logout';
        button.style.position = 'fixed';
        button.style.right = '16px';
        button.style.bottom = '16px';
        button.style.zIndex = '9999';
        button.style.border = 'none';
        button.style.borderRadius = '999px';
        button.style.padding = '10px 14px';
        button.style.background = '#dc3545';
        button.style.color = '#fff';
        button.style.fontWeight = '600';
        button.style.boxShadow = '0 8px 20px rgba(0,0,0,0.2)';
        button.style.cursor = 'pointer';
        button.addEventListener('click', () => this.logout(loginUrl));

        document.body.appendChild(button);
    },
    buildAuthHeaders(extraHeaders) {
        const headers = { ...(extraHeaders || {}) };
        const token = this.getToken();
        if (token) {
            headers.Authorization = `Bearer ${token}`;
        }
        return headers;
    },
    authFetch(url, options) {
        const requestOptions = { ...(options || {}) };
        requestOptions.headers = this.buildAuthHeaders(requestOptions.headers);
        return fetch(url, requestOptions).then(response => {
            if (response.status === 401) {
                this.logout('../Home/Login.html');
            }
            return response;
        });
    }
};
