import type { Caja } from "../tema/tipos";

export interface Guia {
  eje: "x" | "y";
  posicion: number;
}

export interface Ajuste {
  dx: number;
  dy: number;
  guias: Guia[];
}

export const UMBRAL = 6;

function puntos(caja: Caja, eje: "x" | "y"): number[] {
  return eje === "x"
    ? [caja.x, caja.x + caja.ancho / 2, caja.x + caja.ancho]
    : [caja.y, caja.y + caja.alto / 2, caja.y + caja.alto];
}

export function pegar(
  movida: Caja,
  dx: number,
  dy: number,
  otras: readonly Caja[],
  lienzo: { ancho: number; alto: number },
  umbral = UMBRAL,
): Ajuste {
  const destino: Caja = { ...movida, x: movida.x + dx, y: movida.y + dy };
  const guias: Guia[] = [];
  const corregido = { x: dx, y: dy };

  for (const eje of ["x", "y"] as const) {
    const tamano = eje === "x" ? lienzo.ancho : lienzo.alto;
    const candidatas = [0, tamano / 2, tamano, ...otras.filter((o) => o.id !== movida.id).flatMap((o) => puntos(o, eje))];
    let mejor: { distancia: number; correccion: number; posicion: number } | null = null;
    for (const p of puntos(destino, eje)) {
      for (const c of candidatas) {
        const distancia = Math.abs(c - p);
        if (distancia <= umbral && (!mejor || distancia < mejor.distancia)) {
          mejor = { distancia, correccion: c - p, posicion: c };
        }
      }
    }
    if (mejor) {
      corregido[eje] += mejor.correccion;
      guias.push({ eje, posicion: mejor.posicion });
    }
  }
  return { dx: corregido.x, dy: corregido.y, guias };
}
