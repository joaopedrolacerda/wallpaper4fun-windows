# wallpaper4fun (Windows)

Versão Windows do wallpaper4fun: wallpaper animado (shader ou vídeo em loop) atrás dos ícones do desktop, em todos os monitores, com painel de controle e ícone na bandeja.
Feito em **C# + WPF (.NET 11)**. A versão macOS (SwiftUI) fica em `../Untitled Project 2` (repo `joaopedrolacerda/wallpaper4fun`) e tem a mesma arquitetura: mantenha as duas parecidas.

## Build e testes

```bash
dotnet test Wallpaper4Fun.sln
```

```bash
dotnet run --project src/Wallpaper4Fun.App
```

- No Mac, o `dotnet` veio do Homebrew: exporte `DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec` antes de usar.
- **No Mac dá para compilar tudo e rodar os testes**, mas o app (`dotnet run`) só abre no Windows. `EnableWindowsTargeting` (em `Directory.Build.props`) permite compilar o projeto WPF fora do Windows.
- O app exige Windows 10 2004 (19041) ou mais novo (`net11.0-windows10.0.19041.0`), por causa do `SkiaSharp.Views.WPF`.
- `TreatWarningsAsErrors` está ligado em todos os projetos: warning quebra o build.
- Dados do usuário ficam em `%APPDATA%\wallpaper4fun` (`settings.json` e a pasta `Wallpapers`).

## Gerar o .exe

```bash
./tools/publish.sh
```

- Gera `publish/wallpaper4fun-win-x64.zip`: uma pasta `wallpaper4fun` com o `wallpaper4fun.exe`, as DLLs do .NET (self-contained: o PC não precisa ter o .NET) e a pasta `Videos`. **Não use `.exe` único (`PublishSingleFile`)**: a v1.0.1, em arquivo único comprimido, não abria no Windows 10 testado, sem mensagem nenhuma; a versão com arquivos soltos abriu.
- `./tools/publish.sh win-arm64` gera a versão para PCs com processador ARM.
- `./tools/publish.sh --diagnostico` gera `publish/wallpaper4fun-diagnostico-win-x64.zip`: abre com uma janela de console que mostra cada passo da inicialização e qualquer erro, com os arquivos soltos (sem `.exe` único). Use quando o app "não abre" num PC. Os passos também vão para `%APPDATA%\wallpaper4fun\startup.log` (`StartupTrace`, que começa num `[ModuleInitializer]`, antes do `Main` do WPF).
- Funciona no Mac também (o ícone do `.exe` entra mesmo assim). `publish/` está no `.gitignore`.
- O `.exe` não é assinado: o Windows SmartScreen avisa "fonte não confiável"/"editor desconhecido" na primeira vez ("Mais informações" → "Executar assim mesmo"). Isso **não se resolve com código**: só some com o app assinado por um certificado de assinatura de código (pago, emitido para uma pessoa ou empresa verificada; com certificado comum o aviso some aos poucos, conforme o app ganha reputação) ou com o app distribuído pela Microsoft Store (a loja assina). Certificado autoassinado não adianta.
- A pasta `Videos` precisa ficar ao lado do `.exe`.

### Versões publicadas

O `.github/workflows/release.yml` monta a Release sozinho, numa máquina Windows do GitHub:

1. Suba `<Version>` em `src/Wallpaper4Fun.App/Wallpaper4Fun.App.csproj` (ex.: `1.0.5`).
2. Faça o commit, crie a tag `v1.0.5` e envie (`git push origin main --follow-tags`).
3. O workflow confere se a tag bate com a `<Version>`, roda os testes no Windows, publica e cria a Release com:
   - `wallpaper4fun-<versão>-win-x64.zip`: a pasta com o `.exe`, para baixar e usar.
   - `wallpaper4fun-<versão>-win-x64.msi`: instalador feito com WiX 6 a partir de `packaging/msi/wallpaper4fun.wxs` (Arquivos de Programas, todos os usuários, atalho no menu Iniciar, instala em silêncio com `/qn`). O `UpgradeCode` nunca muda.
   - `wallpaper4fun-<versão>-store.msix`: o pacote para a Microsoft Store, feito com o `makeappx` do Windows SDK.

O texto da Release vem de `packaging/release-notes.md` mais a lista de commits. Também dá para rodar à mão (aba Actions → Release → Run workflow): gera os arquivos como artefatos, sem criar Release.

- A v1.0.0 também ficou em `releases/` com **Git LFS** (primeira tentativa). Não adicione versões novas ali: gasta a cota grátis de LFS.
- Histórico: **v1.0.0** e **v1.0.1** não abriam no Windows 10 testado (sumiam sem mensagem). A v1.0.1 adicionou `crash.log`, painel primeiro e `.ico` com BMP. A **v1.0.2** troca o `.exe` único por arquivos soltos (o que fez abrir), pausa com app em tela cheia e recupera o vídeo que congela. A **v1.0.4** faz minimizar também ir para a bandeja. A **v1.0.5** adiciona o instalador `.msi`. A **v1.0.3** corrige "o vídeo trava muito e recarrega": vídeos convertidos para H.264 1080p 30 fps (o Appa era 4K 60 fps e o Fractal era HEVC), vigia que desiste depois de 2 recargas e painel escondido sem prévia tocando.

### Microsoft Store

- O pacote é MSIX, montado a partir de `packaging/msix/AppxManifest.template.xml` (app de desktop com `runFullTrust`), dos logos em `packaging/msix/Assets` (gerados pelo `tools/IconKit/generate.sh`) e da identidade em `packaging/msix/store-identity.json`.
- **`store-identity.json` precisa dos valores reais do Partner Center** (Gerenciamento do produto → Identidade do produto: `Package/Identity/Name`, `Package/Identity/Publisher` e `Package/Properties/PublisherDisplayName`). Com os valores de exemplo (`SUBSTITUA…`), o `.msix` é gerado mas a loja recusa o envio.
- O `.msix` não é assinado: não instala com dois cliques; a loja assina ao publicar.
- A versão do pacote é `<Version>.0` (a loja exige o último número zero).
- Passos do usuário: criar a conta de desenvolvedor individual no Partner Center (gratuita, com verificação de identidade), reservar o nome "wallpaper4fun", copiar a identidade para o `store-identity.json`, gerar uma nova versão e enviar o `.msix` numa submissão (com descrição, capturas de tela e classificação etária).
- **Alternativa escolhida pelo usuário: enviar como app "MSI/EXE"**, com o link do `.msi` da Release (`https://github.com/joaopedrolacerda/wallpaper4fun-windows/releases/download/v<versão>/wallpaper4fun-<versão>-win-x64.msi`; cada versão tem um link fixo). O `.msi` instala em silêncio sem parâmetros extras. **A loja exige que o `.msi` e os `.exe` dentro dele sejam assinados com um certificado de assinatura de código pago**; sem isso a certificação recusa. Quando houver certificado, assine no workflow (o `.exe` antes do `wix build`, o `.msi` depois).
- Risco na análise da loja: o vídeo "Appa na lua" usa um personagem com direitos autorais.

## Estrutura

```
src/Wallpaper4Fun.Core/      Lógica, sem nada de Windows (net11.0). Testada.
src/Wallpaper4Fun.App/       WPF: janelas atrás dos ícones, bandeja, painel, vídeo.
tests/Wallpaper4Fun.Core.Tests/   xUnit. Roda em qualquer sistema.
```

## Arquitetura

A lógica fica no `Core`, sem WPF nem Win32, e é testada. O `App` é uma camada fina que só monta a interface e repassa eventos.

| Arquivo (Core) | Papel | Testado em |
|---|---|---|
| `WallpaperSettings.cs` | `SourceIds` e `WallpaperSettings` (record imutável com tudo que o usuário escolhe). | `SettingsStoreTests` |
| `SettingsStore.cs` | Lê e grava as configurações em JSON. Arquivo corrompido volta ao padrão. | `SettingsStoreTests` |
| `VideoCatalog.cs` | `BundledVideo`, `VideoCatalog` (vídeos padrão), `IBundledVideoLocator` e `FolderVideoLocator`. | `VideoCatalogTests` |
| `VideoLibrary.cs` | Copia vídeos para a pasta do app, substitui o anterior, encontra o salvo. | `VideoLibraryTests` |
| `WallpaperManager.cs` | `IWallpaperDisplay`, `Playback` e o manager: configurações, mostrar/esconder, pausa, importar vídeo. Dispara `Changed` a cada mudança. | `WallpaperManagerTests` |
| `ExtensionPoints.cs` | `IWallpaperSourceInfo`, `SourceRegistry<T>`, `ShaderStyle`, `ShaderCatalog`. | `ExtensionPointTests` |
| `ShaderRenderer.cs` | Desenha um shader SkSL num canvas do Skia, em resolução reduzida (CPU). | `ShaderRendererTests` |
| `PlaybackHealth.cs` | `FullscreenDetector` (a janela da frente cobre o monitor?) e `StallDetector` (o vídeo parou sozinho?). | `PlaybackHealthTests` |
| `CrashLog.cs` | Grava erros inesperados num arquivo de texto, sem nunca lançar erro. | `CrashLogTests` |
| `SmallRules.cs` | `ShaderClock`, `ClosePolicy`, `DesktopLayout` (posição de cada monitor dentro da WorkerW). | `SmallRulesTests` |
| `Shaders/*.sksl` | Shaders em SkSL, embutidos no assembly. | `ExtensionPointTests` (compilam no Skia) |

| Arquivo (App) | Papel |
|---|---|
| `App.xaml.cs` | Monta e liga as peças; `AppPaths`. |
| `Desktop/DesktopHost.cs` + `NativeMethods.cs` | Win32: pede ao Explorer a camada WorkerW (mensagem `0x052C` ao Progman) e pendura a janela atrás dos ícones. Trata o layout clássico e o do Windows 11 24H2+. Restaura o papel de parede original ao esconder. |
| `Desktop/DesktopWindowPresenter.cs` | Implementa `IWallpaperDisplay`: uma janela por monitor, recriadas quando os monitores mudam. |
| `Desktop/ForegroundWatcher.cs` | Olha a janela da frente e avisa o manager quando há um app em tela cheia. |
| `Desktop/SystemEventObserver.cs` | Suspensão e bloqueio de tela → `SystemDidSuspend`/`SystemDidResume`. |
| `TrayIcon.cs` | Ícone na bandeja (Windows Forms `NotifyIcon`). |
| `Sources/WallpaperSource.cs` | `IWallpaperSource` (parte visual de um tipo), `AppSources.Standard` (registro), `WallpaperHost`, `ManagerBinding.BindTo`. |
| `Sources/ShaderSource.cs` | `ShaderSource`, `ShaderWallpaperView` (SKElement a 30 fps), seletor de shader. |
| `Sources/VideoSource.cs` | `VideoSource`, `VideoWallpaperView` (MediaElement mudo em loop), controles de vídeo. |
| `Panel/PanelWindow.xaml(.cs)` | Painel de controle. |

Como funciona:
- **Fluxo**: painel e bandeja chamam `manager.Update(s => s with { ... })`. O manager salva, mostra/esconde as janelas se `IsEnabled` mudou e dispara `Changed`. As views se atualizam com `BindTo`.
- **Atrás dos ícones**: a janela é criada sem aparecer (`EnsureHandle`), pendurada na WorkerW (ou no Progman, no 24H2+), posicionada em pixels com `DesktopLayout` e só então mostrada.
- **Pausa**: use `manager.Playback` (pausa do usuário ou sistema suspenso/bloqueado), não `Settings.IsPaused` direto.
- **Fechar ou minimizar o painel**: `ClosePolicy` decide. Com "Ao fechar ou minimizar, ficar só na bandeja" ligado (`HideToTrayWhenClosed`), fechar e minimizar escondem o painel e tiram ele da barra de tarefas; desligado, fechar encerra o app e minimizar funciona como sempre.
- **Pausa em tela cheia**: `ForegroundWatcher` olha a janela da frente a cada 1,5 s; se for um app em tela cheia (`FullscreenDetector`: jogo, vídeo), chama `manager.FullscreenAppChanged`, que entra no `EffectivePaused`.
- **Vídeo que congela**: o `MediaElement` do WPF congela depois que um jogo em tela cheia toma a placa de vídeo (visto com Valorant). `VideoWallpaperView` recarrega o vídeo ao voltar de uma pausa, continuando do mesmo ponto, e um vigia (`StallDetector`, a cada 1 s) recarrega quando o vídeo deveria tocar mas ficou parado por 5 s. Depois de 2 recargas seguidas sem o vídeo andar, ele desiste (e anota no `startup.log`) até o vídeo voltar a andar ou haver uma pausa, para não entrar num ciclo de recarregar.
- **Painel escondido não toca nada**: ao ir para a bandeja, o painel solta a prévia e as miniaturas (`OnVisibilityChanged`) e recria quando volta.
- **Erros nunca somem em silêncio**: `ErrorReporter` (em `App.xaml.cs`) mostra uma mensagem e grava em `%APPDATA%\wallpaper4fun\crash.log` (`CrashLog`, no Core). Quando alguém disser que "não abriu", peça esse arquivo.
- **Ordem de inicialização**: o painel abre primeiro; bandeja, eventos do sistema e wallpaper vêm depois, cada um com `ErrorReporter.Try`, para uma falha não esconder o app inteiro. A v1.0.0 abria o painel por último e sumia sem mensagem quando algo antes falhava.
- **Shader na CPU**: `ShaderRenderer` desenha em metade da resolução e amplia. Se precisar de mais desempenho, troque `ShaderWallpaperView` para `SKGLElement` (GPU).
- WPF e Windows Forms juntos têm tipos com o mesmo nome: os do Forms são usados com o alias `Forms.` (os `using` globais deles estão removidos no `.csproj`).

### Não verificado ainda

Este código foi escrito e testado num Mac. **O app nunca foi aberto no Windows.** Pontos que mais precisam de teste manual:
- A janela atrás dos ícones, principalmente no Windows 11 24H2+ (layout novo do Progman).
- Vários monitores com escalas (DPI) diferentes.
- Restaurar o papel de parede ao sair.

## Ícone

Café numa mesa com paisagem noturna (lua, montanhas e uma aurora suave), igual ao da versão Mac.

- Fontes: `design/icon/icon-full.svg` (64 px para cima) e `design/icon/icon-small.svg` (versão simplificada, 32 px para baixo, porque os detalhes somem). As mesmas fontes estão no repo Mac, em `Design/Icon/`. Mantenha as duas cópias iguais.
- O `.ico` usa BMP de 32 bits do 16 ao 128 px e PNG só no 256, que é o formato que o Windows e o `System.Drawing.Icon` esperam.
- `src/Wallpaper4Fun.App/Assets/wallpaper4fun.ico` é gerado: é usado no `.exe` (`ApplicationIcon`), na janela do painel e na bandeja (`AppIcon.Load`).
- Para gerar de novo depois de mudar os SVGs: `./tools/IconKit/generate.sh`. Passe a pasta do repo Mac (`../Untitled\ Project\ 2`) para gerar também o AppIcon de lá. O `IconKit` usa Svg.Skia e não faz parte da solução.
- Artes da página da Microsoft Store: `design/store/box-art-1080x1080.svg` (1:1) e `design/store/poster-art-720x1080.svg` (2:3), com a mesma cena do ícone sem o canto arredondado, mais céu e o nome do app. Os PNGs ao lado saem do `generate.sh` e são enviados à mão no Partner Center (não vão no pacote). Se mudar a cena do ícone, mude também nessas duas artes.

## Vídeos padrão

Cinco vídeos vêm dentro do app e aparecem como miniaturas no painel (tipo "Vídeo"): Gato no pôr do sol (padrão), Moto na estrada, Appa na lua, Planeta vermelho e Fractal violeta. O usuário ainda pode importar um vídeo próprio; o importado fica guardado e dá para voltar a ele.

- `VideoCatalog` lista os vídeos (id + título). `settings.bundledVideoID` guarda o escolhido; `nil`/`null` = usar o importado.
- Origem: bancos de vídeo gratuitos (nomes originais no estilo Pexels/Pixabay), escolhidos pelo usuário. **O "Appa na lua" mostra um personagem de Avatar (direitos autorais)**: o usuário escolheu incluir mesmo assim. Confira as licenças antes de distribuir o app publicamente.
- **Formato dos vídeos no Windows: H.264, 1080p, no máximo 30 fps.** Nunca HEVC (o Windows 10 só toca com extensão paga) nem 4K/60 fps (pesado demais: o app decodifica uma cópia por monitor). Converta com `swift tools/transcode.swift <entrada> <saída.mp4> <Mbps>` (precisa de Mac com Xcode). Os vídeos da versão Mac são outros arquivos (lá HEVC e 4K tocam bem).
- Mantenha cada arquivo abaixo de 100 MB (limite do GitHub). O Fractal (2 min) ficou com 75 MB a 5 Mbps.
- Arquivos em `src/Wallpaper4Fun.App/Videos/<id>.mp4` e `<id>.jpg`, copiados para `Videos\` ao lado do `.exe` (`Content` no `.csproj`). `FolderVideoLocator` acha os arquivos.
- `VideoCatalogTests.EveryVideoIsShippedWithApp` falha se faltar arquivo de algum vídeo do catálogo.

### Como adicionar um vídeo padrão

1. Coloque `<id>.mp4` e uma miniatura `<id>.jpg` (~480 px de largura) em `src/Wallpaper4Fun.App/Videos/`.
2. Adicione `new("<id>", "Título")` em `VideoCatalog.All`.
3. Faça o mesmo na versão Mac.

## Como adicionar um shader

1. Crie `src/Wallpaper4Fun.Core/Shaders/<id>.sksl` com `uniform float2 iResolution; uniform float iTime;` e `half4 main(float2 fragCoord)`.
2. Adicione `new("<id>", "Título")` em `ShaderCatalog.All`.

Os testes já verificam que todo shader do catálogo compila e anima.

## Como adicionar um tipo de wallpaper

1. Crie `src/Wallpaper4Fun.App/Sources/<Nome>Source.cs` com uma classe que implementa `IWallpaperSource` (veja `ShaderSource` e `VideoSource`).
2. Registre em `AppSources.Standard`.
3. Se precisar guardar escolhas, adicione propriedades em `WallpaperSettings` e um id em `SourceIds`.

## Regras de código

Estas regras valem para **todo** código novo e para qualquer código existente que for alterado. São as mesmas da versão Mac.

### TDD sempre

1. **Vermelho**: escreva primeiro um teste que falha, descrevendo o comportamento. Rode e confirme que falha pelo motivo certo.
2. **Verde**: escreva o mínimo de código para passar.
3. **Refatorar**: limpe com os testes passando.

- Use **xUnit** com `DisplayName` em português descrevendo o comportamento: `[Fact(DisplayName = "pausa quando o sistema suspende")]`.
- Nenhuma lógica entra sem teste. Correção de bug começa com um teste que reproduz o bug.
- Testes rápidos e isolados: nada de `%APPDATA%` real, registro do Windows ou rede. Use `TemporaryFolder` e dublês (`FakeDisplay`).
- Um comportamento por teste.

### Código testável

- **Lógica no `Core`**, sem `System.Windows`, Win32 ou Windows Forms. O `App` só monta a interface e repassa ações.
- **Injete dependências** pelo construtor (caminhos, `IWallpaperDisplay`, ...). Nada de estáticos globais dentro da lógica.
- **Interfaces pequenas** nas fronteiras com o sistema, para poder trocar por dublês nos testes.

### Funções pequenas (Clean Code)

- **Uma função faz uma coisa só.** Se dá pra descrever com "e", divida.
- Mire em métodos de **até ~15 linhas** e no máximo **um nível de indentação**. Passou disso, extraia métodos com nomes descritivos.
- **Nomes revelam intenção**. Identificadores em inglês; textos da interface e comentários em português.
- **Poucos parâmetros** (ideal até 2–3). Mais que isso, agrupe num tipo (`record`).
- Prefira **imutabilidade**: `record` + `with` para estado.
- Separe consulta de comando. Comentários explicam o **porquê**.
- Trate só as exceções que você espera (`catch (Exception e) when (e is IOException ...)`); nunca engula tudo.

### DRY

- **Não repita lógica.** Na segunda vez, extraia.
- Exemplos neste projeto: `ManagerBinding.BindTo` (um jeito só de ligar views ao manager), `Playback` (pausa + velocidade), `SettingsStore` (um lugar só para salvar).
- Constantes (ids, fps, escala) ficam definidas em **um lugar**.
- DRY é sobre conhecimento, não sobre texto parecido.

### Open-Closed Principle (SOLID)

Aberto para extensão, fechado para modificação: comportamento novo é código novo, não edição do que já funciona.

- **Sem `switch`/`if` espalhados sobre "tipos".** Use uma interface com uma implementação por variação.
- **Um ponto de registro** (`AppSources.Standard`, `ShaderCatalog.All`).
- **Dependa de abstrações** (`IWallpaperDisplay`, `IWallpaperSource`).
- **Dados em vez de código** quando a variação é só configuração (shader = arquivo `.sksl` + entrada no catálogo).
- **Prove a extensão com teste** (veja `SourceRegistryTests.ExtendsWithNewSource`).
- Não crie abstração antes da hora.

| Para adicionar… | Faça | Não mexa em |
|---|---|---|
| Um tipo de wallpaper | Nova classe `IWallpaperSource` + registro em `AppSources.Standard`. | `WallpaperHost`, `PanelWindow`, `WallpaperManager` |
| Um shader | Arquivo `.sksl` + entrada em `ShaderCatalog.All`. | `ShaderSource`, `ShaderRenderer` |
| Outra forma de mostrar o wallpaper | Implemente `IWallpaperDisplay`. | `WallpaperManager` |

### Antes de terminar uma tarefa

- `dotnet test Wallpaper4Fun.sln` passa e o build não tem warnings (eles quebram o build).
- Nenhum método novo fora das regras acima.
- Commits sem linha de co-autoria.
