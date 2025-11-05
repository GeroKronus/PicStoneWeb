#!/usr/bin/env python3
"""
Script para criar um ícone baseado na moldura da Bancada #2 com fundo cinza claro
Mantém proporção original da moldura sem forçar formato quadrado
"""
from PIL import Image
import os

# Caminhos
bancada_path = "Backend/MockupResources/Bancadas/bancada2.png"
output_path = "Backend/wwwroot/icon-ambientes.png"

# Carrega a imagem da bancada
img = Image.open(bancada_path)

# Converte para RGBA se necessário
if img.mode != 'RGBA':
    img = img.convert('RGBA')

# Redimensiona mantendo proporção original
# Define largura máxima de 128px
max_width = 128
aspect_ratio = img.size[1] / img.size[0]  # altura / largura
new_width = max_width
new_height = int(new_width * aspect_ratio)

img = img.resize((new_width, new_height), Image.Resampling.LANCZOS)

# Cria uma nova imagem com fundo cinza claro usando o tamanho proporcional
icon = Image.new('RGBA', (new_width, new_height), (229, 231, 235, 255))

# Faz o composite da bancada sobre o fundo cinza (respeita alpha/transparência)
icon.paste(img, (0, 0), img)

# Salva o ícone
icon.save(output_path, 'PNG', optimize=True)
print(f"Icone criado: {output_path}")
print(f"Dimensoes: {new_width}x{new_height}px")
print(f"Tamanho: {os.path.getsize(output_path) / 1024:.1f} KB")
