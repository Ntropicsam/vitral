# Protocolo de las pantallas 87AD:70DB

Cómo habla el PC con la pantalla de las líquidas y disipadores «Vision» de
Thermalright que usan el controlador ChiZhu `87AD:70DB`. Vitral está escrito a
partir de esta documentación, sin copiar código de otros proyectos.

## Fuentes

Cada dato lleva la letra de su fuente. Las tres coinciden en todo lo que se cita.

| | Proyecto | Licencia | Commit | Qué aporta |
|---|---|---|---|---|
| **[L]** | [Lexonight1/thermalright-trcc-linux](https://github.com/Lexonight1/thermalright-trcc-linux) | GPL-3.0 | `53b7bf1` | Descompilación de `USBLCDNEW.exe` de TRCC: `doc/PROTOCOL_USBLCDNEW.md`, `src/trcc/adapters/device/bulk_lcd.py`, `src/trcc/core/protocol.py`, `doc/REFERENCE_TECHNICAL.md`, `doc/REFERENCE_DEVICES.md` |
| **[T]** | [0iroN/trvision](https://github.com/0iroN/trvision) | MIT | `bc88670` | Implementación en Windows con WinUSB: `protocol.py`, `usb_handler.py` |
| **[O]** | [Mistress-Lukutar/oled-webui](https://github.com/Mistress-Lukutar/oled-webui) | MIT | `b81f27a` | Constantes verificadas contra una captura USB: `src/luminaflowui/core/constants.py` |
| **[V]** | Vitral | — | — | Probado en una Mjolnir Vision 360 (PM 7, SUB 2) con `scripts/prueba_pantalla.py` |

## Abrir el dispositivo

- VID `0x87AD`, PID `0x70DB`, nombre `USBDISPLAY` [L][T][O].
- Windows le asigna WinUSB solo, por los descriptores MS OS: no hace falta Zadig [T][V].
- Una interfaz, clase `0xFF`, con endpoints bulk `0x01` (salida) y `0x81`
  (entrada), de 512 bytes por paquete [L][O][T][V].
- Solo un programa a la vez: con TRCC abierto, su ayudante `USBLCDNEW.exe` tiene
  la interfaz y nadie más la puede abrir [T].

## Handshake

Se escriben 64 bytes en `0x01` y se leen hasta 1024 de `0x81` [L][T][O]:

```text
Offset  Valor
0-3     12 34 56 78     magia
4-55    00
56      01              consulta de información
57-63   00
```

Las fuentes hablan de 1024 bytes de respuesta, pero la Mjolnir Vision 360 devuelve
64 [V]: se acepta lo que llegue desde 57 bytes.

| Byte | Qué es |
|---|---|
| 0-3 | La misma magia |
| 4-11 | `SSCRM-V1` en ASCII [V] |
| 24 | PM, el modelo. 0 = no es una pantalla válida |
| 36 | SUB, la variante del modelo |
| 48-51 | Identificador del panel, si el byte 56 vale `0x81`. TRCC lo usa para nombrar sus archivos (`Theme.dc<id>`) |

Ejemplo real [V]:

```text
12 34 56 78 53 53 43 52 4d 2d 56 31 00 00 00 00 00 00 00 00 57 33 38 9b 07 00 00 00 20 00 00 00
06 00 00 00 02 00 00 00 00 00 00 00 01 00 00 00 xx xx xx xx 00 00 00 00 81 00 00 00 00 00 00 00
```

### Modelo → resolución

TRCC parte de 480 × 480 y la cambia según el PM y el SUB [L, `protocol.py`
`_PM_TO_FBL_OVERRIDES`, `_FBL_224_BY_PM`, `_FBL_192_BY_PM`, `_PM_SUB_TO_FBL`;
nombres en `dev/decompiler/AUDIT_DISCOVERY.md`]. Vitral usa la misma tabla
(`src/Vitral.Core/Dispositivo/Catalogo.cs`):

| PM | SUB | Modelo | Resolución | Imagen |
|---|---|---|---|---|
| 1 | 48 / 49 | — | 1600 × 720 / 1920 × 462 | JPEG |
| 1 | otro | Grand Vision | 480 × 480 | JPEG |
| 3 | — | Core Vision | 480 × 480 | JPEG |
| 4 | 1 | Hyper Vision | 480 × 480 | JPEG |
| 5 | — | Mjolnir Vision | 320 × 240 | JPEG |
| 6 | 1 / 2 | Frozen Warframe Ultra / Frozen Vision V2 | 480 × 480, montada girada 180° | JPEG |
| 7 | 1 / 2 | Stream Vision / Mjolnir Vision Pro | 640 × 480 | JPEG |
| 9, 11, 20 | — | — | 854 × 480 | JPEG |
| 10, 16 | — | — | 960 × 540 | JPEG |
| 12 | — | — | 800 × 480 | JPEG |
| 13, 17, 18 | — | — | 960 × 320 | JPEG |
| 14 | — | — | 640 × 480 | JPEG |
| 15 | — | — | 640 × 172 | RGB565 |
| 32 | — | — | 320 × 320 | RGB565 |
| 50 | — | — | 320 × 240 | RGB565 big-endian |
| 63, 64 | — | — | 1600 × 720 | JPEG |
| 65, 66 | — | — | 1920 × 462 | JPEG |
| 68 | — | — | 1280 × 480 | JPEG |
| 69 | — | — | 1920 × 440 | JPEG |
| cualquier otro | — | — | 480 × 480 | JPEG |

Los modelos que la comunidad ya reportó en esta familia [L, `REFERENCE_DEVICES.md`]:
Grand Vision 360, Mjolnir Vision 360, Levita Vision, Wonder Vision Pro 360,
Frozen Warframe Pro, Hyper Vision 360, Peerless Vision 360 y Phantom Spirit 120
Vision EVO.

## Cada cuadro

Una cabecera de 64 bytes y la imagen [L][T][O]:

```text
Offset  Tipo        Valor
0-3     bytes       12 34 56 78
4-7     uint32 LE   2 = JPEG, 3 = RGB565
8-11    uint32 LE   ancho
12-15   uint32 LE   alto
16-55   —           ceros
56-59   uint32 LE   2
60-63   uint32 LE   largo de la imagen
64…     bytes       la imagen
```

- JPEG baseline normal; Vitral usa calidad 90.
- El ancho y alto de la cabecera tienen que ser los del JPEG. Si no, el panel pinta
  solo la parte en que se cruzan, sin dar error [L, issue #262].
- RGB565 (PM 15, 32 y 50) va sin compresión, 2 bytes por píxel [L]. Vitral todavía
  no lo soporta.

## Envío

- El cuadro completo sale por `0x01` en trozos de 16 KiB [L][T].
- Si el total es múltiplo de 512, va además un paquete vacío (ZLP) [L][T][O].
- Hay que revisar cuántos bytes salieron: una escritura corta se ve igual que una
  pantalla muerta [L].
- La pantalla no responde a los cuadros [L].

## Ritmo

- Sin cuadros, el firmware vuelve al logo a los 2-3 s [L]. Por eso hay que mandar
  siempre, aunque la imagen no cambie.
- TRCC reenvía cada 150 ms [L]; trvision va a 30 fps [T]; oled-webui reenvía cada
  1,5 s [O]. Vitral redibuja solo cuando algo cambia y reenvía el último JPEG cada
  segundo.

## Brillo y giro

`USBLCDNEW.exe` define comandos de giro (6) y brillo (8), pero nadie ha visto a TRCC
usarlos con esta pantalla [L]. TRCC oscurece y gira la imagen antes de mandarla
[L, `REFERENCE_TECHNICAL.md`], y Vitral hace lo mismo. Las Frozen Warframe Ultra
(PM 6) vienen montadas giradas 180° y TRCC lo compensa [L, `protocol.py`
`encode_pm_bases`].

## Temas de TRCC (`Theme.zt`)

TRCC guarda la animación de cada tema en
`C:\TRCCCAP\Data\USBLCD\Theme<ancho><alto>\<tema>\Theme.zt`, con los cuadros ya en
JPEG [L, `dev/decompiler/AUDIT_VIDEO.md` §1.4]:

```text
byte        0xDC
int32 LE    cantidad de cuadros
int32 LE ×n marca de tiempo acumulada de cada cuadro, en ms
por cuadro: int32 LE largo y ese largo de bytes JPEG
```

Probado con un tema de 122 cuadros a ~24 fps [V]: la duración de cada cuadro es su
marca menos la anterior, y el archivo termina justo en el último cuadro.
