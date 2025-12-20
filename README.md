# QuantumDice 🎲

Telegram Bot 骰子游戏 SaaS 平台

## 特性

- 🎮 三种游戏类型：扫雷、龙虎、快三
- 👥 SaaS 多租户架构
- 🤖 Telegram Bot 集成
- 💰 完整的用户资金管理
- 📊 Web 管理后台

## 技术栈

- **后端**: .NET Core 8 + Entity Framework Core
- **数据库**: PostgreSQL
- **前端**: HTML/CSS/JavaScript
- **Bot**: Telegram.Bot SDK
- **部署**: Docker + Nginx

## 快速开始

### 本地开发

```bash
# 克隆仓库
git clone https://github.com/coolmimic/QuantumDice.git
cd QuantumDice

# 配置数据库连接
# 编辑 src/QuantumDice.Api/appsettings.json

# 运行 API
dotnet run --project src/QuantumDice.Api

# 运行 Web
dotnet run --project src/QuantumDice.Web
```

### 服务器部署 (Ubuntu)

```bash
# 1. 初始化环境
./deploy.sh setup

# 2. 编辑配置
vim .env

# 3. 申请 SSL 证书
./deploy.sh ssl

# 4. 启动服务
./deploy.sh start
```

## 默认账号

- **超级管理员**: `admin` / `admin123`

## 项目结构

```
QuantumDice/
├── src/
│   ├── QuantumDice.Core/        # 核心实体
│   ├── QuantumDice.Infrastructure/  # 数据访问层
│   ├── QuantumDice.Api/         # API + Bot
│   └── QuantumDice.Web/         # 管理后台
├── nginx/                       # Nginx 配置
├── docker-compose.yml           # Docker 编排
├── Dockerfile.api               # API 镜像
├── Dockerfile.web               # Web 镜像
└── deploy.sh                    # 部署脚本
```

## 游戏玩法

### 扫雷 (1骰子)
- 定位胆: `1/10` (猜1, 投10)
- 大小: `大10`, `小10`
- 单双: `单10`, `双10`

### 龙虎 (2骰子)
- 龙虎: `龙10`, `虎10`, `和10`

### 快三 (3骰子)
- 大小单双: `大10`, `小10`
- 豹子: `豹子10`
- 顺子: `顺子10`

## License

MIT
