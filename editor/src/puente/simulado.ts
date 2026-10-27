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
    { tipo: "medidor", id: "carga", nombre: "Carga de la CPU", visible: true, x: 40, y: 110, sensorId: sensores[1]!.id, estilo: "Anillo", minimo: 0, maximo: 100, ancho: 240, grosor: 18, color: "#ffb020", colorPista: "#262b35", mostrarValor: true, formato: "Corto" },
    { tipo: "sensor", id: "cpu", nombre: "CPU", visible: true, x: 600, y: 70, sensorId: sensores[0]!.id, etiqueta: "CPU", formato: "Corto", tamano: 96, color: "#f4efe6", tamanoEtiqueta: 22, colorEtiqueta: "#9aa3b5", alineacion: "Derecha", velo: false },
    { tipo: "sensor", id: "gpu", nombre: "GPU", visible: true, x: 600, y: 260, sensorId: sensores[2]!.id, etiqueta: "GPU", formato: "Corto", tamano: 96, color: "#7ad3ff", tamanoEtiqueta: 22, colorEtiqueta: "#9aa3b5", alineacion: "Derecha", velo: false },
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
    const paso = (cuadro / 48) * 640;
    ctx.fillStyle = "#151a24";
    for (let x = -640; x < 1280; x += 160) {
      ctx.beginPath();
      ctx.moveTo(x + paso, 480);
      ctx.lineTo(x + paso + 80, 480);
      ctx.lineTo(x + paso + 320, 0);
      ctx.lineTo(x + paso + 240, 0);
      ctx.fill();
    }
    const cajas: Caja[] = [];
    for (const capa of tema.capas) {
      if (!capa.visible) continue;
      if (capa.tipo === "medidor") {
        cajas.push({ id: capa.id, x: capa.x, y: capa.y, ancho: capa.ancho, alto: capa.estilo === "Barra" ? capa.grosor * 2.6 : capa.ancho });
        const cx = capa.x + capa.ancho / 2;
        const cy = capa.y + capa.ancho / 2;
        const radio = (capa.ancho - capa.grosor) / 2;
        const valor = valores[capa.sensorId] ?? 0;
        const fraccion = Math.min(1, Math.max(0, (valor - capa.minimo) / (capa.maximo - capa.minimo)));
        ctx.lineCap = "round";
        ctx.lineWidth = capa.grosor;
        ctx.strokeStyle = capa.colorPista;
        ctx.beginPath();
        ctx.arc(cx, cy, radio, 0.75 * Math.PI, 2.25 * Math.PI);
        ctx.stroke();
        ctx.strokeStyle = capa.color;
        ctx.beginPath();
        ctx.arc(cx, cy, radio, 0.75 * Math.PI, (0.75 + 1.5 * fraccion) * Math.PI);
        ctx.stroke();
        ctx.fillStyle = capa.color;
        ctx.font = `600 ${capa.ancho * 0.24}px "Barlow Semi Condensed", sans-serif`;
        ctx.textAlign = "center";
        ctx.fillText(textoSensor(valor, "Carga", capa.formato), cx, cy + capa.ancho * 0.08);
        ctx.textAlign = "left";
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
        void Promise.all([
          document.fonts.load('600 96px "Barlow Semi Condensed"'),
          document.fonts.load('500 22px "Barlow Semi Condensed"'),
        ]).then(dibujar);
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
