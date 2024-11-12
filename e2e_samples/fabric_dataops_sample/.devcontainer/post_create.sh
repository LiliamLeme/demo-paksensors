#!/usr/bin/env bash
export PATH=$PATH:~/.local/bin
echo 'export PATH=$PATH:~/.local/bin' >> ~/.zshrc

cd /workspace
pip install -e ".[dev]"
pre-commit install
