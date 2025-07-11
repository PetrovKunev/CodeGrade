#!/bin/bash
set -e

# Create source file
cat > /workspace/Main.java << 'EOF'
$1
EOF

# Compile and run
cd /workspace
javac Main.java
java Main 