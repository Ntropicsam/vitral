# Diseño de Vitral

El editor se llama por dentro «Mesa de luz»: la pantalla en el centro, como una
diapositiva sobre una mesa iluminada, y todo lo demás a los lados. Es una
herramienta, así que gana la claridad.

## Editor de temas

Una ventana con tres columnas y una barra arriba:

- **Barra**: nombre del tema y si está guardado; estado de la pantalla (modelo,
  resolución, fps actuales); un solo botón principal, «Aplicar a la pantalla».
- **Izquierda, capas** de arriba abajo, con el valor en vivo de cada sensor al
  lado. Abajo, botones de sensor, texto y medidor, y la tira de miniaturas
  «Tus temas».
- **Centro, la pantalla**: el mismo JPEG que va al panel, a 4:3. Las capas se
  eligen haciendo clic en la lista o en la propia pantalla; la elegida lleva un
  contorno y una manija para el tamaño. Guías de alineación al mover. Debajo,
  la línea de tiempo del GIF o del video (reproducir y pausar, cuadro por
  cuadro); en pausa la pantalla baja a 1 fps.
- **Derecha, inspector** de la capa elegida: sensor, formato («52°» o
  «52,3 °C»), tamaño, color (muestras con nombre) y **legibilidad**: el
  contraste de las cifras contra el cuadro más claro del fondo, con el aviso y
  la opción de un velo oscuro detrás cuando baja de 4,5:1. Al final, el brillo
  de la pantalla.

La legibilidad es lo que diferencia a Vitral de TRCC, donde las temperaturas
muchas veces no se leen sobre el fondo: el editor nunca deja una cifra ilegible
sin decirlo.

## Contenido de la pantalla

- Fuente por defecto de las cifras: **Barlow Semi Condensed** 600, cifras
  tabulares (no bailan al cambiar). Condensada para que quepan grandes en 640 px
  y legible de lejos; licencia OFL, se puede empaquetar.
- Colores sugeridos: hueso `#f4efe6`, ámbar `#ffb020`, hielo `#7ad3ff`, coral
  `#ff6b5a`; etiquetas en `#9aa3b5`. El fondo lo pone el tema.

## Ventana del editor

Solo oscura, a propósito: el editor rodea una pantalla que brilla y una ventana
clara la apagaría. Los tokens viven en `editor/src/estilos.css` (`@theme`).

- **Fuente**: Segoe UI Variable, la de Windows 11. Es una app de Windows y debe
  sentirse nativa, sin fuente que cargar. Las cifras de la interfaz (valores de
  sensores, fps, contraste) van en Barlow Semi Condensed con cifras tabulares,
  la misma de la pantalla.
- **Escala**: 13 px el cuerpo (densidad de herramienta de escritorio, como el
  Explorador), 12 px las ayudas, 14 px los títulos de sección y las cifras.
- **Radios**: 6 px los controles, 10 px los menús; los paños del ícono, 22 %.
- **Espaciado**: rejilla de 4 px; columnas de 236 / flexible / 292 px; ventana
  mínima de 1040 × 640.
- **Paleta y contraste** (los pares están en `docs/contraste.json`; todos pasan
  WCAG 2.2 AA):

  | Token | Valor | Contra | Contraste |
  |---|---|---|---|
  | texto | `#ecebe6` | panel `#181b21` | 14,5:1 |
  | texto-2 | `#a3a9b5` | panel / campo `#252a33` | 7,3:1 / 6,1:1 |
  | texto-3 (ayudas) | `#8a919e` | panel | 5,4:1 |
  | acento (selección y foco) | `#6ea8ff` | fondo `#121419` | 7,6:1 |
  | sobre-acento (botón principal) | `#0d0f14` | acento | 8,0:1 |
  | borde-fuerte (campos) | `#737b8b` | panel / campo | 4,1:1 / 3,4:1 |
  | éxito / aviso / peligro | `#5dd39e` / `#ffb020` / `#ff7a6b` | panel | 9,3 / 9,4 / 6,1:1 |

- **Un solo botón con acento** por vista: «Aplicar a la pantalla».
- Piezas con Radix (Select, Slider, Checkbox, ContextMenu) detrás de componentes
  propios; iconos Tabler con un solo trazo; sin emojis.
- Movimiento: ninguno decorativo; `prefers-reduced-motion` apaga transiciones.
