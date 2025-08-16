#!/bin/bash
# This script runs after the dev container is created to set up the workspace.

dotnet publish ./tests/DotWrap.TestLib/DotWrap.TestLib.csproj -r linux-x64
pip install ./tests/DotWrap.TestLib/python_project_root/
pytest