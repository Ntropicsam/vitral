import type { Caja, Distribucion, SensorInfo, Tema } from "../tema/tipos";

export type Conexion = "Buscando" | "Conectada" | "NoConectada" | "Ocupada" | "NoSoportada" | "Error";

export interface EstadoPantalla {
  conexion: Conexion;
  modelo: string | null;
  detectado?: string | null;
  forzado?: boolean;
  ancho: number | null;
  alto: number | null;
  detalle: string;
  fps: number;
  cuadro: number;
  cuadros: number;
  pausado: boolean;
}

export interface TemaResumen {
  id: string;
  nombre: string;
  miniatura: string | null;
}

export type Idioma = "auto" | "es" | "en";

export interface AjustesVitral {
  brillo: number;
  rotacion: number;
  modoJuego: boolean;
  idioma: Idioma;
  idiomaEfectivo: "es" | "en";
  modeloForzado: string | null;
  arranque: boolean;
  trccArrancaSolo: boolean;
  version: string;
  lector: { incluido: boolean; activo: boolean; version: string | null; desactualizado: boolean };
  actualizacion: { instalado: boolean; nueva: string | null; revisando: boolean };
}

export interface ModeloElegible {
  clave: string;
  nombre: string;
  ancho: number;
  alto: number;
}

export type DeVitral =
  | ({ tipo: "estado" } & EstadoPantalla)
  | { tipo: "temas"; activo: string | null; lista: TemaResumen[] }
  | { tipo: "tema"; id: string; tema: Tema }
  | { tipo: "sensores"; leeCpu: boolean; lista: SensorInfo[] }
  | { tipo: "valores"; valores: Record<string, number> }
  | { tipo: "vista"; jpeg: string; cuadro: number; cajas: Caja[] }
  | { tipo: "legibilidad"; capaId: string | null; contraste: number; suficiente: boolean }
  | { tipo: "guardado"; id: string }
  | ({ tipo: "ajustes" } & AjustesVitral)
  | { tipo: "modelos"; lista: ModeloElegible[] }
  | { tipo: "error"; mensaje: string };

export type HaciaVitral =
  | { tipo: "listo" }
  | { tipo: "tema.abrir"; id: string }
  | { tipo: "tema.cambiar"; tema: Tema }
  | { tipo: "tema.nuevo"; distribucion: Distribucion }
  | { tipo: "tema.duplicar"; id: string }
  | { tipo: "tema.borrar"; id: string }
  | { tipo: "tema.aplicar" }
  | { tipo: "fondo.archivo"; nombre: string; datos: string }
  | { tipo: "fondo.video.inicio" }
  | { tipo: "fondo.video.cuadro"; jpeg: string; duracion: number }
  | { tipo: "fondo.video.fin" }
  | { tipo: "animacion.pausar"; pausado: boolean }
  | { tipo: "animacion.ir"; cuadro: number }
  | { tipo: "legibilidad"; capaId: string }
  | {
      tipo: "ajustes.cambiar";
      brillo?: number;
      rotacion?: number;
      modoJuego?: boolean;
      idioma?: Idioma;
      arranque?: boolean;
      modeloForzado?: string | null;
    }
  | { tipo: "lector.instalar" }
  | { tipo: "trcc.desactivar" }
  | { tipo: "actualizacion.buscar" }
  | { tipo: "actualizacion.aplicar" };
