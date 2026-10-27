import type { FormatoValor, TipoSensor } from "./tipos";

export const SIN_DATO = "–";

function numero(valor: number, decimales: number): string {
  const factor = 10 ** decimales;
  let r = (Math.sign(valor) * Math.round(Math.abs(valor) * factor)) / factor;
  if (r === 0) r = 0;
  const [entero = "0", fraccion] = Math.abs(r).toFixed(decimales).split(".");
  const miles = entero.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
  return `${r < 0 ? "-" : ""}${miles}${fraccion ? `,${fraccion}` : ""}`;
}

export function textoSensor(valor: number | null | undefined, tipo: TipoSensor, formato: FormatoValor = "Corto"): string {
  if (valor == null || !Number.isFinite(valor)) return SIN_DATO;
  const det = formato === "Detallado";
  switch (tipo) {
    case "Temperatura":
      return det ? `${numero(valor, 1)} °C` : `${numero(valor, 0)}°`;
    case "Carga":
      return `${numero(valor, det ? 1 : 0)} %`;
    case "Reloj":
      return valor >= 1000 ? `${numero(valor / 1000, det ? 2 : 1)} GHz` : `${numero(valor, 0)} MHz`;
    case "Ventilador":
      return `${numero(valor, 0)} RPM`;
    case "Potencia":
      return `${numero(valor, det ? 1 : 0)} W`;
    case "Memoria":
    case "Datos":
      return `${numero(valor, 1)} GB`;
    case "Voltaje":
      return `${numero(valor, det ? 3 : 2)} V`;
    default:
      return numero(valor, det ? 1 : 0);
  }
}
