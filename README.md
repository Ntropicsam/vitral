<p align="center"><img src="docs/icono.png" width="96" alt=""></p>

# Vitral

Vitral maneja la pantalla de las líquidas y disipadores Thermalright «Vision» sin el
programa del fabricante (TRCC). Vive en la bandeja de Windows, mantiene la pantalla
prendida y trae un editor de temas para poner lo que quieras: un GIF, un video, una
foto, y encima tus temperaturas en grande.

- **Siempre prendida.** La pantalla de estas líquidas se apaga a los dos o tres
  segundos si nadie le manda imágenes. Vitral se las sigue mandando con la ventana
  cerrada.
- **Liviana.** Con un tema quieto solo redibuja cuando cambia una cifra. Si no hay
  pantalla conectada, no dibuja nada. En juegos de pantalla completa baja a un
  cuadro por segundo, y con el editor cerrado Windows lo corre en modo de ahorro.
- **Cifras que se leen.** El editor mide el contraste de cada cifra contra el
  cuadro más claro del fondo y te avisa si cuesta leerla, con un velo para
  arreglarlo.
- **Tus temas de TRCC sirven.** Importa el `Theme.zt` de cualquier tema de TRCC
  tal cual, sin convertir nada.
- **Se actualiza solo.** Cuando sale una versión nueva, Vitral la descarga y la
  instala la próxima vez que lo abras.

## Instalar

1. Descarga `Vitral-win-Setup.exe` de la [última versión](https://github.com/Ntropicsam/vitral/releases/latest) y ábrelo.
2. Cierra TRCC desde su ícono de la bandeja: solo un programa a la vez puede
   usar la pantalla. Si TRCC se abre solo al iniciar Windows, el menú de Vitral
   tiene «Evitar que TRCC se abra solo».
3. Para ver la temperatura de la CPU, en el menú de Vitral elige «Leer la
   temperatura de la CPU». Windows te pide permiso una vez (ver «Seguridad»).
4. Si quieres que arranque con Windows, márcalo en el mismo menú.

Pide Windows 10 u 11 de 64 bits y WebView2 (viene con Windows 11). No hace falta
Zadig ni instalar drivers: Windows ya le pone WinUSB a la pantalla.

## Pantallas compatibles

Vitral habla con las pantallas USB `87AD:70DB` (ChiZhu). Es un solo identificador
para muchos modelos: cada pantalla dice cuál es al conectarse, y Vitral elige la
resolución con esa respuesta.

| Modelo | Resolución | Estado |
|---|---|---|
| Mjolnir Vision 360 / Mjolnir Vision Pro | 640 × 480 | Probada |
| Stream Vision | 640 × 480 | Debería funcionar |
| Mjolnir Vision | 320 × 240 | Debería funcionar |
| Grand Vision 360, Hyper Vision 360, Peerless Vision 360, Phantom Spirit 120 Vision EVO, Core Vision | 480 × 480 | Debería funcionar |
| Frozen Warframe Ultra, Frozen Vision V2 | 480 × 480 (vienen giradas 180°, Vitral lo corrige) | Debería funcionar |
| Levita Vision y otras de 1600 × 720 | 1600 × 720 | Debería funcionar |
| Pantallas anchas de 1920 × 462, 1920 × 440, 1280 × 480, 960 × 540, 960 × 320, 854 × 480 y 800 × 480 | — | Debería funcionar |
| Pantallas de 320 × 320, 320 × 240 en RGB565 y 640 × 172 | — | Todavía no: usan otro formato de imagen |

Las líquidas con pantalla de otros identificadores (`87CD:70DB`, `0402:3922`,
`0416:5302` y parecidos) hablan otro protocolo y todavía no están soportadas.

Para saber si la tuya es `87AD:70DB`: Administrador de dispositivos, busca
«USBDISPLAY», Propiedades → Detalles → Identificadores de hardware. Si dice
`VID_87AD&PID_70DB`, es de esta familia. Si la tuya funciona (o no), abre un
*issue* con el modelo: así crece la tabla.

## Cómo funciona

- La app y el editor corren **sin permisos de administrador**. La pantalla se
  maneja por WinUSB directo, sin libusb.
- Cada cuadro es un JPEG con una cabecera de 64 bytes. Los detalles del protocolo,
  con sus fuentes, están en [`docs/protocolo.md`](docs/protocolo.md).
- El editor es una página web (React) dentro de WebView2. Se crea al abrir la
  ventana y se destruye al cerrarla: cerrado no gasta memoria.
- Los temas viven en `%APPDATA%\Vitral\Temas`, una carpeta por tema.

## Seguridad

Windows solo deja leer la temperatura de la CPU a un programa con permisos de
administrador. Vitral no corre así: instala un servicio pequeño,
«Vitral · Sensores», en `Archivos de programa`, donde solo un administrador puede
escribir. Ese servicio solo lee sensores (con LibreHardwareMonitor) y se los pasa a
Vitral por una tubería local de solo lectura que no acepta conexiones de red. Si no
lo instalas, Vitral funciona igual, con todo menos la temperatura de la CPU.

El instalador todavía no está firmado, así que Windows lo muestra como de «editor
desconocido». Si encuentras un problema de seguridad, escríbeme por un
[aviso privado](https://github.com/Ntropicsam/vitral/security/advisories/new) en vez
de un *issue* público.

## Compilar

Hace falta el SDK de .NET 10 y Node 24.

```bash
npm --prefix editor ci
npm --prefix editor run build
dotnet test tests/Vitral.Core.Tests
dotnet run --project src/Vitral.App
```

El editor también corre solo en el navegador, con un Vitral simulado:
`npm --prefix editor run dev`.

El instalador se arma con `scripts/publicar.ps1` (usa Velopack).

## Créditos

Vitral lo hizo [Samuel](https://github.com/Ntropicsam), cansado de un programa que
apaga la pantalla de su líquida apenas lo cierras.

El protocolo lo descifró la comunidad: [thermalright-trcc-linux](https://github.com/Lexonight1/thermalright-trcc-linux),
[trvision](https://github.com/0iroN/trvision) y [oled-webui](https://github.com/Mistress-Lukutar/oled-webui).
Los sensores salen de [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor);
las cifras van en [Barlow](https://github.com/jpt/barlow) (SIL Open Font License).

Vitral no está afiliado a Thermalright.

## Licencia

[GPL-3.0](LICENSE).
