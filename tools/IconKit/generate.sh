#!/bin/zsh
# Gera os ícones do wallpaper4fun a partir de design/icon/*.svg: o .ico do Windows,
# os logos do pacote e as artes da página da Microsoft Store e, opcionalmente, o AppIcon do Mac.
#   ./tools/IconKit/generate.sh                      -> só o .ico do Windows
#   ./tools/IconKit/generate.sh <pasta do repo Mac>  -> também o AppIcon do Mac
set -e
cd "$(dirname "$0")/../.."
TMP=$(mktemp -d)
dotnet build tools/IconKit -c Release -o $TMP/iconkit -v q --nologo >/dev/null
kit() { dotnet $TMP/iconkit/IconKit.dll "$@"; }

# Windows: 16–32 px usam a versão simplificada; ocupa o quadro inteiro.
for px in 16 24 32; do kit png design/icon/icon-small.svg $TMP/$px.png $px; done
for px in 48 64 128 256; do kit png design/icon/icon-full.svg $TMP/$px.png $px; done
kit ico src/Wallpaper4Fun.App/Assets/wallpaper4fun.ico $TMP/{16,24,32,48,64,128,256}.png

# Microsoft Store (MSIX): logos do pacote. Até 50 px usam a versão simplificada.
M=packaging/msix/Assets
kit png design/icon/icon-small.svg $M/Square44x44Logo.png 44
kit png design/icon/icon-small.svg $M/StoreLogo.png 50
kit png design/icon/icon-full.svg $M/Square150x150Logo.png 150

# Artes da página da loja (enviadas pelo Partner Center, não vão no pacote).
kit png design/store/box-art-1080x1080.svg design/store/box-art-1080x1080.png 1080x1080
kit png design/store/poster-art-720x1080.svg design/store/poster-art-720x1080.png 720x1080

# Mac: margem transparente do grid da Apple (824 de 1024 px).
if [[ -n "$1" ]]; then
  SET="$1/wallpaper4fun/Assets.xcassets/AppIcon.appiconset"
  for px in 16 32; do kit png design/icon/icon-small.svg $SET/icon_$px.png $px 0.09765625; done
  for px in 64 128 256 512 1024; do kit png design/icon/icon-full.svg $SET/icon_$px.png $px 0.09765625; done
fi
rm -rf $TMP
