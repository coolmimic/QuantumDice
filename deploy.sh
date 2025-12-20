#!/bin/bash

# QuantumDice 部署脚本
# 使用方法: ./deploy.sh [命令]
# 命令: setup | start | stop | restart | logs | update | ssl

set -e

# 配置
DOMAIN="liangzi.love"
EMAIL="admin@liangzi.love"
PROJECT_DIR="/opt/quantumdice"

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# 初始化环境
setup() {
    log_info "开始环境初始化..."
    
    # 安装 Docker
    if ! command -v docker &> /dev/null; then
        log_info "安装 Docker..."
        curl -fsSL https://get.docker.com | sh
        sudo usermod -aG docker $USER
        log_warn "请重新登录以使 Docker 权限生效"
    else
        log_info "Docker 已安装"
    fi
    
    # 安装 Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        log_info "安装 Docker Compose..."
        sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
        sudo chmod +x /usr/local/bin/docker-compose
    else
        log_info "Docker Compose 已安装"
    fi
    
    # 创建目录
    sudo mkdir -p $PROJECT_DIR
    sudo mkdir -p $PROJECT_DIR/nginx/ssl
    
    # 创建 .env 文件
    if [ ! -f "$PROJECT_DIR/.env" ]; then
        log_info "创建环境配置文件..."
        cat > $PROJECT_DIR/.env << EOF
# 数据库密码
DB_PASSWORD=QuantumDice@$(date +%s | sha256sum | head -c 12)

# JWT 密钥
JWT_KEY=QuantumDice_$(date +%s | sha256sum | head -c 32)

# Telegram Bot Token (请填写)
TELEGRAM_BOT_TOKEN=

EOF
        log_warn "请编辑 $PROJECT_DIR/.env 填写 Telegram Bot Token"
    fi
    
    log_info "环境初始化完成!"
}

# 申请 SSL 证书
ssl() {
    log_info "申请 SSL 证书..."
    
    # 停止 nginx 以释放 80 端口
    docker-compose stop nginx 2>/dev/null || true
    
    # 使用 certbot 申请证书
    docker run -it --rm \
        -v "$PROJECT_DIR/nginx/ssl:/etc/letsencrypt" \
        -v "$PROJECT_DIR/nginx/certbot:/var/www/certbot" \
        -p 80:80 \
        certbot/certbot certonly \
        --standalone \
        --email $EMAIL \
        --agree-tos \
        --no-eff-email \
        -d $DOMAIN \
        -d www.$DOMAIN \
        -d api.$DOMAIN
    
    # 复制证书
    sudo cp /etc/letsencrypt/live/$DOMAIN/fullchain.pem $PROJECT_DIR/nginx/ssl/
    sudo cp /etc/letsencrypt/live/$DOMAIN/privkey.pem $PROJECT_DIR/nginx/ssl/
    
    log_info "SSL 证书申请完成!"
}

# 启动服务
start() {
    log_info "启动服务..."
    cd $PROJECT_DIR
    docker-compose up -d
    log_info "服务已启动!"
    log_info "管理后台: https://$DOMAIN"
    log_info "API 接口: https://$DOMAIN/api"
}

# 停止服务
stop() {
    log_info "停止服务..."
    cd $PROJECT_DIR
    docker-compose down
    log_info "服务已停止"
}

# 重启服务
restart() {
    stop
    start
}

# 查看日志
logs() {
    cd $PROJECT_DIR
    docker-compose logs -f --tail=100
}

# 更新代码
update() {
    log_info "更新代码..."
    cd $PROJECT_DIR
    git pull origin main
    
    log_info "重新构建镜像..."
    docker-compose build --no-cache
    
    log_info "重启服务..."
    docker-compose up -d
    
    log_info "更新完成!"
}

# 备份数据库
backup() {
    log_info "备份数据库..."
    BACKUP_FILE="backup_$(date +%Y%m%d_%H%M%S).sql"
    docker exec quantumdice-db pg_dump -U postgres quantum_dice > $PROJECT_DIR/backups/$BACKUP_FILE
    log_info "备份完成: $BACKUP_FILE"
}

# 主入口
case "$1" in
    setup)
        setup
        ;;
    ssl)
        ssl
        ;;
    start)
        start
        ;;
    stop)
        stop
        ;;
    restart)
        restart
        ;;
    logs)
        logs
        ;;
    update)
        update
        ;;
    backup)
        backup
        ;;
    *)
        echo "QuantumDice 部署脚本"
        echo ""
        echo "使用方法: $0 {setup|ssl|start|stop|restart|logs|update|backup}"
        echo ""
        echo "命令说明:"
        echo "  setup   - 初始化服务器环境"
        echo "  ssl     - 申请 SSL 证书"
        echo "  start   - 启动服务"
        echo "  stop    - 停止服务"
        echo "  restart - 重启服务"
        echo "  logs    - 查看日志"
        echo "  update  - 更新代码并重启"
        echo "  backup  - 备份数据库"
        exit 1
        ;;
esac
