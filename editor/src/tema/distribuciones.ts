import type { Distribucion, Marco } from "./tipos";

export const DISTRIBUCIONES: Distribucion[] = ["Completa", "MarcoIzquierda", "MarcoArriba", "MarcoCentrado", "SoloDatos"];

const RELATIVOS: Record<Distribucion, Marco | null> = {
  Completa: null,
  MarcoIzquierda: { x: 0, y: 0, ancho: 400 / 640, alto: 1 },
  MarcoArriba: { x: 0, y: 0, ancho: 1, alto: 320 / 480 },
  MarcoCentrado: { x: 60 / 640, y: 40 / 480, ancho: 520 / 640, alto: 300 / 480 },
  SoloDatos: null,
};

export function marcoDe(distribucion: Distribucion, ancho: number, alto: number): Marco | null {
  const r = RELATIVOS[distribucion];
  if (!r) return null;
  return { x: Math.round(r.x * ancho), y: Math.round(r.y * alto), ancho: Math.round(r.ancho * ancho), alto: Math.round(r.alto * alto) };
}

export function distribucionDe(marco: Marco | null, ancho: number, alto: number): Distribucion | null {
  if (!marco) return "Completa";
  return (
    DISTRIBUCIONES.find((d) => {
      const m = marcoDe(d, ancho, alto);
      return m && Math.abs(m.x - marco.x) <= 2 && Math.abs(m.y - marco.y) <= 2 && Math.abs(m.ancho - marco.ancho) <= 2 && Math.abs(m.alto - marco.alto) <= 2;
    }) ?? null
  );
}

export type Forma = "masAncho" | "masAlto" | "parecido";

export function formaDe(aspectoFondo: number, aspectoPantalla: number): Forma {
  const razon = aspectoFondo / aspectoPantalla;
  if (razon > 1.12) return "masAncho";
  if (razon < 0.88) return "masAlto";
  return "parecido";
}

export function sugerir(aspectoFondo: number, aspectoPantalla: number): Distribucion[] {
  switch (formaDe(aspectoFondo, aspectoPantalla)) {
    case "masAncho":
      return ["MarcoArriba", "MarcoCentrado", "Completa"];
    case "masAlto":
      return ["MarcoIzquierda", "MarcoCentrado", "Completa"];
    default:
      return ["Completa", "MarcoCentrado", "MarcoIzquierda"];
  }
}

export function tamanoDelFondo(marco: Marco | null, ancho: number, alto: number): { ancho: number; alto: number } {
  return marco ? { ancho: Math.round(marco.ancho), alto: Math.round(marco.alto) } : { ancho, alto };
}
