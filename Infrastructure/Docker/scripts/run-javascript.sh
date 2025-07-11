#!/bin/bash
set -e

# Create source file
cat > /workspace/main.js << 'EOF'
$1
EOF

# Run Node.js code
cd /workspace
node main.js 