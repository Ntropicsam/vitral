export type Alineacion = "Izquierda" | "Centro" | "Derecha";
export type FormatoValor = "Corto" | "Detallado";
export type EstiloMedidor = "Anillo" | "Barra";
export type TipoFondo = "Color" | "Imagen" | "Animacion";
export type AjusteFondo = "Cubrir" | "Contener" | "Estirar";
export type PosicionEtiqueta = "Encima" | "Debajo" | "Lado";
export type Distribucion = "Completa" | "MarcoIzquierda" | "MarcoArriba" | "MarcoCentrado" | "SoloDatos";

interface CapaBase {
  id: string;
  nombre: string;
  visible: boolean;
  x: number;
  y: number;
}

export interface CapaSensor extends CapaBase {
  tipo: "sensor";
  sensorId: string;
  etiqueta: string;
  formato: FormatoValor;
  tamano: number;
  color: string;
  fuente: string;
  tamanoEtiqueta: number;
  colorEtiqueta: string;
  fuenteEtiqueta: string;
  posicionEtiqueta: PosicionEtiqueta;
  alineacion: Alineacion;
  velo: boolean;
}

export interface CapaTexto extends CapaBase {
  tipo: "texto";
  texto: string;
  tamano: number;
  color: string;
  fuente: string;
  alineacion: Alineacion;
  velo: boolean;
}

export interface CapaMedidor extends CapaBase {
  tipo: "medidor";
  sensorId: string;
  estilo: EstiloMedidor;
  minimo: number;
  maximo: number;
  ancho: number;
  grosor: number;
  color: string;
  colorPista: string;
  fuente: string;
  mostrarValor: boolean;
  formato: FormatoValor;
}

export type Capa = CapaSensor | CapaTexto | CapaMedidor;
export type TipoCapa = Capa["tipo"];

export interface Marco {
  x: number;
  y: number;
  ancho: number;
  alto: number;
}

export interface Fondo {
  tipo: TipoFondo;
  color: string;
  archivo: string | null;
  ajuste: AjusteFondo;
  marco: Marco | null;
  radioMarco: number;
}

export interface Tema {
  version: number;
  nombre: string;
  ancho: number;
  alto: number;
  fondo: Fondo;
  capas: Capa[];
}

export type TipoSensor =
  | "Temperatura"
  | "Carga"
  | "Reloj"
  | "Ventilador"
  | "Potencia"
  | "Memoria"
  | "Datos"
  | "Voltaje"
  | "Otro";

export interface SensorInfo {
  id: string;
  nombre: string;
  hardware: string;
  tipo: TipoSensor;
  agregado?: "promedio" | "maximo" | null;
}

export interface Caja {
  id: string;
  x: number;
  y: number;
  ancho: number;
  alto: number;
}
