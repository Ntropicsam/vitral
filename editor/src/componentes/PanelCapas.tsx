import {
  IconArrowDown,
  IconArrowUp,
  IconCopy,
  IconEye,
  IconEyeOff,
  IconGauge,
  IconPhoto,
  IconTemperature,
  IconTrash,
  IconTypography,
} from "@tabler/icons-react";
import { cambiar, editarTema, obtener, useEstado } from "../estado/tienda";
import { traducir, useT } from "../i18n";
import { textoSensor } from "../tema/formato";
import { agregarCapa, borrarCapa, crearCapa, duplicarCapa, moverEnOrden } from "../tema/operaciones";
import type { Capa, TipoCapa } from "../tema/tipos";
import { TusTemas } from "./TusTemas";

const ICONOS = { sensor: IconTemperature, texto: IconTypography, medidor: IconGauge } as const;

export const ID_FONDO = "__fondo";

function Fila({ capa }: { capa: Capa }) {
  const t = useT();
  const seleccionada = useEstado((e) => e.seleccion === capa.id);
  const valor = useEstado((e) => ("sensorId" in capa ? e.valores[capa.sensorId] : undefined));
  const tipoSensor = useEstado((e) => ("sensorId" in capa ? e.sensores.find((s) => s.id === capa.sensorId)?.tipo : undefined));
  const Icono = ICONOS[capa.tipo];
  const detalle = "sensorId" in capa ? textoSensor(valor, tipoSensor ?? "Otro", capa.formato) : capa.tipo === "texto" ? capa.texto : "";

  return (
    <li className="group relative">
      <button
        type="button"
        aria-pressed={seleccionada}
        onClick={() => cambiar({ seleccion: capa.id })}
        className={`flex h-8 w-full items-center gap-2 rounded-control pl-2 pr-16 text-left ${seleccionada ? "bg-realce text-texto" : "text-texto-2 hover:bg-realce/60"} ${capa.visible ? "" : "opacity-60"}`}
      >
        <Icono size={16} aria-hidden className="shrink-0" />
        <span className="truncate">{capa.nombre}</span>
        <span className="cifras ml-auto shrink-0 truncate text-[13px] text-texto-3">{detalle}</span>
      </button>
      <button
        type="button"
        aria-label={t(capa.visible ? "capas.ocultar" : "capas.mostrar", { nombre: capa.nombre })}
        onClick={() => editarTema((tm) => ({ ...tm, capas: tm.capas.map((c) => (c.id === capa.id ? { ...c, visible: !c.visible } : c)) }))}
        className="absolute right-1 top-1 flex size-6 items-center justify-center rounded-control text-texto-3 hover:bg-campo hover:text-texto"
      >
        {capa.visible ? <IconEye size={15} aria-hidden /> : <IconEyeOff size={15} aria-hidden />}
      </button>
    </li>
  );
}

function agregar(tipo: TipoCapa) {
  const { tema, sensores, ajustes } = obtener();
  if (!tema) return;
  const idioma = ajustes.idiomaEfectivo;
  const capa = crearCapa(tipo, tema, sensores, {
    sensor: traducir(idioma, "capas.sensor"),
    texto: traducir(idioma, "capas.texto"),
    medidor: traducir(idioma, "capas.medidor"),
  });
  editarTema((t) => agregarCapa(t, capa));
  cambiar({ seleccion: capa.id });
}

export function accionesDeCapa(id: string) {
  return {
    subir: () => editarTema((t) => moverEnOrden(t, id, 1)),
    bajar: () => editarTema((t) => moverEnOrden(t, id, -1)),
    duplicar: () => {
      const tema = obtener().tema;
      if (!tema) return;
      const r = duplicarCapa(tema, id);
      editarTema(() => r.tema);
      cambiar({ seleccion: r.nuevoId });
    },
    borrar: () => {
      editarTema((t) => borrarCapa(t, id));
      cambiar({ seleccion: null });
    },
  };
}

export function PanelCapas() {
  const t = useT();
  const capas = useEstado((e) => e.tema?.capas);
  const seleccion = useEstado((e) => e.seleccion);
  const fondoSeleccionado = seleccion === ID_FONDO;
  const ordenadas = capas ? [...capas].reverse() : [];
  const acciones = seleccion && seleccion !== ID_FONDO ? accionesDeCapa(seleccion) : null;

  return (
    <aside className="flex min-h-0 flex-col border-r border-borde bg-panel" aria-label={t("capas.titulo")}>
      <div className="flex items-center justify-between px-3 pb-1.5 pt-3">
        <h2 className="text-[13px] font-semibold text-texto-2">{t("capas.titulo")}</h2>
        {acciones && (
          <div className="flex gap-0.5">
            <BotonIcono nombre={t("capas.subir")} onClick={acciones.subir}><IconArrowUp size={15} aria-hidden /></BotonIcono>
            <BotonIcono nombre={t("capas.bajar")} onClick={acciones.bajar}><IconArrowDown size={15} aria-hidden /></BotonIcono>
            <BotonIcono nombre={t("capas.duplicar")} onClick={acciones.duplicar}><IconCopy size={15} aria-hidden /></BotonIcono>
            <BotonIcono nombre={t("capas.borrar")} onClick={acciones.borrar}><IconTrash size={15} aria-hidden /></BotonIcono>
          </div>
        )}
      </div>
      <ul className="flex flex-col gap-0.5 overflow-y-auto px-2">
        {ordenadas.map((c) => <Fila key={c.id} capa={c} />)}
        <li>
          <button
            type="button"
            aria-pressed={fondoSeleccionado}
            onClick={() => cambiar({ seleccion: ID_FONDO })}
            className={`flex h-8 w-full items-center gap-2 rounded-control px-2 text-left ${fondoSeleccionado ? "bg-realce text-texto" : "text-texto-2 hover:bg-realce/60"}`}
          >
            <IconPhoto size={16} aria-hidden />
            {t("capas.fondo")}
          </button>
        </li>
      </ul>
      <div className="mt-2 grid grid-cols-3 gap-1 px-3">
        <BotonAgregar nombre={t("capas.sensor")} onClick={() => agregar("sensor")}><IconTemperature size={16} aria-hidden /></BotonAgregar>
        <BotonAgregar nombre={t("capas.texto")} onClick={() => agregar("texto")}><IconTypography size={16} aria-hidden /></BotonAgregar>
        <BotonAgregar nombre={t("capas.medidor")} onClick={() => agregar("medidor")}><IconGauge size={16} aria-hidden /></BotonAgregar>
      </div>
      <TusTemas />
    </aside>
  );
}

function BotonIcono({ nombre, onClick, children }: { nombre: string; onClick: () => void; children: React.ReactNode }) {
  return (
    <button type="button" aria-label={nombre} title={nombre} onClick={onClick} className="flex size-6 items-center justify-center rounded-control text-texto-3 hover:bg-campo hover:text-texto">
      {children}
    </button>
  );
}

function BotonAgregar({ nombre, onClick, children }: { nombre: string; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex h-8 items-center justify-center gap-1.5 rounded-control border border-borde text-texto-2 hover:border-borde-fuerte hover:text-texto"
    >
      {children}
      <span className="text-[12px]">{nombre}</span>
    </button>
  );
}
