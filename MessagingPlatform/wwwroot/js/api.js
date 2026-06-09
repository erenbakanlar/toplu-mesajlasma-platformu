// ─── API İstemcisi ─────────────────────────────────────────────────
const API = (() => {
    const BASE = ''; // Aynı origin

    function getToken() {
        return localStorage.getItem('token');
    }

    async function request(method, path, body) {
        const headers = { 'Content-Type': 'application/json' };
        const token = getToken();
        if (token) headers['Authorization'] = `Bearer ${token}`;

        const opts = { method, headers };
        if (body !== undefined) opts.body = JSON.stringify(body);

        const res = await fetch(BASE + path, opts);

        if (res.status === 401) {
            // Token geçersiz → çıkış
            localStorage.clear();
            location.reload();
            throw new Error('Oturum süresi doldu');
        }

        let data = null;
        const text = await res.text();
        if (text) {
            try { data = JSON.parse(text); } catch { data = text; }
        }

        if (!res.ok) {
            const msg = (data && (data.message || (data.errors && data.errors.join(', ')))) || 'İşlem başarısız';
            throw new Error(msg);
        }
        return data;
    }

    return {
        getToken,

        // Auth
        login: (email, password) => request('POST', '/api/auth/login', { email, password }),
        register: (dto) => request('POST', '/api/auth/register', dto),

        // Users
        getMembers: () => request('GET', '/api/users/members'),
        getAdmins: () => request('GET', '/api/users/admins'),
        getAllUsers: () => request('GET', '/api/users'),

        // Messages (birebir)
        sendMessage: (receiverId, content) => request('POST', '/api/messages', { receiverId, content }),
        getConversation: (userId) => request('GET', `/api/messages/conversation/${userId}`),
        getMyMessages: () => request('GET', '/api/messages/my'),
        deleteMessage: (id) => request('DELETE', `/api/messages/${id}`),

        // Groups
        getGroups: () => request('GET', '/api/groups'),
        getGroup: (id) => request('GET', `/api/groups/${id}`),
        createGroup: (dto) => request('POST', '/api/groups', dto),
        addMembers: (id, userIds) => request('POST', `/api/groups/${id}/members`, { userIds }),
        removeMember: (id, userId) => request('DELETE', `/api/groups/${id}/members/${userId}`),
        sendGroupMessage: (id, content) => request('POST', `/api/groups/${id}/messages`, { content }),
        getGroupMessages: (id) => request('GET', `/api/groups/${id}/messages`),
        deleteGroupMessage: (groupId, messageId) => request('DELETE', `/api/groups/${groupId}/messages/${messageId}`),
        deleteGroup: (id) => request('DELETE', `/api/groups/${id}`),
    };
})();
