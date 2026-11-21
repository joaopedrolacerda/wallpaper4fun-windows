#!/bin/zsh
# Gera o wallpaper4fun para Windows: um .exe que não precisa do .NET instalado,
# mais a pasta Videos, empacotados em publish/wallpaper4fun-<rid>.zip.
#   ./tools/publish.sh            -> win-x64 (PCs comuns; roda também em ARM por emulação)
#   ./tools/publish.sh win-arm64  -> PCs com processador ARM
set -e
cd "$(dirname "$0")/.."
RID=${1:-win-x64}
OUT=publish/$RID
rm -rf $OUT publish/wallpaper4fun-$RID.zip
dotnet publish src/Wallpaper4Fun.App -c Release -r $RID --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o $OUT
rm -f $OUT/*.pdb
(cd publish && mv $RID wallpaper4fun && zip -qr wallpaper4fun-$RID.zip wallpaper4fun && mv wallpaper4fun $RID)
echo "pronto: publish/wallpaper4fun-$RID.zip"
