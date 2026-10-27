import { textoSensor } from "../tema/formato";
import type { Caja, SensorInfo, Tema } from "../tema/tipos";
import type { Canal } from "./puente";
import type { DeVitral, HaciaVitral } from "./tipos";

const sensores: SensorInfo[] = [
  { id: "/amdcpu/0/temperature/2", nombre: "Core (Tctl/Tdie)", hardware: "AMD Ryzen 5 5600T", tipo: "Temperatura" },
  { id: "/amdcpu/0/load/0", nombre: "CPU Total", hardware: "AMD Ryzen 5 5600T", tipo: "Carga" },
  { id: "/gpu-amd/0/temperature/0", nombre: "GPU Core", hardware: "AMD Radeon RX 9060 XT", tipo: "Temperatura" },
  { id: "/gpu-amd/0/temperature/7", nombre: "GPU Hot Spot", hardware: "AMD Radeon RX 9060 XT", tipo: "Temperatura" },
  { id: "/ram/data/0", nombre: "Memory Used", hardware: "Generic Memory", tipo: "Memoria" },
];

const temaInicial: Tema = {
  version: 1,
  nombre: "Neón",
  ancho: 640,
  alto: 480,
  fondo: { tipo: "Animacion", color: "#0d0f14", archivo: "fondo.cuadros", ajuste: "Cubrir" },
  capas: [
    { tipo: "sensor", id: "cpu", nombre: "CPU", visible: true, x: 600, y: 48, sensorId: sensores[0]!.id, etiqueta: "CPU", formato: "Corto", tamano: 88, color: "#f4efe6", tamanoEtiqueta: 22, colorEtiqueta: "#9aa3b5", alineacion: "Derecha", velo: false },
    { tipo: "sensor", id: "gpu", nombre: "GPU", visible: true, x: 600, y: 250, sensorId: sensores[2]!.id, etiqueta: "GPU", formato: "Corto", tamano: 88, color: "#f4efe6", tamanoEtiqueta: 22, colorEtiqueta: "#9aa3b5", alineacion: "Derecha", velo: false },
  ],
};

export function canalSimulado(): Canal {
  const oyentes = new Set<(m: DeVitral) => void>();
  let tema = temaInicial;
  let cuadro = 0;
  let pausado = false;
  let brillo = 100;
  const valores: Record<string, number> = {};
  const lienzo = document.createElement("canvas");
  lienzo.width = 640;
  lienzo.height = 480;
  const ctx = lienzo.getContext("2d")!;

  const emitir = (m: DeVitral) => setTimeout(() => oyentes.forEach((o) => o(m)), 0);

  function dibujar() {
    ctx.fillStyle = tema.fondo.color;
    ctx.fillRect(0, 0, 640, 480);
    ctx.fillStyle = "#2a2f3a";
    const sube = Math.sin((cuadro / 48) * Math.PI * 2) * 8;
    ctx.beginPath();
    ctx.ellipse(150, 480 + sube, 120, 220, 0, Math.PI, 0);
    ctx.fill();
    const cajas: Caja[] = [];
    for (const capa of tema.capas) {
      if (!capa.visible) continue;
      if (capa.tipo === "medidor") {
        cajas.push({ id: capa.id, x: capa.x, y: capa.y, ancho: capa.ancho, alto: capa.estilo === "Barra" ? capa.grosor * 2.6 : capa.ancho });
        ctx.strokeStyle = capa.color;
        ctx.lineWidth = capa.grosor;
        ctx.beginPath();
        ctx.arc(capa.x + capa.ancho / 2, capa.y + capa.ancho / 2, (capa.ancho - capa.grosor) / 2, 0.75 * Math.PI, 2.25 * Math.PI);
        ctx.stroke();
        continue;
      }
      const sensor = capa.tipo === "sensor" ? sensores.find((s) => s.id === capa.sensorId) : null;
      const texto = capa.tipo === "sensor" ? textoSensor(valores[capa.sensorId], sensor?.tipo ?? "Otro", capa.formato) : capa.texto;
      const etiqueta = capa.tipo === "sensor" ? capa.etiqueta : "";
      ctx.font = `600 ${capa.tamano}px "Barlow Semi Condensed", sans-serif`;
      const ancho = ctx.measureText(texto).width;
      const altoEtiqueta = etiqueta && capa.tipo === "sensor" ? capa.tamanoEtiqueta * 1.2 : 0;
      const alto = capa.tamano * 0.7 + altoEtiqueta;
      const x = capa.alineacion === "Derecha" ? capa.x - ancho : capa.alineacion === "Centro" ? capa.x - ancho / 2 : capa.x;
      if (capa.velo) {
        ctx.fillStyle = "rgba(13,15,20,0.72)";
        ctx.fillRect(x - 12, capa.y - 12, ancho + 24, alto + 24);
      }
      if (etiqueta && capa.tipo === "sensor") {
        ctx.font = `500 ${capa.tamanoEtiqueta}px "Barlow Semi Condensed", sans-serif`;
        ctx.fillStyle = capa.colorEtiqueta;
        ctx.fillText(etiqueta, x, capa.y + capa.tamanoEtiqueta * 0.7);
        ctx.font = `600 ${capa.tamano}px "Barlow Semi Condensed", sans-serif`;
      }
      ctx.fillStyle = capa.color;
      ctx.fillText(texto, x, capa.y + alto);
      cajas.push({ id: capa.id, x, y: capa.y, ancho, alto });
    }
    if (brillo < 100) {
      ctx.fillStyle = `rgba(0,0,0,${1 - brillo / 100})`;
      ctx.fillRect(0, 0, 640, 480);
    }
    const jpeg = lienzo.toDataURL("image/jpeg", 0.85).split(",")[1]!;
    emitir({ tipo: "vista", jpeg, cuadro, cajas });
  }

  function leer() {
    valores[sensores[0]!.id] = 48 + Math.random() * 9;
    valores[sensores[1]!.id] = 10 + Math.random() * 30;
    valores[sensores[2]!.id] = 58 + Math.random() * 7;
    valores[sensores[3]!.id] = 66 + Math.random() * 8;
    valores[sensores[4]!.id] = 11 + Math.random();
    emitir({ tipo: "valores", valores: { ...valores } });
  }

  function estado() {
    emitir({ tipo: "estado", conexion: "Conectada", modelo: "Mjolnir Vision Pro", ancho: 640, alto: 480, detalle: "", fps: pausado ? 1 : 24, cuadro, cuadros: 48, pausado });
  }

  const resumen = [{ id: "neon", nombre: "Neón", miniatura: null }, { id: "bosque", nombre: "Bosque", miniatura: null }];

  function recibir(m: HaciaVitral) {
    switch (m.tipo) {
      case "listo":
        emitir({ tipo: "sensores", leeCpu: false, lista: sensores });
        emitir({ tipo: "ajustes", brillo, rotacion: 0 });
        emitir({ tipo: "temas", activo: "neon", lista: resumen });
        emitir({ tipo: "tema", id: "neon", tema });
        leer();
        dibujar();
        estado();
        setInterval(() => { leer(); dibujar(); estado(); }, 1000);
        setInterval(() => { if (!pausado) { cuadro = (cuadro + 1) % 48; dibujar(); } }, 120);
        break;
      case "tema.cambiar":
        tema = m.tema;
        dibujar();
        setTimeout(() => emitir({ tipo: "guardado", id: "neon" }), 500);
        break;
      case "animacion.pausar":
        pausado = m.pausado;
        estado();
        break;
      case "animacion.ir":
        cuadro = m.cuadro;
        dibujar();
        break;
      case "legibilidad": {
        const capa = tema.capas.find((c) => c.id === m.capaId);
        const claro = capa && "color" in capa && capa.color === "#f4efe6" && !("velo" in capa && capa.velo);
        emitir({ tipo: "legibilidad", capaId: m.capaId, contraste: claro ? 3.8 : 9.6, suficiente: !claro });
        break;
      }
      case "lector.instalar":
        setTimeout(() => emitir({ tipo: "sensores", leeCpu: true, lista: sensores }), 1500);
        break;
      case "ajustes.cambiar":
        brillo = m.brillo ?? brillo;
        emitir({ tipo: "ajustes", brillo, rotacion: m.rotacion ?? 0 });
        dibujar();
        break;
      default:
        break;
    }
  }

  return {
    enviar: (m) => setTimeout(() => recibir(m), 0),
    escuchar(oyente) {
      oyentes.add(oyente);
      return () => oyentes.delete(oyente);
    },
  };
}
