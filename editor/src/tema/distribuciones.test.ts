import { describe, expect, it } from "vitest";
import { agrupar } from "../componentes/inspector/BuscadorSensor";
import { distribucionDe, formaDe, marcoDe, sugerir, tamanoDelFondo } from "./distribuciones";
import type { SensorInfo } from "./tipos";

describe("distribuciones", () => {
  it("escala el marco a la pantalla", () => {
    expect(marcoDe("MarcoIzquierda", 640, 480)).toEqual({ x: 0, y: 0, ancho: 400, alto: 480 });
    expect(marcoDe("MarcoArriba", 320, 320)).toEqual({ x: 0, y: 0, ancho: 320, alto: 213 });
    expect(marcoDe("Completa", 640, 480)).toBeNull();
  });

  it("reconoce la distribución de un marco", () => {
    expect(distribucionDe(null, 640, 480)).toBe("Completa");
    expect(distribucionDe({ x: 61, y: 39, ancho: 520, alto: 301 }, 640, 480)).toBe("MarcoCentrado");
    expect(distribucionDe({ x: 10, y: 10, ancho: 100, alto: 100 }, 640, 480)).toBeNull();
  });

  it("sugiere según la forma del fondo", () => {
    expect(formaDe(16 / 9, 4 / 3)).toBe("masAncho");
    expect(formaDe(9 / 16, 4 / 3)).toBe("masAlto");
    expect(formaDe(1.3, 4 / 3)).toBe("parecido");
    expect(sugerir(16 / 9, 4 / 3)[0]).toBe("MarcoArriba");
    expect(sugerir(9 / 16, 4 / 3)[0]).toBe("MarcoIzquierda");
    expect(sugerir(4 / 3, 4 / 3)[0]).toBe("Completa");
  });

  it("importa el fondo al tamaño del marco", () => {
    expect(tamanoDelFondo({ x: 0, y: 0, ancho: 400, alto: 480 }, 640, 480)).toEqual({ ancho: 400, alto: 480 });
    expect(tamanoDelFondo(null, 640, 480)).toEqual({ ancho: 640, alto: 480 });
  });
});

describe("agrupar sensores", () => {
  const sensores: SensorInfo[] = [
    { id: "a", nombre: "CPU Core", hardware: "Ryzen", tipo: "Carga", agregado: "promedio" },
    { id: "b", nombre: "CPU Core #1", hardware: "Ryzen", tipo: "Carga" },
    { id: "c", nombre: "Tctl", hardware: "Ryzen", tipo: "Temperatura" },
    { id: "d", nombre: "Motherboard", hardware: "ASUS PRIME B550M-A", tipo: "Temperatura" },
  ];

  it("ordena por dispositivo y tipo, con lo unificado al final", () => {
    const grupos = agrupar(sensores, null);
    expect(grupos.map((g) => g.clave)).toEqual(["Ryzen|Temperatura", "Ryzen|Carga", "ASUS PRIME B550M-A|Temperatura"]);
    expect(grupos[1]!.sensores.map((s) => s.id)).toEqual(["b", "a"]);
  });

  it("filtra por dispositivo", () => {
    expect(agrupar(sensores, "ASUS PRIME B550M-A").map((g) => g.hardware)).toEqual(["ASUS PRIME B550M-A"]);
  });
});
