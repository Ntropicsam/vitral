import { cambiar } from "../estado/tienda";
import { enviar } from "../puente/puente";

export const MAXIMO_ARCHIVO = 50 * 1024 * 1024;
export const MAXIMO_VIDEO_SEGUNDOS = 300;
export const CUADROS_POR_SEGUNDO = 24;

export function esVideo(archivo: File): boolean {
  return archivo.type.startsWith("video/") || /\.(mp4|webm|mov|mkv|m4v)$/i.test(archivo.name);
}

function base64(datos: ArrayBuffer): string {
  const bytes = new Uint8Array(datos);
  let texto = "";
  for (let i = 0; i < bytes.length; i += 0x8000) texto += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
  return btoa(texto);
}

export async function importarArchivo(archivo: File, lienzo: { ancho: number; alto: number }): Promise<void> {
  if (esVideo(archivo)) {
    await importarVideo(archivo, lienzo);
    return;
  }
  if (archivo.size > MAXIMO_ARCHIVO) {
    cambiar({ error: "Ese archivo pesa más de 50 MB. Prueba con uno más liviano." });
    return;
  }
  cambiar({ importando: { mensaje: `Trayendo ${archivo.name}…`, progreso: 0 } });
  enviar({ tipo: "fondo.archivo", nombre: archivo.name, datos: base64(await archivo.arrayBuffer()) });
}

function esperar(el: HTMLVideoElement, evento: "loadeddata" | "seeked"): Promise<void> {
  return new Promise((resolver, rechazar) => {
    const ok = () => { limpiar(); resolver(); };
    const mal = () => { limpiar(); rechazar(new Error("El video no se pudo leer.")); };
    const limpiar = () => { el.removeEventListener(evento, ok); el.removeEventListener("error", mal); };
    el.addEventListener(evento, ok, { once: true });
    el.addEventListener("error", mal, { once: true });
  });
}

async function importarVideo(archivo: File, lienzo: { ancho: number; alto: number }): Promise<void> {
  const url = URL.createObjectURL(archivo);
  const video = document.createElement("video");
  video.muted = true;
  video.preload = "auto";
  video.src = url;
  try {
    await esperar(video, "loadeddata");
    const duracion = Math.min(video.duration || 0, MAXIMO_VIDEO_SEGUNDOS);
    const total = Math.max(1, Math.floor(duracion * CUADROS_POR_SEGUNDO));
    const canvas = new OffscreenCanvas(lienzo.ancho, lienzo.alto);
    const ctx = canvas.getContext("2d")!;
    const escala = Math.max(lienzo.ancho / video.videoWidth, lienzo.alto / video.videoHeight);
    const w = video.videoWidth * escala;
    const h = video.videoHeight * escala;
    const ms = Math.round(1000 / CUADROS_POR_SEGUNDO);

    enviar({ tipo: "fondo.video.inicio" });
    for (let i = 0; i < total; i++) {
      video.currentTime = i / CUADROS_POR_SEGUNDO;
      await esperar(video, "seeked");
      ctx.fillStyle = "#000";
      ctx.fillRect(0, 0, lienzo.ancho, lienzo.alto);
      ctx.drawImage(video, (lienzo.ancho - w) / 2, (lienzo.alto - h) / 2, w, h);
      const blob = await canvas.convertToBlob({ type: "image/jpeg", quality: 0.88 });
      enviar({ tipo: "fondo.video.cuadro", jpeg: base64(await blob.arrayBuffer()), duracion: ms });
      if (i % 6 === 0) cambiar({ importando: { mensaje: `Convirtiendo ${archivo.name}…`, progreso: i / total } });
    }
    cambiar({ importando: { mensaje: "Guardando el fondo…", progreso: 1 } });
    enviar({ tipo: "fondo.video.fin" });
  } catch (e) {
    cambiar({ importando: null, error: e instanceof Error ? e.message : "El video no se pudo leer." });
  } finally {
    URL.revokeObjectURL(url);
  }
}
