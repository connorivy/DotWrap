# Devcontainer Dockerfile for DotWrap
# Base: .NET 10 SDK with Python 3.13

# Use official .NET 10 SDK image as base
FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-preview
# FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-noble

# Set default shell to bash
SHELL ["/bin/bash", "-c"]

# Set working directory
WORKDIR /workspace

RUN apt-get update && apt-get install -y \
    python3-pip \
    python3-venv \
    git