#!/bin/bash
set -e

# Create source file
cat > /workspace/main.py << 'EOF'
$1
EOF

# Run Python code
cd /workspace
python3 main.py 