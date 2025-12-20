// ========== 应用状态 ==========
const state = {
    user: null,
    role: null,
    currentPage: 'dashboard',
    dealers: [],
    groups: [],
    players: []
};

// ========== 菜单配置 ==========
const menus = {
    admin: [
        { id: 'dashboard', icon: '📊', label: '仪表盘' },
        { id: 'dealers', icon: '👔', label: '庄家管理' },
        { id: 'stats', icon: '📈', label: '全局统计' },
        { id: 'settings', icon: '⚙️', label: '系统设置' }
    ],
    dealer: [
        { id: 'dashboard', icon: '📊', label: '仪表盘' },
        { id: 'groups', icon: '👥', label: '群组管理' },
        { id: 'players', icon: '🎮', label: '玩家管理' },
        { id: 'odds', icon: '💰', label: '赔率配置' },
        { id: 'records', icon: '📋', label: '投注记录' }
    ]
};

// ========== 初始化 ==========
document.addEventListener('DOMContentLoaded', () => {
    // 绑定登录表单
    document.getElementById('login-form').addEventListener('submit', handleLogin);

    // 更新时间
    updateTime();
    setInterval(updateTime, 1000);

    // 检查登录状态
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
        const user = JSON.parse(savedUser);
        state.user = user.username;
        state.role = user.role;
        showMainPage();
    }
});

// ========== 登录处理 ==========
async function handleLogin(e) {
    e.preventDefault();

    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;
    const role = document.getElementById('login-role').value;

    try {
        // 调用真实登录 API
        const loginFn = role === 'admin' ? api.auth.adminLogin : api.auth.dealerLogin;
        const result = await loginFn({ username, password });

        if (result.success && result.data) {
            state.user = result.data.username;
            state.role = result.data.role.toLowerCase();

            localStorage.setItem('user', JSON.stringify({
                username: result.data.username,
                role: result.data.role.toLowerCase(),
                token: result.data.token,
                expiresAt: result.data.expiresAt
            }));

            showMainPage();
        } else {
            alert(result.message || '登录失败');
        }
    } catch (e) {
        alert('登录失败，请检查 API 服务是否运行');
    }
}

// ========== 显示主页面 ==========
function showMainPage() {
    document.getElementById('login-page').classList.remove('active');
    document.getElementById('main-page').classList.add('active');

    document.getElementById('current-user').textContent = state.user;
    document.getElementById('current-role').textContent = state.role === 'admin' ? '超级管理员' : '庄家';

    renderSidebar();
    navigateTo('dashboard');
}

// ========== 渲染侧边栏 ==========
function renderSidebar() {
    const nav = document.getElementById('sidebar-nav');
    const menuItems = menus[state.role] || menus.dealer;

    nav.innerHTML = menuItems.map(item => `
        <div class="nav-item ${state.currentPage === item.id ? 'active' : ''}" 
             onclick="navigateTo('${item.id}')">
            <span class="icon">${item.icon}</span>
            <span>${item.label}</span>
        </div>
    `).join('');
}

// ========== 页面导航 ==========
function navigateTo(page) {
    state.currentPage = page;
    document.getElementById('page-title').textContent = getPageTitle(page);
    renderSidebar();
    renderContent(page);
}

function getPageTitle(page) {
    const titles = {
        dashboard: '仪表盘',
        dealers: '庄家管理',
        groups: '群组管理',
        players: '玩家管理',
        odds: '赔率配置',
        records: '投注记录',
        stats: '全局统计',
        settings: '系统设置'
    };
    return titles[page] || page;
}

// ========== 渲染内容 ==========
async function renderContent(page) {
    const content = document.getElementById('content-area');

    switch (page) {
        case 'dashboard':
            content.innerHTML = renderDashboard();
            break;
        case 'dealers':
            content.innerHTML = await renderDealersPage();
            break;
        case 'groups':
            content.innerHTML = await renderGroupsPage();
            break;
        case 'players':
            content.innerHTML = await renderPlayersPage();
            break;
        case 'odds':
            content.innerHTML = renderOddsPage();
            break;
        default:
            content.innerHTML = `<div class="card"><div class="card-body"><p>页面开发中...</p></div></div>`;
    }
}

// ========== 仪表盘 ==========
function renderDashboard() {
    return `
        <div class="stats-grid">
            <div class="stat-card">
                <div class="icon primary">💰</div>
                <div class="value">¥12,580</div>
                <div class="label">今日流水</div>
            </div>
            <div class="stat-card">
                <div class="icon success">📈</div>
                <div class="value">¥3,240</div>
                <div class="label">今日盈利</div>
            </div>
            <div class="stat-card">
                <div class="icon warning">🎯</div>
                <div class="value">256</div>
                <div class="label">今日投注</div>
            </div>
            <div class="stat-card">
                <div class="icon danger">👥</div>
                <div class="value">48</div>
                <div class="label">活跃玩家</div>
            </div>
        </div>
        
        <div class="card">
            <div class="card-header">
                <h3>最近投注</h3>
            </div>
            <div class="table-container">
                <table>
                    <thead>
                        <tr>
                            <th>玩家</th>
                            <th>游戏</th>
                            <th>投注</th>
                            <th>金额</th>
                            <th>状态</th>
                            <th>时间</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>@player1</td>
                            <td>快三</td>
                            <td>大</td>
                            <td>¥100</td>
                            <td><span class="badge badge-success">中奖</span></td>
                            <td>刚刚</td>
                        </tr>
                        <tr>
                            <td>@player2</td>
                            <td>龙虎</td>
                            <td>龙</td>
                            <td>¥50</td>
                            <td><span class="badge badge-danger">未中</span></td>
                            <td>2分钟前</td>
                        </tr>
                        <tr>
                            <td>@player3</td>
                            <td>扫雷</td>
                            <td>小</td>
                            <td>¥200</td>
                            <td><span class="badge badge-warning">待开奖</span></td>
                            <td>5分钟前</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    `;
}

// ========== 庄家管理 (超管) ==========
async function renderDealersPage() {
    let dealersHtml = '<tr><td colspan="6">加载中...</td></tr>';

    try {
        const result = await api.admin.getDealers();
        if (result.success && result.data) {
            state.dealers = result.data;
            dealersHtml = state.dealers.map(d => `
                <tr>
                    <td>${d.id}</td>
                    <td><strong>${d.username}</strong></td>
                    <td>${d.contactTelegram || '-'}</td>
                    <td>${d.groupCount}</td>
                    <td>
                        <span class="badge ${d.isActive ? 'badge-success' : 'badge-danger'}">
                            ${d.isActive ? '正常' : '停用'}
                        </span>
                    </td>
                    <td>${d.subscriptionEndTime ? new Date(d.subscriptionEndTime).toLocaleDateString() : '-'}</td>
                    <td>
                        <button class="btn btn-sm btn-secondary" onclick="editDealer(${d.id})">编辑</button>
                        <button class="btn btn-sm btn-primary" onclick="extendDealer(${d.id})">续费</button>
                    </td>
                </tr>
            `).join('');
        }
    } catch (e) {
        dealersHtml = '<tr><td colspan="7">加载失败，请检查 API 服务是否运行</td></tr>';
    }

    return `
        <div class="card">
            <div class="card-header">
                <h3>庄家列表</h3>
                <button class="btn btn-primary btn-sm" onclick="showCreateDealerModal()">
                    + 新增庄家
                </button>
            </div>
            <div class="table-container">
                <table>
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>用户名</th>
                            <th>联系方式</th>
                            <th>群数</th>
                            <th>状态</th>
                            <th>到期时间</th>
                            <th>操作</th>
                        </tr>
                    </thead>
                    <tbody id="dealers-table">
                        ${dealersHtml}
                    </tbody>
                </table>
            </div>
        </div>
        
        <!-- 创建庄家模态框 -->
        <div class="modal-overlay" id="create-dealer-modal">
            <div class="modal">
                <div class="modal-header">
                    <h3>新增庄家</h3>
                    <button class="modal-close" onclick="closeModal('create-dealer-modal')">&times;</button>
                </div>
                <div class="modal-body">
                    <form id="create-dealer-form">
                        <div class="form-group">
                            <label>用户名</label>
                            <input type="text" id="dealer-username" required>
                        </div>
                        <div class="form-group">
                            <label>密码</label>
                            <input type="password" id="dealer-password" required>
                        </div>
                        <div class="form-group">
                            <label>联系方式 (Telegram)</label>
                            <input type="text" id="dealer-contact">
                        </div>
                        <div class="form-group">
                            <label>订阅到期时间</label>
                            <input type="date" id="dealer-expire" required>
                        </div>
                    </form>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary" onclick="closeModal('create-dealer-modal')">取消</button>
                    <button class="btn btn-primary" onclick="createDealer()">创建</button>
                </div>
            </div>
        </div>
    `;
}

// ========== 群组管理 (庄家) ==========
async function renderGroupsPage() {
    let groupsHtml = '<tr><td colspan="6">加载中...</td></tr>';

    try {
        // 获取当前 Dealer ID
        const token = JSON.parse(localStorage.getItem('user')).token;
        const payload = JSON.parse(atob(token.split('.')[1]));
        const dealerId = payload.nameid || payload.sub || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

        const result = await api.dealer.getGroups(dealerId);
        if (result.success && result.data && result.data.length > 0) {
            groupsHtml = result.data.map(g => `
                <tr>
                    <td>${g.telegramGroupId}</td>
                    <td>${g.groupName || '-'}</td>
                    <td>${g.playerCount}</td>
                    <td><span class="badge ${g.isActive ? 'badge-success' : 'badge-danger'}">${g.isActive ? '运行中' : '未激活'}</span></td>
                    <td>${new Date(g.boundAt).toLocaleDateString()}</td>
                    <td>
                        <button class="btn btn-sm btn-secondary" onclick="configureGroup(${g.id})">配置</button>
                        <button class="btn btn-sm btn-danger" onclick="unbindGroup(${g.id})">解绑</button>
                    </td>
                </tr>
            `).join('');
        } else {
            groupsHtml = '<tr><td colspan="6">暂无绑定的群组</td></tr>';
        }
    } catch (e) {
        console.error(e);
        groupsHtml = '<tr><td colspan="6">加载失败</td></tr>';
    }

    return `
        <div class="card">
            <div class="card-header">
                <h3>我的群组</h3>
                <button class="btn btn-primary btn-sm" onclick="showBindGroupModal()">
                    + 绑定群组
                </button>
            </div>
            <div class="table-container">
                <table id="groups-table">
                    <thead>
                        <tr>
                            <th>群组ID</th>
                            <th>群名</th>
                            <th>玩家数</th>
                            <th>状态</th>
                            <th>绑定时间</th>
                            <th>操作</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${groupsHtml}
                    </tbody>
                </table>
            </div>
        </div>
        
        <!-- 绑定群组模态框 -->
        <div class="modal-overlay" id="bind-group-modal">
            <div class="modal">
                <div class="modal-header">
                    <h3>绑定群组</h3>
                    <button class="modal-close" onclick="closeModal('bind-group-modal')">&times;</button>
                </div>
                <div class="modal-body">
                    <form id="bind-group-form" onsubmit="event.preventDefault(); bindGroup();">
                        <div class="form-group">
                            <label>Telegram 群组 ID</label>
                            <input type="text" id="group-telegram-id" placeholder="-100xxxxxxxxxx" required>
                            <small class="hint">请确保机器人已加入该群组并设置为管理员。ID通常以 -100 开头。</small>
                        </div>
                        <div class="form-group">
                            <label>群组备注名称</label>
                            <input type="text" id="group-name" placeholder="例如：测试一群">
                        </div>
                    </form>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary" onclick="closeModal('bind-group-modal')">取消</button>
                    <button class="btn btn-primary" onclick="bindGroup()">绑定</button>
                </div>
            </div>
        </div>
    `;
}

function showBindGroupModal() {
    showModal('bind-group-modal');
}

async function bindGroup() {
    const telegramId = document.getElementById('group-telegram-id').value;
    const groupName = document.getElementById('group-name').value;

    if (!telegramId) {
        alert('请输入 Telegram 群组 ID');
        return;
    }

    try {
        const token = JSON.parse(localStorage.getItem('user')).token;
        const payload = JSON.parse(atob(token.split('.')[1]));
        const dealerId = payload.nameid || payload.sub || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

        const data = {
            telegramGroupId: parseInt(telegramId),
            groupName: groupName
        };

        const result = await api.dealer.bindGroup(dealerId, data);

        if (result.success) {
            alert('绑定成功');
            closeModal('bind-group-modal');
            const content = await renderGroupsPage();
            document.getElementById('content-area').innerHTML = content;
        } else {
            alert('绑定失败: ' + result.message);
        }
    } catch (e) {
        console.error(e);
        alert('操作失败: ' + e.message);
    }
}

function configureGroup(id) { alert('配置功能开发中'); }
function unbindGroup(id) { alert('解绑功能开发中'); }

// ========== 玩家管理 (庄家) ==========
async function renderPlayersPage() {
    return `
        <div class="card">
            <div class="card-header">
                <h3>玩家列表</h3>
                <div>
                    <select class="form-group" style="display:inline-block;width:auto;margin:0">
                        <option>全部群组</option>
                    </select>
                </div>
            </div>
            <div class="table-container">
                <table>
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>用户名</th>
                            <th>余额</th>
                            <th>累计充值</th>
                            <th>累计投注</th>
                            <th>状态</th>
                            <th>操作</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>1</td>
                            <td>@player1</td>
                            <td>¥1,250.00</td>
                            <td>¥5,000.00</td>
                            <td>¥3,800.00</td>
                            <td><span class="badge badge-success">正常</span></td>
                            <td>
                                <button class="btn btn-sm btn-primary" onclick="showDepositModal(1)">上分</button>
                                <button class="btn btn-sm btn-secondary" onclick="showWithdrawModal(1)">下分</button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
        
        <!-- 上下分模态框 -->
        <div class="modal-overlay" id="adjust-balance-modal">
            <div class="modal">
                <div class="modal-header">
                    <h3 id="adjust-title">上分</h3>
                    <button class="modal-close" onclick="closeModal('adjust-balance-modal')">&times;</button>
                </div>
                <div class="modal-body">
                    <form id="adjust-balance-form">
                        <div class="form-group">
                            <label>金额</label>
                            <input type="number" id="adjust-amount" min="1" required>
                        </div>
                        <div class="form-group">
                            <label>备注</label>
                            <input type="text" id="adjust-remark">
                        </div>
                    </form>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary" onclick="closeModal('adjust-balance-modal')">取消</button>
                    <button class="btn btn-primary" id="adjust-submit">确定</button>
                </div>
            </div>
        </div>
    `;
}

// ========== 赔率配置 ==========
function renderOddsPage() {
    return `
        <div class="card">
            <div class="card-header">
                <h3>赔率配置</h3>
                <select style="padding:8px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:8px;color:var(--text-primary)">
                    <option>选择群组</option>
                </select>
            </div>
            <div class="card-body">
                <h4 style="margin-bottom:16px">🎰 快三</h4>
                <table>
                    <thead>
                        <tr>
                            <th>玩法</th>
                            <th>默认赔率</th>
                            <th>自定义赔率</th>
                            <th>最小投注</th>
                            <th>最大投注</th>
                            <th>状态</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>大</td>
                            <td>1.96</td>
                            <td><input type="number" step="0.01" value="1.96" style="width:80px;padding:6px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)"></td>
                            <td><input type="number" value="1" style="width:80px;padding:6px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)"></td>
                            <td><input type="number" value="10000" style="width:100px;padding:6px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)"></td>
                            <td><span class="badge badge-success">启用</span></td>
                        </tr>
                        <tr>
                            <td>小</td>
                            <td>1.96</td>
                            <td><input type="number" step="0.01" value="1.96" style="width:80px;padding:6px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)"></td>
                            <td><input type="number" value="1" style="width:80px;padding:6px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)"></td>
                            <td><input type="number" value="10000" style="width:100px;padding:6px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)"></td>
                            <td><span class="badge badge-success">启用</span></td>
                        </tr>
                        <tr>
                            <td>豹子</td>
                            <td>30.00</td>
                            <td><input type="number" step="0.01" value="30.00" style="width:80px;padding:6px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)"></td>
                            <td><input type="number" value="1" style="width:80px;padding:6px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)"></td>
                            <td><input type="number" value="1000" style="width:100px;padding:6px;background:var(--bg-input);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)"></td>
                            <td><span class="badge badge-success">启用</span></td>
                        </tr>
                    </tbody>
                </table>
                <div style="margin-top:20px">
                    <button class="btn btn-primary">保存配置</button>
                </div>
            </div>
        </div>
    `;
}

// ========== 辅助函数 ==========
function updateTime() {
    const now = new Date();
    document.getElementById('current-time').textContent = now.toLocaleString('zh-CN');
}

function toggleSidebar() {
    document.getElementById('sidebar').classList.toggle('open');
}

function logout() {
    localStorage.removeItem('user');
    state.user = null;
    state.role = null;
    document.getElementById('main-page').classList.remove('active');
    document.getElementById('login-page').classList.add('active');
}

function showModal(id) {
    document.getElementById(id).classList.add('active');
}

function closeModal(id) {
    document.getElementById(id).classList.remove('active');
}

// ========== 庄家操作 ==========
function showCreateDealerModal() {
    // 设置默认日期为一个月后
    const defaultDate = new Date();
    defaultDate.setMonth(defaultDate.getMonth() + 1);
    document.getElementById('dealer-expire').value = defaultDate.toISOString().split('T')[0];
    showModal('create-dealer-modal');
}

async function createDealer() {
    const data = {
        username: document.getElementById('dealer-username').value,
        password: document.getElementById('dealer-password').value,
        contactTelegram: document.getElementById('dealer-contact').value,
        subscriptionEndTime: new Date(document.getElementById('dealer-expire').value).toISOString()
    };

    try {
        const result = await api.admin.createDealer(data);
        if (result.success) {
            alert('创建成功!');
            closeModal('create-dealer-modal');
            navigateTo('dealers');
        } else {
            alert('创建失败: ' + result.message);
        }
    } catch (e) {
        alert('请求失败，请检查 API 服务');
    }
}

// ========== 玩家上下分 ==========
let adjustPlayerId = null;
let adjustType = 'deposit';

function showDepositModal(playerId) {
    adjustPlayerId = playerId;
    adjustType = 'deposit';
    document.getElementById('adjust-title').textContent = '上分';
    showModal('adjust-balance-modal');
}

function showWithdrawModal(playerId) {
    adjustPlayerId = playerId;
    adjustType = 'withdraw';
    document.getElementById('adjust-title').textContent = '下分';
    showModal('adjust-balance-modal');
}
