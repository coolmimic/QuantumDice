// API 基础配置 - 动态获取当前域名
const API_BASE = window.location.origin + '/api';

// 获取存储的 Token
function getToken() {
    const user = localStorage.getItem('user');
    if (user) {
        const { token } = JSON.parse(user);
        return token;
    }
    return null;
}

// API 请求封装
const api = {
    // 通用请求方法
    async request(endpoint, options = {}) {
        const url = `${API_BASE}${endpoint}`;
        const token = getToken();

        const config = {
            headers: {
                'Content-Type': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
                ...options.headers
            },
            ...options
        };

        try {
            const response = await fetch(url, config);

            // 处理 401 未授权
            if (response.status === 401) {
                // 如果是登录接口，直接返回错误信息，不强制刷新
                if (endpoint.includes('/login')) {
                    const data = await response.json();
                    return data;
                }

                localStorage.removeItem('user');
                window.location.reload();
                return { success: false, message: '登录已过期' };
            }

            const data = await response.json();
            return data;
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    },

    // GET 请求
    async get(endpoint) {
        return this.request(endpoint, { method: 'GET' });
    },

    // POST 请求
    async post(endpoint, body) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(body)
        });
    },

    // PUT 请求
    async put(endpoint, body) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(body)
        });
    },

    // DELETE 请求
    async delete(endpoint) {
        return this.request(endpoint, { method: 'DELETE' });
    },

    // ========== 认证接口 ==========
    auth: {
        // 超管登录
        adminLogin: (data) => api.post('/auth/admin/login', data),

        // 庄家登录
        dealerLogin: (data) => api.post('/auth/dealer/login', data),

        // 验证 Token
        verify: () => api.post('/auth/verify')
    },

    // ========== 超管接口 ==========
    admin: {
        getDealers: () => api.get('/admin/dealers'),
        getDealer: (id) => api.get(`/admin/dealers/${id}`),
        createDealer: (data) => api.post('/admin/dealers', data),
        updateDealer: (id, data) => api.put(`/admin/dealers/${id}`, data),
        extendSubscription: (id, data) => api.post(`/admin/dealers/${id}/extend`, data),
        disableDealer: (id) => api.post(`/admin/dealers/${id}/disable`),
        enableDealer: (id) => api.post(`/admin/dealers/${id}/enable`)
    },

    // ========== 庄家接口 ==========
    dealer: {
        getGroups: (dealerId) => api.get(`/dealer/groups?dealerId=${dealerId}`),
        getGroup: (groupId) => api.get(`/dealer/groups/${groupId}`),
        bindGroup: (dealerId, data) => api.post(`/dealer/groups?dealerId=${dealerId}`, data),
        generateBindingCode: (dealerId) => api.post(`/dealer/groups/binding-code?dealerId=${dealerId}`),
        unbindGroup: (groupId) => api.delete(`/dealer/groups/${groupId}`),
        getOddsConfig: (groupId) => api.get(`/dealer/groups/${groupId}/odds`),
        updateOddsConfig: (groupId, data) => api.put(`/dealer/groups/${groupId}/odds`, data),
        getPlayers: (groupId) => api.get(`/dealer/players?groupId=${groupId}`),
        getPlayer: (playerId) => api.get(`/dealer/players/${playerId}`),
        deposit: (playerId, operatorId, data) =>
            api.post(`/dealer/players/${playerId}/deposit?operatorId=${operatorId}`, data),
        withdraw: (playerId, operatorId, data) =>
            api.post(`/dealer/players/${playerId}/withdraw?operatorId=${operatorId}`, data),
        banPlayer: (playerId) => api.post(`/dealer/players/${playerId}/ban`),
        unbanPlayer: (playerId) => api.post(`/dealer/players/${playerId}/unban`)
    }
};
