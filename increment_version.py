#!/usr/bin/env python3
"""
Script para incrementar automaticamente a versão da aplicação
Uso: python increment_version.py
"""

import json
import os
from datetime import datetime
import subprocess

def get_git_commit():
    """Obtém o hash do último commit git"""
    try:
        result = subprocess.run(['git', 'rev-parse', '--short', 'HEAD'],
                              capture_output=True, text=True, check=True)
        return result.stdout.strip()
    except:
        return 'unknown'

def increment_version(version_str):
    """
    Incrementa a versão no formato 1.0001
    1.0001 -> 1.0002
    1.9999 -> 2.0000
    """
    parts = version_str.split('.')
    major = int(parts[0])
    minor = int(parts[1])

    minor += 1

    # Se minor passar de 9999, incrementa major e reseta minor
    if minor > 9999:
        major += 1
        minor = 0

    return f"{major}.{minor:04d}"

def main():
    # Caminho do arquivo version.json
    version_file = os.path.join(os.path.dirname(__file__), 'Backend', 'version.json')

    # Lê o arquivo atual
    try:
        with open(version_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
    except FileNotFoundError:
        print(f"❌ Arquivo {version_file} não encontrado!")
        return

    # Incrementa a versão
    old_version = data['version']
    new_version = increment_version(old_version)

    # Atualiza os dados
    data['version'] = new_version
    data['buildDate'] = datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%SZ')
    data['commit'] = get_git_commit()

    # Salva o arquivo
    with open(version_file, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2)

    print(f"✅ Versão incrementada: {old_version} → {new_version}")
    print(f"📅 Build Date: {data['buildDate']}")
    print(f"🔖 Commit: {data['commit']}")

if __name__ == '__main__':
    main()
