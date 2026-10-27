import { describe, expect, it } from "vitest";
import { pegar } from "../lienzo/guias";
import { textoSensor } from "./formato";
import {
  actualizarCapa,
  agregarCapa,
  borrarCapa,
  conTamano,
  crearCapa,
  duplicarCapa,
  etiquetaCorta,
  moverCapa,
  moverEnOrden,
} from "./operaciones";
import type { SensorInfo, Tema } from "./tipos";

const sensores: SensorInfo[] = [
  { id: "cpu", nombre: "Core (Tctl/Tdie)", hardware: "AMD Ryzen 5 5600T", tipo: "Temperatura" },
  { id: "gpu", nombre: "GPU Core", hardware: "AMD Radeon RX 9060 XT", tipo: "Temperatura" },
  { id: "carga", nombre: "CPU Total", hardware: "AMD Ryzen 5 5600T", tipo: "Carga" },
];

const vacio: Tema = {
  version: 1,
  nombre: "x",
  ancho: 640,
  alto: 480,
  fondo: { tipo: "Color", color: "#0d0f14", archivo: null, ajuste: "Cubrir" },
  capas: [],
};

describe("operaciones sobre el tema", () => {
  it("una capa de sensor nueva toma un sensor de temperatura que no esté usado", () => {
    const uno = agregarCapa(vacio, crearCapa("sensor", vacio, sensores));
    const dos = crearCapa("sensor", uno, sensores);
    expect(uno.capas[0]).toMatchObject({ sensorId: "cpu", etiqueta: "CPU" });
    expect(dos).toMatchObject({ sensorId: "gpu", etiqueta: "GPU" });
  });

  it("sin sensores la capa queda sin sensor, sin lanzar", () => {
    expect(crearCapa("medidor", vacio, [])).toMatchObject({ sensorId: "" });
  });

  it("actualizar no cambia el tipo ni el id aunque lleguen en los cambios", () => {
    const tema = agregarCapa(vacio, crearCapa("texto", vacio, sensores));
    const id = tema.capas[0]!.id;
    const nuevo = actualizarCapa(tema, id, { id: "otro", tamano: 60 } as never);
    expect(nuevo.capas[0]).toMatchObject({ id, tipo: "texto", tamano: 60 });
    expect(tema.capas[0]).toMatchObject({ tamano: 40 });
  });

  it("borrar, duplicar y reordenar", () => {
    let tema = agregarCapa(vacio, crearCapa("texto", vacio, sensores));
    tema = agregarCapa(tema, crearCapa("medidor", tema, sensores));
    const [a, b] = tema.capas.map((c) => c.id);
    expect(moverEnOrden(tema, a!, 1).capas.map((c) => c.id)).toEqual([b, a]);
    expect(moverEnOrden(tema, a!, -1)).toBe(tema);
    const { tema: conCopia, nuevoId } = duplicarCapa(tema, a!);
    expect(conCopia.capas[1]?.id).toBe(nuevoId);
    expect(duplicarCapa(tema, "no").nuevoId).toBeNull();
    expect(borrarCapa(tema, a!).capas).toHaveLength(1);
  });

  it("mover redondea a píxeles enteros e ignora capas que no están", () => {
    const tema = agregarCapa(vacio, crearCapa("texto", vacio, sensores));
    const id = tema.capas[0]!.id;
    expect(moverCapa(tema, id, 10.6, -3.2).capas[0]).toMatchObject({ x: 331, y: 157 });
    expect(moverCapa(tema, "no", 5, 5)).toBe(tema);
  });

  it("el tamaño de la manija respeta los topes de Vitral", () => {
    const texto = crearCapa("texto", vacio, sensores);
    const medidor = crearCapa("medidor", vacio, sensores);
    expect(conTamano(texto, 2)).toEqual({ tamano: 8 });
    expect(conTamano(texto, 9999)).toEqual({ tamano: 400 });
    expect(conTamano(medidor, 300.4)).toEqual({ ancho: 300 });
  });

  it("etiqueta corta según el hardware", () => {
    expect(etiquetaCorta(sensores[0]!)).toBe("CPU");
    expect(etiquetaCorta(sensores[1]!)).toBe("GPU");
    expect(etiquetaCorta({ id: "d", nombre: "Temperature", hardware: "Samsung SSD 980", tipo: "Temperatura" })).toBe("Temperature");
  });
});

describe("formato igual al de la pantalla", () => {
  it.each([
    [52.4, "Temperatura", "Corto", "52°"],
    [52.46, "Temperatura", "Detallado", "52,5 °C"],
    [-0.2, "Temperatura", "Corto", "0°"],
    [4250, "Reloj", "Corto", "4,3 GHz"],
    [1850, "Ventilador", "Corto", "1.850 RPM"],
    [11.24, "Memoria", "Corto", "11,2 GB"],
    [1.2, "Voltaje", "Corto", "1,20 V"],
  ] as const)("%s %s %s → %s", (valor, tipo, formato, esperado) => {
    expect(textoSensor(valor, tipo, formato)).toBe(esperado);
  });

  it("sin dato es una raya", () => {
    expect(textoSensor(null, "Temperatura")).toBe("–");
    expect(textoSensor(Number.NaN, "Carga")).toBe("–");
  });
});

describe("guías de alineación", () => {
  const lienzo = { ancho: 640, alto: 480 };
  const caja = { id: "a", x: 100, y: 100, ancho: 100, alto: 50 };

  it("se pega al centro del lienzo cuando está cerca", () => {
    const r = pegar(caja, 167, 0, [], lienzo);
    expect(r.dx).toBe(170);
    expect(r.guias).toContainEqual({ eje: "x", posicion: 320 });
  });

  it("se pega al borde de otra caja", () => {
    const otra = { id: "b", x: 400, y: 310, ancho: 80, alto: 40 };
    const r = pegar(caja, 0, 197, [otra], lienzo);
    expect(r.dy).toBe(200);
    expect(r.guias).toContainEqual({ eje: "y", posicion: 350 });
  });

  it("lejos de todo no corrige ni muestra guías", () => {
    const r = pegar(caja, 40, 40, [], lienzo);
    expect(r).toEqual({ dx: 40, dy: 40, guias: [] });
  });

  it("no se pega a sí misma", () => {
    const r = pegar(caja, 3, 0, [caja], lienzo);
    expect(r.dx).toBe(3);
  });
});
