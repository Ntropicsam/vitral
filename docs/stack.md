# Arquitectura

```text
Vitral.exe (sin permisos de administrador, en %LOCALAPPDATA%\Vitral)
├─ Bandeja        menú, arranque con Windows, actualizaciones (Velopack)
├─ Motor          dibuja el tema con SkiaSharp, lo pasa a JPEG y lo manda
├─ Pantalla       WinUSB directo: handshake, cuadros, reconexión
├─ Temas          %APPDATA%\Vitral\Temas, validados al leerlos
└─ Editor         WebView2 con React; existe solo mientras la ventana está abierta

VitralSensores.exe (servicio de Windows, en Archivos de programa\Vitral\Sensores)
└─ LibreHardwareMonitor + PawnIO → tubería local «Vitral.Sensores», solo lecturas
```

## Por qué así

- **La temperatura de la CPU exige administrador**; el resto no. Por eso lo único
  que corre con privilegios es un servicio pequeño, en una carpeta donde solo un
  administrador escribe, que no acepta otra cosa que pedidos de lectura. La app,
  el editor web y los archivos de los temas nunca corren elevados.
- **.NET 10** porque LibreHardwareMonitor es .NET: el servicio lo usa directo. La
  app también es .NET (WinForms para la bandeja, WebView2 para el editor).
- **Velopack** instala por usuario y actualiza desde las versiones de GitHub, sin
  pedir permisos.
- **SkiaSharp** dibuja y codifica el JPEG. La vista previa del editor es el mismo
  JPEG que va a la pantalla.

## Consumo

- Con un tema quieto, el motor redibuja solo cuando cambia una cifra y reenvía el
  último JPEG cada segundo.
- Sin pantalla conectada y sin editor, no dibuja ni lee sensores.
- El servicio solo actualiza el hardware de los sensores que el tema usa (con el
  editor abierto, todos).
- Con el editor cerrado, la app pide a Windows el modo de ahorro (EcoQoS) y suelta
  la memoria que le sobra.
- Los videos y GIF se pasan una vez a JPEG al importarlos: reproducirlos es
  decodificar, dibujar las cifras encima y volver a codificar.
- En un Ryzen 5 5600T, con un GIF de 24 fps y dos cifras, el proceso de Vitral usó
  73 MB y 0,5 % del procesador con el editor abierto.
