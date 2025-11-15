import shutil
import os

# Copia os arquivos
src_app = r"D:\Claude Code\PicStone WEB\Frontend\app.js"
dst_app = r"D:\Claude Code\PicStone WEB\Backend\wwwroot\app.js"

src_html = r"D:\Claude Code\PicStone WEB\Frontend\index.html"
dst_html = r"D:\Claude Code\PicStone WEB\Backend\wwwroot\index.html"

try:
    shutil.copy(src_app, dst_app)
    print("OK - app.js copiado")

    shutil.copy(src_html, dst_html)
    print("OK - index.html copiado")

    print("\nTodos os arquivos copiados com sucesso!")
except Exception as e:
    print(f"ERRO: {e}")
