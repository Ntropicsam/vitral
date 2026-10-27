<p align="center"><img src="docs/icono.png" width="96" alt=""></p>

<h1 align="center">Vitral</h1>

<p align="center">
  Un reemplazo de código abierto para TRCC de Thermalright en Windows.<br>
  Mantiene prendida la pantalla de tu líquida o disipador y te deja diseñar lo que muestra.
</p>

<p align="center">
  <a href="https://github.com/Ntropicsam/vitral/releases/latest">Descargar</a> ·
  <a href="README.md">Read in English</a> ·
  <a href="CONTRIBUTING.md">Aportar</a>
</p>

---

**Vitral es una alternativa gratuita y de código abierto a TRCC (Thermalright
Control Center) para las pantallas de las líquidas y disipadores Thermalright
«Vision», como la Mjolnir Vision 360.** Esas pantallas solo muestran imagen
mientras TRCC está abierto y se apagan un par de segundos después de cerrarlo.
Vitral habla con la pantalla directamente por USB, vive en la bandeja de Windows y
la mantiene prendida. Trae un editor de temas para poner un GIF, un video o una
foto detrás de las temperaturas de tu CPU y tu GPU.

<p align="center"><img src="docs/editor.png" alt="El editor de temas de Vitral: las capas a la izquierda, la vista en vivo de la pantalla al centro con las temperaturas de CPU y GPU sobre un fondo animado, y los ajustes de la capa a la derecha" width="860"></p>

## Qué hace

- **Siempre prendida.** La pantalla conserva su imagen con el editor cerrado y, si
  quieres, arranca con Windows.
- **Tus propios temas.** Imágenes, GIF y videos de fondo, con cifras de sensores,
  textos y medidores de anillo o de barra encima. Sirve cualquier sensor que lea
  LibreHardwareMonitor.
- **Cifras que se leen.** El editor mide el contraste de cada cifra contra el cuadro
  más claro del fondo y te avisa si cuesta leerla, con un velo de un clic para
  arreglarlo.
- **Trae tus temas de TRCC.** Importa cualquier `Theme.zt` de TRCC tal cual.
- **Gasta poco.** Un tema quieto solo se redibuja cuando cambia una cifra. Sin
  pantalla conectada no dibuja nada, en juegos de pantalla completa baja a un cuadro
  por segundo y, con el editor cerrado, Windows lo corre en modo de ahorro.
- **Se actualiza solo.** Las versiones nuevas se descargan por detrás y se instalan
  la próxima vez que abres Vitral.

## Vitral y TRCC

| | TRCC | Vitral |
|---|---|---|
| Código | Cerrado | Abierto (GPL-3.0) |
| La pantalla al cerrar la ventana | Se apaga | Sigue prendida (vive en la bandeja) |
| Corre como administrador | Sí, en cada inicio | No; solo un servicio pequeño de sensores, de solo lectura |
| Sirve con temas de TRCC | Sí | Sí (importa `Theme.zt`) |

## Instalar

1. Descarga `Vitral-win-Setup.exe` de la [última versión](https://github.com/Ntropicsam/vitral/releases/latest) y ábrelo.
2. Cierra TRCC desde su ícono de la bandeja: solo un programa a la vez puede usar la
   pantalla. Si TRCC se abre solo con Windows, el menú de Vitral lo puede apagar.
3. Para ver la temperatura de la CPU, dale **Activarla** en el editor o en la
   notificación. Windows pide permiso una vez; ver [Seguridad](#seguridad).
4. Si quieres que arranque con Windows, márcalo en el menú de la bandeja.

Pide Windows 10 u 11 de 64 bits y WebView2, que viene con Windows 11. No hace falta
Zadig ni drivers: Windows ya le pone WinUSB a la pantalla.

## Pantallas compatibles

Vitral habla con las pantallas USB de identificador `87AD:70DB` (ChiZhu). Muchos
modelos lo comparten: cada pantalla dice cuál es al conectarse y Vitral elige la
resolución con esa respuesta.

| Modelo | Resolución | Estado |
|---|---|---|
| Mjolnir Vision 360 / Mjolnir Vision Pro | 640 × 480 | Probada |
| Stream Vision | 640 × 480 | Debería funcionar |
| Mjolnir Vision | 320 × 240 | Debería funcionar |
| Grand Vision 360, Hyper Vision 360, Peerless Vision 360, Phantom Spirit 120 Vision EVO, Core Vision | 480 × 480 | Debería funcionar |
| Frozen Warframe Ultra, Frozen Vision V2 | 480 × 480 (vienen montadas al revés, Vitral lo corrige) | Debería funcionar |
| Levita Vision y otras de 1600 × 720 | 1600 × 720 | Debería funcionar |
| Pantallas anchas: 1920 × 462, 1920 × 440, 1280 × 480, 960 × 540, 960 × 320, 854 × 480, 800 × 480 | — | Debería funcionar |
| Pantallas de 320 × 320, de 320 × 240 en RGB565 y de 640 × 172 | — | Todavía no: usan otro formato de imagen |

Las pantallas con otros identificadores (`87CD:70DB`, `0402:3922`, `0416:5302` y
parecidos) hablan otro protocolo y todavía no están soportadas.

**¿La tuya es `87AD:70DB`?** Abre el Administrador de dispositivos, busca
«USBDISPLAY» y entra a Propiedades → Detalles → Identificadores de hardware. Si dice
`VID_87AD&PID_70DB`, lo es. Funcione o no, [cuéntanos de tu pantalla](https://github.com/Ntropicsam/vitral/issues/new?template=pantalla.yml):
cada reporte ayuda a completar esta tabla.

## Preguntas

**¿Hay una alternativa a TRCC para las pantallas de las líquidas Thermalright?**
Sí. Vitral reemplaza a TRCC en Windows para las pantallas con identificador USB
`87AD:70DB`, que son la mayoría de las Thermalright «Vision» (ver la tabla de
arriba). En Linux está [thermalright-trcc-linux](https://github.com/Lexonight1/thermalright-trcc-linux).

**¿Por qué se apaga la pantalla de mi líquida cuando cierro TRCC?**
El firmware de la pantalla vuelve a su logo dos o tres segundos después de la
última imagen que recibe, así que un programa tiene que seguir mandándole cuadros.
TRCC deja de mandarlos al cerrarse; Vitral los sigue mandando desde la bandeja.

**¿Puedo usar mis temas de TRCC?**
Sí. En el editor, elige **Fondo** y escoge el archivo `Theme.zt` del tema, que está
en `C:\TRCCCAP\Data\USBLCD\Theme<ancho><alto>\<tema>\`.

**¿Puedo poner un GIF o un video en la pantalla de la líquida?**
Sí: PNG, JPG, GIF, WebP y cualquier video que el sistema reproduzca (MP4, WebM,
MOV) de hasta cinco minutos.

**¿Afecta a los juegos?**
Con un juego en pantalla completa, Vitral baja la pantalla a un cuadro por segundo.
En un Ryzen 5 5600T, con un tema animado a 24 fps y el editor abierto, usó 0,5 %
del procesador y 73 MB de memoria.

**¿Tengo que dejar TRCC instalado?**
No, pero ciérralo antes de usar Vitral: solo un programa a la vez puede usar la
pantalla.

## Seguridad

Windows solo deja leer la temperatura de la CPU a programas con permisos de
administrador. Vitral nunca corre así. En su lugar instala un servicio pequeño,
«Vitral · Sensores», en `Archivos de programa`, donde solo un administrador puede
escribir. Ese servicio lee los sensores con LibreHardwareMonitor y le pasa los
valores a Vitral por una tubería local de solo lectura que no acepta conexiones de
red. Sin él, todo funciona menos la temperatura de la CPU.

El instalador todavía no está firmado, así que Windows lo muestra como de «editor
desconocido». Si encuentras un problema de seguridad, repórtalo por un
[aviso privado](https://github.com/Ntropicsam/vitral/security/advisories/new) en vez
de un *issue* público.

## Cómo funciona

- La pantalla se maneja por WinUSB directo. Cada cuadro es un JPEG con una cabecera
  de 64 bytes; el protocolo completo, con sus fuentes, está en [`docs/protocolo.md`](docs/protocolo.md).
- El dibujo y el JPEG salen de SkiaSharp. La vista previa del editor es el mismo
  cuadro que va a la pantalla.
- El editor es una página de React dentro de WebView2 y solo existe mientras su
  ventana está abierta.
- Los temas viven en `%APPDATA%\Vitral\Temas`, una carpeta por tema.

Más en [`docs/stack.md`](docs/stack.md) y [`DESIGN.md`](DESIGN.md).

## Compilar

Hace falta el SDK de .NET 10 y Node 24.

```bash
npm --prefix editor ci
npm --prefix editor run build
dotnet test tests/Vitral.Core.Tests
dotnet run --project src/Vitral.App
```

El editor también corre solo en el navegador, con un Vitral simulado:
`npm --prefix editor run dev`. El instalador se arma con `scripts/publicar.ps1`
(Velopack).

## Aportar

Vitral está empezando y hay mucho por hacer, así que cualquier ayuda suma:

- **Probar una pantalla** que todavía no esté marcada como probada.
- **Pantallas RGB565** (320 × 320, 640 × 172) y los otros protocolos (`87CD:70DB`,
  `0402:3922`, `0416:5302`).
- **Traducir** la interfaz, empezando por el inglés.
- **Funciones del editor**: medidores nuevos, relojes, gráficas, más fuentes.
- **Firmar** el instalador.

En [CONTRIBUTING.md](CONTRIBUTING.md) está cómo empezar. Los *issues* y *pull
requests* pueden ir en español o en inglés.

## Créditos

Vitral lo creó y lo mantiene [Samuel](https://github.com/Ntropicsam).

Se apoya en el trabajo de quienes descifraron estas pantallas y lo compartieron:
[thermalright-trcc-linux](https://github.com/Lexonight1/thermalright-trcc-linux),
[trvision](https://github.com/0iroN/trvision) y
[oled-webui](https://github.com/Mistress-Lukutar/oled-webui). Los sensores salen de
[LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)
y las cifras van en [Barlow](https://github.com/jpt/barlow) (SIL Open Font License).

Vitral no está afiliado a Thermalright.

## Licencia

[GPL-3.0](LICENSE).
