#!/bin/zsh
# Gera o wallpaper4fun para Windows: uma pasta com o wallpaper4fun.exe, as DLLs do .NET
# (não precisa ter o .NET instalado) e a pasta Videos, empacotada em publish/wallpaper4fun-<rid>.zip.
#   ./tools/publish.sh                  -> win-x64 (PCs comuns; roda também em ARM por emulação)
#   ./tools/publish.sh win-arm64        -> PCs com processador ARM
#   ./tools/publish.sh --diagnostico    -> abre com uma janela de console mostrando cada passo e
#                                          os erros.
#                                          Use quando o app "não abre" num PC.
set -e
cd "$(dirname "$0")/.."

RID=win-x64
DIAGNOSTIC=false
for arg in "$@"; do
  case $arg in
    --diagnostico) DIAGNOSTIC=true ;;
    *) RID=$arg ;;
  esac
done

NAME=wallpaper4fun-$RID
EXTRA=()
if $DIAGNOSTIC; then
  NAME=wallpaper4fun-diagnostico-$RID
  EXTRA=(-p:Diagnostic=true)
fi

OUT=publish/$NAME
rm -rf $OUT publish/$NAME.zip
# Configurações de publicação em src/Wallpaper4Fun.App/Properties/PublishProfiles/Release.pubxml
dotnet publish src/Wallpaper4Fun.App -c Release -r $RID -p:PublishProfile=Release $EXTRA -o $OUT
rm -f $OUT/*.pdb
(cd publish && mv $NAME wallpaper4fun && zip -qr $NAME.zip wallpaper4fun && mv wallpaper4fun $NAME)
echo "pronto: publish/$NAME.zip"
