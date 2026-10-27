import { useSyncExternalStore } from "react";
import { enviar } from "../puente/puente";
import type { AjustesVitral, DeVitral, EstadoPantalla, ModeloElegible, TemaResumen } from "../puente/tipos";
import type { Caja, SensorInfo, Tema } from "../tema/tipos";

export interface Estado {
  pantalla: EstadoPantalla | null;
  temas: TemaResumen[];
  activo: string | null;
  edicionId: string | null;
  tema: Tema | null;
  sensores: SensorInfo[];
  leeCpu: boolean;
  valores: Record<string, number>;
  vista: { url: string; cuadro: number; cajas: Caja[] } | null;
  legibilidad: { capaId: string | null; contraste: number; suficiente: boolean } | null;
  ajustes: AjustesVitral;
  modelos: ModeloElegible[];
  dialogo: "ajustes" | "nuevo" | null;
  seleccion: string | null;
  guardado: "guardado" | "pendiente";
  error: string | null;
  importando: { mensaje: string; progreso: number } | null;
}

const inicial: Estado = {
  pantalla: null,
  temas: [],
  activo: null,
  edicionId: null,
  tema: null,
  sensores: [],
  leeCpu: true,
  valores: {},
  vista: null,
  legibilidad: null,
  ajustes: {
    brillo: 100,
    rotacion: 0,
    modoJuego: true,
    idioma: "auto",
    idiomaEfectivo: navigator.language.startsWith("es") ? "es" : "en",
    modeloForzado: null,
    arranque: false,
    trccArrancaSolo: false,
    version: "",
    lector: { incluido: false, activo: false, version: null, desactualizado: false },
    actualizacion: { instalado: false, nueva: null, revisando: false },
  },
  modelos: [],
  dialogo: null,
  seleccion: null,
  guardado: "guardado",
  error: null,
  importando: null,
};

let estado = inicial;
const oyentes = new Set<() => void>();

export function obtener(): Estado {
  return estado;
}

export function cambiar(parcial: Partial<Estado> | ((e: Estado) => Partial<Estado>)): void {
  const cambios = typeof parcial === "function" ? parcial(estado) : parcial;
  estado = { ...estado, ...cambios };
  oyentes.forEach((o) => o());
}

function suscribir(oyente: () => void): () => void {
  oyentes.add(oyente);
  return () => oyentes.delete(oyente);
}

export function useEstado<T>(selector: (e: Estado) => T): T {
  return useSyncExternalStore(suscribir, () => selector(estado));
}

let urlVista: string | null = null;

function urlDeJpeg(base64: string): string {
  const binario = atob(base64);
  const bytes = new Uint8Array(binario.length);
  for (let i = 0; i < binario.length; i++) bytes[i] = binario.charCodeAt(i);
  if (urlVista) URL.revokeObjectURL(urlVista);
  urlVista = URL.createObjectURL(new Blob([bytes], { type: "image/jpeg" }));
  return urlVista;
}

export function recibir(m: DeVitral): void {
  switch (m.tipo) {
    case "estado": {
      const { tipo: _, ...pantalla } = m;
      cambiar({ pantalla });
      break;
    }
    case "temas":
      cambiar({ temas: m.lista, activo: m.activo });
      break;
    case "tema":
      cambiar((e) => ({
        edicionId: m.id,
        tema: m.tema,
        guardado: "guardado",
        seleccion: e.edicionId === m.id && m.tema.capas.some((c) => c.id === e.seleccion) ? e.seleccion : null,
        importando: null,
      }));
      break;
    case "sensores":
      cambiar({ sensores: m.lista, leeCpu: m.leeCpu });
      break;
    case "valores":
      cambiar({ valores: m.valores });
      break;
    case "vista":
      cambiar({ vista: { url: urlDeJpeg(m.jpeg), cuadro: m.cuadro, cajas: m.cajas } });
      break;
    case "legibilidad":
      cambiar({ legibilidad: { capaId: m.capaId, contraste: m.contraste, suficiente: m.suficiente } });
      break;
    case "guardado":
      if (m.id === estado.edicionId && estado.guardado === "pendiente" && !cambioEnCamino) cambiar({ guardado: "guardado" });
      break;
    case "ajustes":
      {
        const { tipo: _, ...ajustes } = m;
        cambiar({ ajustes });
      }
      break;
    case "modelos":
      cambiar({ modelos: m.lista });
      break;
    case "error":
      cambiar({ error: m.mensaje, importando: null });
      break;
  }
}

let cambioEnCamino = false;
let cuadroPedido = 0;

export function editarTema(cambio: (t: Tema) => Tema): void {
  const actual = estado.tema;
  if (!actual) return;
  cambiar({ tema: cambio(actual), guardado: "pendiente" });
  cambioEnCamino = true;
  if (cuadroPedido) return;
  cuadroPedido = requestAnimationFrame(() => {
    cuadroPedido = 0;
    cambioEnCamino = false;
    if (estado.tema) enviar({ tipo: "tema.cambiar", tema: estado.tema });
  });
}
