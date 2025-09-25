#!/bin/bash
# This script runs after the dev container is created to set up the workspace.

set -e

python3 -m venv venv
source venv/bin/activate
python3 -m pip install -r requirements.txt

# make python and pip commands available
ln -sf "$VIRTUAL_ENV/bin/python" /usr/local/bin/python
ln -sf "$VIRTUAL_ENV/bin/pip" /usr/local/bin/pip

# Restore .NET dependencies
dotnet restore
