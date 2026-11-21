#!/bin/zsh
# Gera os ícones do wallpaper4fun a partir de design/icon/*.svg.
#   ./tools/IconKit/generate.sh                      -> só o .ico do Windows
#   ./tools/IconKit/generate.sh <pasta do repo Mac>  -> também o AppIcon do Mac
set -e
cd "$(dirname "$0")/../.."
KIT="dotnet run --project tools/IconKit --"
TMP=$(mktemp -d)

# Windows: 16–32 px usam a versão simplificada; ocupa o quadro inteiro.
for px in 16 24 32; do $KIT png design/icon/icon-small.svg $TMP/$px.png $px; done
for px in 48 64 128 256; do $KIT png design/icon/icon-full.svg $TMP/$px.png $px; done
$KIT ico src/Wallpaper4Fun.App/Assets/wallpaper4fun.ico $TMP/{16,24,32,48,64,128,256}.png

# Mac: margem transparente do grid da Apple (824 de 1024 px).
if [[ -n "$1" ]]; then
  SET="$1/wallpaper4fun/Assets.xcassets/AppIcon.appiconset"
  for px in 16 32; do $KIT png design/icon/icon-small.svg $SET/icon_$px.png $px 0.09765625; done
  for px in 64 128 256 512 1024; do $KIT png design/icon/icon-full.svg $SET/icon_$px.png $px 0.09765625; done
fi
rm -rf $TMP
