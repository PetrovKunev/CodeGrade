#!/bin/bash
set -e

# Create source file
cat > /workspace/Program.cs << 'EOF'
$1
EOF

# Compile and run
cd /workspace
dotnet new console --force
dotnet build --no-restore
dotnet run --no-build 