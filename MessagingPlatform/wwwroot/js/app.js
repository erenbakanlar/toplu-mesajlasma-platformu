// ─── Uygulama Durumu ───────────────────────────────────────────────
const State = {
    user: null,          // { userId, email, fullName, role }
    members: [],         // tüm üyeler
    groups: [],          // tüm gruplar
    activeChat: null,    // { type: 'user'|'group', id, name }
    connection: null,    // SignalR
};

// ─── Kısa DOM yardımcıları ─────────────────────────────────────────
const $ = (sel) => document.querySelector(sel);
const $$ = (sel) => document.querySelectorAll(sel);

function initials(name) {
    return (name || '?').trim().split(/\s+/).map(w => w[0]).slice(0, 2).join('').toUpperCase();
}
function fmtTime(iso) {
    const d = new Date(iso);
    return d.toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
}
function escapeHtml(s) {
    const div = document.createElement('div');
    div.textContent = s;
    return div.innerHTML;
}

// ─── Toast bildirimleri ────────────────────────────────────────────
function toast(msg, type = 'info') {
    const el = document.createElement('div');
    el.className = `toast ${type}`;
    el.textContent = msg;
    $('#toast-container').appendChild(el);
    setTimeout(() => {
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.3s';
        setTimeout(() => el.remove(), 300);
    }, 3000);
}

// ─── Oturum ────────────────────────────────────────────────────────
function saveSession(auth) {
    localStorage.setItem('token', auth.token);
    localStorage.setItem('user', JSON.stringify({
        userId: auth.userId,
        email: auth.email,
        fullName: auth.fullName,
        role: auth.role,
    }));
}
function loadSession() {
    const token = localStorage.getItem('token');
    const user = localStorage.getItem('user');
    if (token && user) {
        State.user = JSON.parse(user);
        return true;
    }
    return false;
}
function logout() {
    if (State.connection) State.connection.stop();
    localStorage.clear();
    location.reload();
}

// ─── Giriş ─────────────────────────────────────────────────────────
$('#login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const email = $('#login-email').value.trim();
    const password = $('#login-password').value;
    const errBox = $('#login-error');
    errBox.classList.add('hidden');

    try {
        const auth = await API.login(email, password);
        saveSession(auth);
        State.user = { userId: auth.userId, email: auth.email, fullName: auth.fullName, role: auth.role };
        await startApp();
    } catch (err) {
        errBox.textContent = err.message;
        errBox.classList.remove('hidden');
    }
});

$('#logout-btn').addEventListener('click', logout);

// ─── Uygulama Başlat ───────────────────────────────────────────────
async function startApp() {
    $('#login-screen').classList.add('hidden');
    $('#app-screen').classList.remove('hidden');

    $('#current-user').textContent = State.user.fullName;
    const badge = $('#user-badge');
    badge.textContent = State.user.role;
    badge.classList.toggle('user', State.user.role !== 'Admin');

    const isAdmin = State.user.role === 'Admin';

    // Üye olmayanlar için gruplar sekmesini ve oluşturma butonunu gizle gibi davranabiliriz
    // ancak gruplar API'si admin gerektirdiğinden normal üyeye sadece "mesajlarım" gösterilir.
    if (!isAdmin) {
        await initMemberView();
    } else {
        await initAdminView();
    }

    await connectSignalR();
}

// ─── ADMIN GÖRÜNÜMÜ ────────────────────────────────────────────────
async function initAdminView() {
    await Promise.all([loadMembers(), loadGroups()]);
    setupTabs();
    setupGroupModal();
}

async function loadMembers() {
    try {
        State.members = await API.getMembers();
        renderMembers();
    } catch (err) {
        toast(err.message, 'error');
    }
}

function renderMembers() {
    const list = $('#members-list');
    if (State.members.length === 0) {
        list.innerHTML = '<div class="empty-hint">Henüz üye yok.</div>';
        return;
    }
    list.innerHTML = State.members.map(m => `
        <div class="list-item" data-user-id="${m.id}" onclick="openUserChat('${m.id}')">
            <div class="avatar">${initials(m.fullName)}</div>
            <div class="list-item-info">
                <div class="list-item-name">${escapeHtml(m.fullName)}</div>
                <div class="list-item-sub">${escapeHtml(m.email)}</div>
            </div>
        </div>
    `).join('');
}

async function loadGroups() {
    try {
        State.groups = await API.getGroups();
        renderGroups();
    } catch (err) {
        toast(err.message, 'error');
    }
}

function renderGroups() {
    const list = $('#groups-list');
    if (State.groups.length === 0) {
        list.innerHTML = '<div class="empty-hint">Henüz grup yok. Yeni grup oluşturun.</div>';
        return;
    }
    list.innerHTML = State.groups.map(g => `
        <div class="list-item" data-group-id="${g.id}" onclick="openGroupChat(${g.id})">
            <div class="avatar group-avatar">${initials(g.name)}</div>
            <div class="list-item-info">
                <div class="list-item-name">${escapeHtml(g.name)}</div>
                <div class="list-item-sub">${g.memberCount} üye</div>
            </div>
        </div>
    `).join('');
}

// ─── Sekme geçişi ──────────────────────────────────────────────────
function setupTabs() {
    $$('.tab-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            $$('.tab-btn').forEach(b => b.classList.remove('active'));
            $$('.tab-content').forEach(c => c.classList.remove('active'));
            btn.classList.add('active');
            $(`#tab-${btn.dataset.tab}`).classList.add('active');
        });
    });
}

// ─── Birebir Sohbet ────────────────────────────────────────────────
async function openUserChat(userId) {
    const member = State.members.find(m => m.id === userId);
    if (!member) return;

    State.activeChat = { type: 'user', id: userId, name: member.fullName };
    highlightActive(`[data-user-id="${userId}"]`);

    $('#empty-state').classList.add('hidden');
    $('#chat-view').classList.remove('hidden');
    $('#chat-title').textContent = member.fullName;
    $('#chat-subtitle').textContent = member.email;
    $('#chat-actions').innerHTML = '';

    try {
        const messages = await API.getConversation(userId);
        renderMessages(messages.map(toUnified));
    } catch (err) {
        toast(err.message, 'error');
    }
}

// ─── Grup Sohbeti ──────────────────────────────────────────────────
async function openGroupChat(groupId) {
    const group = State.groups.find(g => g.id === groupId);
    if (!group) return;

    State.activeChat = { type: 'group', id: groupId, name: group.name };
    highlightActive(`[data-group-id="${groupId}"]`);

    // SignalR grup odasına katıl
    if (State.connection && State.connection.state === 'Connected') {
        try { await State.connection.invoke('JoinGroup', groupId); } catch {}
    }

    $('#empty-state').classList.add('hidden');
    $('#chat-view').classList.remove('hidden');
    $('#chat-title').textContent = group.name;
    $('#chat-subtitle').textContent = `${group.memberCount} üye${group.description ? ' • ' + group.description : ''}`;
    $('#chat-actions').innerHTML = `
        <button class="btn btn-ghost btn-sm" onclick="openManageModal(${groupId})">Üyeler</button>
        <button class="btn btn-danger btn-sm" onclick="removeGroup(${groupId})">Sil</button>
    `;

    try {
        const messages = await API.getGroupMessages(groupId);
        renderMessages(messages.map(toUnifiedGroup));
    } catch (err) {
        toast(err.message, 'error');
    }
}

function highlightActive(selector) {
    $$('.list-item').forEach(i => i.classList.remove('active'));
    const el = $(selector);
    if (el) el.classList.add('active');
}

// ─── Mesaj Normalizasyon ───────────────────────────────────────────
function toUnified(m) {
    return {
        id: m.id,
        senderId: m.senderId,
        senderName: m.senderName,
        content: m.content,
        sentAt: m.sentAt,
        mine: m.senderId === State.user.userId,
    };
}
function toUnifiedGroup(m) {
    return {
        id: m.id,
        senderId: m.senderId,
        senderName: m.senderName,
        content: m.content,
        sentAt: m.sentAt,
        mine: m.senderId === State.user.userId,
    };
}

// Mesajı gönderen veya yönetici silebilir
function canDelete(m) {
    return m.mine || State.user.role === 'Admin';
}

function messageHtml(m) {
    const delBtn = canDelete(m)
        ? `<button class="msg-del" title="Mesajı sil" onclick="deleteMessageUI(${m.id}, this)">&times;</button>`
        : '';
    return `
        <div class="message ${m.mine ? 'out' : 'in'}" data-msg-id="${m.id}">
            ${!m.mine ? `<div class="message-sender">${escapeHtml(m.senderName)}</div>` : ''}
            <div class="message-body">${escapeHtml(m.content)}</div>
            <div class="message-time">${fmtTime(m.sentAt)}</div>
            ${delBtn}
        </div>`;
}

function renderMessages(messages) {
    const container = $('#messages-container');
    if (!messages || messages.length === 0) {
        container.innerHTML = '<div class="empty-hint">Henüz mesaj yok. İlk mesajı gönderin.</div>';
        return;
    }
    container.innerHTML = messages.map(messageHtml).join('');
    container.scrollTop = container.scrollHeight;
}

function appendMessage(m) {
    const container = $('#messages-container');
    const hint = container.querySelector('.empty-hint');
    if (hint) container.innerHTML = '';
    container.insertAdjacentHTML('beforeend', messageHtml(m));
    container.scrollTop = container.scrollHeight;
}

// Mesaj silme (aktif sohbet türüne göre doğru endpoint'i çağırır)
async function deleteMessageUI(id, btn) {
    if (!State.activeChat) return;
    if (!confirm('Bu mesajı silmek istediğinize emin misiniz?')) return;
    try {
        if (State.activeChat.type === 'user') {
            await API.deleteMessage(id);
        } else {
            await API.deleteGroupMessage(State.activeChat.id, id);
        }
        removeMessageEl(id);
        toast('Mesaj silindi', 'success');
    } catch (err) {
        toast(err.message, 'error');
    }
}

function removeMessageEl(id) {
    const el = document.querySelector(`.message[data-msg-id="${id}"]`);
    if (el) el.remove();
}

// ─── Mesaj Gönder ──────────────────────────────────────────────────
$('#message-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const input = $('#message-input');
    const content = input.value.trim();
    if (!content || !State.activeChat) return;
    input.value = '';

    try {
        if (State.activeChat.type === 'user') {
            const res = await API.sendMessage(State.activeChat.id, content);
            appendMessage(toUnified(res));
        } else {
            const res = await API.sendGroupMessage(State.activeChat.id, content);
            appendMessage(toUnifiedGroup(res));
        }
    } catch (err) {
        toast(err.message, 'error');
        input.value = content;
    }
});

// ─── Yeni Grup Modal ───────────────────────────────────────────────
function setupGroupModal() {
    $('#new-group-btn').addEventListener('click', openGroupModal);

    $$('[data-close-modal]').forEach(btn => {
        btn.addEventListener('click', () => {
            $('#group-modal').classList.add('hidden');
            $('#manage-modal').classList.add('hidden');
        });
    });

    $('#create-group-confirm').addEventListener('click', createGroup);
    $('#save-members-confirm').addEventListener('click', saveGroupMembers);
}

function openGroupModal() {
    $('#group-name').value = '';
    $('#group-desc').value = '';
    const picker = $('#group-member-picker');
    picker.innerHTML = State.members.map(m => `
        <div class="picker-item">
            <input type="checkbox" id="pick-${m.id}" value="${m.id}" />
            <label for="pick-${m.id}">${escapeHtml(m.fullName)} <span style="color:var(--text-muted)">(${escapeHtml(m.email)})</span></label>
        </div>
    `).join('') || '<div class="empty-hint">Üye yok</div>';
    $('#group-modal').classList.remove('hidden');
}

async function createGroup() {
    const name = $('#group-name').value.trim();
    const description = $('#group-desc').value.trim();
    if (!name) { toast('Grup adı gerekli', 'error'); return; }

    const memberIds = [...$$('#group-member-picker input:checked')].map(c => c.value);

    try {
        await API.createGroup({ name, description, memberIds });
        $('#group-modal').classList.add('hidden');
        toast('Grup oluşturuldu', 'success');
        await loadGroups();
        // Gruplar sekmesine geç
        $('[data-tab="groups"]').click();
    } catch (err) {
        toast(err.message, 'error');
    }
}

// ─── Grup Üye Yönetimi ─────────────────────────────────────────────
let _manageGroupId = null;

async function openManageModal(groupId) {
    _manageGroupId = groupId;
    let group;
    try {
        group = await API.getGroup(groupId);
    } catch (err) {
        toast(err.message, 'error');
        return;
    }
    const currentIds = new Set(group.members.map(m => m.userId));

    const picker = $('#manage-member-picker');
    picker.innerHTML = State.members.map(m => `
        <div class="picker-item">
            <input type="checkbox" id="mng-${m.id}" value="${m.id}" ${currentIds.has(m.id) ? 'checked' : ''} />
            <label for="mng-${m.id}">${escapeHtml(m.fullName)} <span style="color:var(--text-muted)">(${escapeHtml(m.email)})</span></label>
        </div>
    `).join('') || '<div class="empty-hint">Üye yok</div>';

    picker.dataset.current = JSON.stringify([...currentIds]);
    $('#manage-modal').classList.remove('hidden');
}

async function saveGroupMembers() {
    const picker = $('#manage-member-picker');
    const current = new Set(JSON.parse(picker.dataset.current || '[]'));
    const selected = new Set([...$$('#manage-member-picker input:checked')].map(c => c.value));

    const toAdd = [...selected].filter(id => !current.has(id));
    const toRemove = [...current].filter(id => !selected.has(id));

    try {
        if (toAdd.length) await API.addMembers(_manageGroupId, toAdd);
        for (const id of toRemove) await API.removeMember(_manageGroupId, id);

        $('#manage-modal').classList.add('hidden');
        toast('Üyeler güncellendi', 'success');
        await loadGroups();
        const group = State.groups.find(g => g.id === _manageGroupId);
        if (group && State.activeChat && State.activeChat.id === _manageGroupId) {
            $('#chat-subtitle').textContent = `${group.memberCount} üye${group.description ? ' • ' + group.description : ''}`;
        }
    } catch (err) {
        toast(err.message, 'error');
    }
}

async function removeGroup(groupId) {
    if (!confirm('Bu grubu silmek istediğinize emin misiniz?')) return;
    try {
        await API.deleteGroup(groupId);
        toast('Grup silindi', 'success');
        State.activeChat = null;
        $('#chat-view').classList.add('hidden');
        $('#empty-state').classList.remove('hidden');
        await loadGroups();
    } catch (err) {
        toast(err.message, 'error');
    }
}

// ─── ÜYE GÖRÜNÜMÜ (Admin olmayan) ──────────────────────────────────
async function initMemberView() {
    // Gruplar sekmesini gizle, üyeler sekmesini "Yöneticiler" yap
    $('[data-tab="groups"]').style.display = 'none';
    $('.tab-btn[data-tab="members"]').textContent = 'Yöneticiler';

    try {
        // Sol listede yöneticiler gösterilir; üye onlarla mesajlaşabilir
        State.members = await API.getAdmins();
        renderMembers();
    } catch (err) {
        toast(err.message, 'error');
    }
}

// ─── SignalR ───────────────────────────────────────────────────────
async function connectSignalR() {
    const token = API.getToken();
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`/hubs/chat?access_token=${token}`)
        .withAutomaticReconnect()
        .build();

    State.connection = connection;

    connection.on('ReceiveMessage', (msg) => {
        handleIncomingDirect(msg);
    });
    connection.on('ReceiveGroupMessage', (msg) => {
        handleIncomingGroup(msg);
    });
    // Karşı taraf bir mesajı silerse anlık kaldır
    connection.on('MessageDeleted', (id) => removeMessageEl(id));
    connection.on('GroupMessageDeleted', (id) => removeMessageEl(id));

    connection.onreconnecting(() => setConn(false));
    connection.onreconnected(() => setConn(true));
    connection.onclose(() => setConn(false));

    try {
        await connection.start();
        setConn(true);
    } catch (err) {
        setConn(false);
        console.error('SignalR bağlantı hatası:', err);
    }
}

function setConn(online) {
    const el = $('#conn-status');
    el.classList.toggle('online', online);
    el.classList.toggle('offline', !online);
    el.title = online ? 'Gerçek zamanlı bağlantı: Aktif' : 'Bağlantı: Kesik';
}

function handleIncomingDirect(msg) {
    const mine = msg.senderId === State.user.userId;
    // Aktif sohbet bu kişiyle mi?
    const active = State.activeChat;
    const relevant = active && active.type === 'user' &&
        (msg.senderId === active.id || msg.receiverId === active.id);

    if (relevant && !mine) {
        appendMessage(toUnified(msg));
    } else if (!mine) {
        toast(`${msg.senderName}: ${msg.content}`, 'info');
    }
}

function handleIncomingGroup(msg) {
    const mine = msg.senderId === State.user.userId;
    const active = State.activeChat;
    const relevant = active && active.type === 'group' && active.id === msg.groupId;

    if (relevant && !mine) {
        appendMessage(toUnifiedGroup(msg));
    } else if (!mine) {
        toast(`[${msg.groupName}] ${msg.senderName}: ${msg.content}`, 'info');
    }
}

// ─── Otomatik oturum açma ──────────────────────────────────────────
if (loadSession()) {
    startApp();
}
