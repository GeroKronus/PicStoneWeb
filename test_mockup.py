#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script de teste completo do fluxo de mockup com verificação de marca d'água
"""

import requests
import json
from PIL import Image, ImageDraw
import os
import time

BASE_URL = "http://localhost:5000"
UPLOAD_DIR = r"D:\Claude Code\PicStone WEB\Backend\uploads"

print("=== TESTE COMPLETO DE MOCKUP COM MARCA D'ÁGUA ===\n")

# 1. Login
print("1. Fazendo login...")
login_response = requests.post(
    f"{BASE_URL}/api/auth/login",
    json={"username": "admin", "password": "admin123"}
)
login_response.raise_for_status()
token = login_response.json()["token"]
print(f"   Token obtido: {token[:20]}...\n")

headers = {"Authorization": f"Bearer {token}"}

# 2. Criar imagem de teste
print("2. Criando imagem de teste (1487x800)...")
img = Image.new('RGB', (1487, 800), color=(200, 200, 200))
draw = ImageDraw.Draw(img)

# Adiciona padrão de mármore simulado
draw.rectangle([100, 100, 1387, 700], fill=(255, 255, 255))
for i in range(20):
    import random
    x1, y1 = random.randint(0, 1487), random.randint(0, 800)
    x2, y2 = random.randint(0, 1487), random.randint(0, 800)
    draw.line([x1, y1, x2, y2], fill=(150, 150, 150), width=3)

test_image_path = "test-marble.jpg"
img.save(test_image_path, "JPEG", quality=90)
print(f"   Imagem criada: {test_image_path}\n")

# 3. Gerar mockup
print("3. Enviando requisição para gerar mockup...")
try:
    with open(test_image_path, 'rb') as f:
        files = {'ImagemCropada': ('test-marble.jpg', f, 'image/jpeg')}
        data = {
            'TipoCavalete': 'simples',
            'Fundo': 'claro'
        }

        response = requests.post(
            f"{BASE_URL}/api/mockup/gerar",
            headers=headers,
            files=files,
            data=data
        )

    response.raise_for_status()
    result = response.json()

    print(f"   Mockup gerado com sucesso!")
    print(f"   Mensagem: {result['mensagem']}")
    print(f"   Arquivos gerados:")

    for caminho in result['caminhosGerados']:
        print(f"      - {caminho}")

    # 4. Verificar arquivos gerados
    print("\n4. Verificando arquivos gerados...")
    for caminho in result['caminhosGerados']:
        file_path = os.path.join(UPLOAD_DIR, caminho)
        if os.path.exists(file_path):
            file_size = os.path.getsize(file_path)
            print(f"   ✓ {caminho} ({file_size} bytes)")

            # Verificar dimensões
            with Image.open(file_path) as mockup_img:
                print(f"     Dimensões: {mockup_img.width}x{mockup_img.height}")

                # 5. Verificar marca d'água (checando pixels no canto inferior direito)
                print("     Verificando marca d'água...")
                width, height = mockup_img.width, mockup_img.height

                # Área esperada da logo (10% da largura, canto inferior direito, 20px de margem)
                logo_width = int(width * 0.1)
                logo_x_start = width - logo_width - 20
                logo_y_start = height - int(logo_width * 0.5) - 20  # Estimativa de altura

                # Verifica se há pixels não-brancos/não-cinzas nessa área (indicando logo)
                has_logo = False
                sample_points = [
                    (logo_x_start + 20, logo_y_start + 20),
                    (logo_x_start + logo_width//2, logo_y_start + 20),
                    (logo_x_start + logo_width - 20, logo_y_start + 20)
                ]

                for px, py in sample_points:
                    if 0 <= px < width and 0 <= py < height:
                        pixel = mockup_img.getpixel((px, py))
                        # Logo amarela deve ter valores RGB diferentes de fundo
                        if isinstance(pixel, tuple) and len(pixel) >= 3:
                            r, g, b = pixel[:3]
                            # Verifica se há cor amarela (R e G altos, B baixo)
                            if r > 200 and g > 180 and b < 100:
                                has_logo = True
                                break

                if has_logo:
                    print(f"     ✓ MARCA D'ÁGUA DETECTADA no canto inferior direito!")
                else:
                    print(f"     ⚠ MARCA D'ÁGUA NÃO DETECTADA - verificar manualmente")
                    print(f"       Área verificada: x={logo_x_start}, y={logo_y_start}, largura={logo_width}")
        else:
            print(f"   ✗ {caminho} NÃO ENCONTRADO!")

    print("\n=== TESTE CONCLUÍDO ===")
    print("Arquivos gerados em:", UPLOAD_DIR)

except requests.exceptions.HTTPError as e:
    print(f"   ERRO HTTP: {e}")
    print(f"   Resposta: {e.response.text}")
except Exception as e:
    print(f"   ERRO: {e}")
finally:
    # Limpar arquivo de teste
    if os.path.exists(test_image_path):
        os.remove(test_image_path)
