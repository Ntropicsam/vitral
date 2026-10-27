import type { Capa, CapaMedidor, CapaSensor, CapaTexto, SensorInfo, Tema, TipoCapa } from "./tipos";

export const COLORES = [
  { nombre: "Hueso", valor: "#f4efe6" },
  { nombre: "Ámbar", valor: "#ffb020" },
  { nombre: "Hielo", valor: "#7ad3ff" },
  { nombre: "Coral", valor: "#ff6b5a" },
] as const;

export function nuevoId(existentes: readonly string[]): string {
  let id: string;
  do {
    id = Math.random().toString(16).slice(2, 10).padEnd(8, "0");
  } while (existentes.includes(id));
  return id;
}

function sensorLibre(tema: Tema, sensores: readonly SensorInfo[]): string {
  const usados = new Set(tema.capas.map((c) => ("sensorId" in c ? c.sensorId : "")));
  const temperatura = sensores.filter((s) => s.tipo === "Temperatura");
  return (temperatura.find((s) => !usados.has(s.id)) ?? temperatura[0] ?? sensores[0])?.id ?? "";
}

export function crearCapa(tipo: TipoCapa, tema: Tema, sensores: readonly SensorInfo[]): Capa {
  const id = nuevoId(tema.capas.map((c) => c.id));
  const base = { id, visible: true, x: Math.round(tema.ancho / 2), y: Math.round(tema.alto / 3) };
  switch (tipo) {
    case "sensor": {
      const sensorId = sensorLibre(tema, sensores);
      const sensor = sensores.find((s) => s.id === sensorId);
      const capa: CapaSensor = {
        ...base,
        tipo: "sensor",
        nombre: sensor?.nombre ?? "Sensor",
        sensorId,
        etiqueta: sensor ? etiquetaCorta(sensor) : "",
        formato: "Corto",
        tamano: 88,
        color: "#f4efe6",
        tamanoEtiqueta: 22,
        colorEtiqueta: "#9aa3b5",
        alineacion: "Centro",
        velo: false,
      };
      return capa;
    }
    case "texto": {
      const capa: CapaTexto = {
        ...base,
        tipo: "texto",
        nombre: "Texto",
        texto: "Texto",
        tamano: 40,
        color: "#f4efe6",
        alineacion: "Centro",
        velo: false,
      };
      return capa;
    }
    case "medidor": {
      const sensorId = sensorLibre(tema, sensores);
      const capa: CapaMedidor = {
        ...base,
        x: Math.round(tema.ancho / 2 - 80),
        tipo: "medidor",
        nombre: "Medidor",
        sensorId,
        estilo: "Anillo",
        minimo: 0,
        maximo: 100,
        ancho: 160,
        grosor: 14,
        color: "#ffb020",
        colorPista: "#2a2f3a",
        mostrarValor: true,
        formato: "Corto",
      };
      return capa;
    }
  }
}

export function etiquetaCorta(sensor: SensorInfo): string {
  const h = sensor.hardware.toLowerCase();
  if (/radeon|geforce|rtx|gtx|arc|gpu/.test(h)) return "GPU";
  if (/ryzen|core|intel|amd|cpu/.test(h) && sensor.tipo !== "Memoria") return "CPU";
  return sensor.nombre;
}

export function agregarCapa(tema: Tema, capa: Capa): Tema {
  return { ...tema, capas: [...tema.capas, capa] };
}

export function actualizarCapa(tema: Tema, id: string, cambios: Partial<Capa>): Tema {
  return {
    ...tema,
    capas: tema.capas.map((c) => (c.id === id ? ({ ...c, ...cambios, tipo: c.tipo, id: c.id } as Capa) : c)),
  };
}

export function borrarCapa(tema: Tema, id: string): Tema {
  return { ...tema, capas: tema.capas.filter((c) => c.id !== id) };
}

export function duplicarCapa(tema: Tema, id: string): { tema: Tema; nuevoId: string | null } {
  const i = tema.capas.findIndex((c) => c.id === id);
  const original = tema.capas[i];
  if (!original) return { tema, nuevoId: null };
  const copia = { ...original, id: nuevoId(tema.capas.map((c) => c.id)), x: original.x + 16, y: original.y + 16 };
  const capas = [...tema.capas];
  capas.splice(i + 1, 0, copia);
  return { tema: { ...tema, capas }, nuevoId: copia.id };
}

export function moverEnOrden(tema: Tema, id: string, direccion: 1 | -1): Tema {
  const i = tema.capas.findIndex((c) => c.id === id);
  const j = i + direccion;
  if (i < 0 || j < 0 || j >= tema.capas.length) return tema;
  const capas = [...tema.capas];
  [capas[i], capas[j]] = [capas[j]!, capas[i]!];
  return { ...tema, capas };
}

export function moverCapa(tema: Tema, id: string, dx: number, dy: number): Tema {
  const capa = tema.capas.find((c) => c.id === id);
  if (!capa) return tema;
  return actualizarCapa(tema, id, { x: Math.round(capa.x + dx), y: Math.round(capa.y + dy) });
}

export function tamanoDe(capa: Capa): number {
  return capa.tipo === "medidor" ? capa.ancho : capa.tamano;
}

export function conTamano(capa: Capa, valor: number): Partial<Capa> {
  const v = Math.round(Math.min(capa.tipo === "medidor" ? 2000 : 400, Math.max(capa.tipo === "medidor" ? 16 : 8, valor)));
  return capa.tipo === "medidor" ? { ancho: v } : { tamano: v };
}
