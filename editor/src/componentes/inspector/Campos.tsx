import { IconAlignCenter, IconAlignLeft, IconAlignRight } from "@tabler/icons-react";
import { editarTema, useEstado } from "../../estado/tienda";
import { useT } from "../../i18n";
import { textoSensor } from "../../tema/formato";
import { FUENTES } from "../../tema/fuentes";
import { actualizarCapa } from "../../tema/operaciones";
import type { Alineacion, Capa, CapaMedidor, CapaSensor, CapaTexto, FormatoValor, PosicionEtiqueta } from "../../tema/tipos";
import { SelectorColor } from "../ui/Color";
import { Campo, CampoNumero, CampoTexto, Casilla, Deslizador, Segmentos } from "../ui/Controles";
import { BuscadorSensor } from "./BuscadorSensor";

function useCambiar(capa: Capa) {
  return (cambios: Partial<Capa>) => editarTema((t) => actualizarCapa(t, capa.id, cambios));
}

function Alinear({ valor, onCambio }: { valor: Alineacion; onCambio: (v: Alineacion) => void }) {
  const t = useT();
  return (
    <Segmentos
      etiqueta={t("inspector.alineacion")}
      valor={valor}
      onCambio={onCambio}
      opciones={[
        { valor: "Izquierda", texto: <IconAlignLeft size={16} aria-hidden />, nombre: t("inspector.izquierda") },
        { valor: "Centro", texto: <IconAlignCenter size={16} aria-hidden />, nombre: t("inspector.centro") },
        { valor: "Derecha", texto: <IconAlignRight size={16} aria-hidden />, nombre: t("inspector.derecha") },
      ]}
    />
  );
}

export function SelectorFuente({ etiqueta, valor, muestra, onCambio }: { etiqueta: string; valor: string; muestra: string; onCambio: (id: string) => void }) {
  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-texto-2">{etiqueta}</span>
      <div role="radiogroup" aria-label={etiqueta} className="grid grid-cols-4 gap-1.5">
        {FUENTES.map((f) => (
          <button
            key={f.id}
            type="button"
            role="radio"
            aria-checked={f.id === valor}
            aria-label={f.nombre}
            title={f.nombre}
            onClick={() => onCambio(f.id)}
            className="h-9 truncate rounded-control border border-borde bg-campo px-1 text-[16px] text-texto hover:border-texto-3 aria-checked:outline-2 aria-checked:outline-offset-1 aria-checked:outline-acento"
            style={{ fontFamily: `"${f.familia}"` }}
          >
            {muestra}
          </button>
        ))}
      </div>
    </div>
  );
}

function ComoSeMuestra({ capa, onCambio }: { capa: CapaSensor | CapaMedidor; onCambio: (f: FormatoValor) => void }) {
  const t = useT();
  const tipo = useEstado((e) => e.sensores.find((s) => s.id === capa.sensorId)?.tipo ?? "Otro");
  const valor = useEstado((e) => e.valores[capa.sensorId]);
  return (
    <Segmentos
      etiqueta={t("inspector.comoSeMuestra")}
      valor={capa.formato}
      onCambio={onCambio}
      opciones={[
        { valor: "Corto", texto: <span className="cifras text-[14px]">{textoSensor(valor ?? 52.3, tipo, "Corto")}</span> },
        { valor: "Detallado", texto: <span className="cifras text-[14px]">{textoSensor(valor ?? 52.3, tipo, "Detallado")}</span> },
      ]}
    />
  );
}

export function Legibilidad({ capa }: { capa: CapaSensor | CapaTexto }) {
  const t = useT();
  const medida = useEstado((e) => (e.legibilidad?.capaId === capa.id ? e.legibilidad : null));
  const cambiar = useCambiar(capa);
  return (
    <div className="flex flex-col gap-2 border-t border-borde pt-3">
      <div className="flex items-baseline justify-between">
        <span className="text-texto-2">{t("legibilidad.titulo")}</span>
        {medida && (
          <span className={`cifras text-[14px] ${medida.suficiente ? "text-exito" : "text-aviso"}`}>
            {medida.contraste.toFixed(1).replace(".", ",")} : 1
          </span>
        )}
      </div>
      <p className="text-[12px] leading-snug text-texto-3">
        {!medida ? t("legibilidad.midiendo") : medida.suficiente ? t("legibilidad.bien") : t("legibilidad.mal")}
      </p>
      <Casilla etiqueta={t("legibilidad.velo")} marcada={capa.velo} onCambio={(velo) => cambiar({ velo })} />
    </div>
  );
}

function Seccion({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-4 border-t border-borde pt-4" aria-label={titulo}>
      <h3 className="text-[13px] font-semibold text-texto">{titulo}</h3>
      {children}
    </section>
  );
}

export function CamposSensor({ capa }: { capa: CapaSensor }) {
  const t = useT();
  const cambiar = useCambiar(capa);
  return (
    <>
      <BuscadorSensor valor={capa.sensorId} onCambio={(sensorId) => cambiar({ sensorId })} />
      <ComoSeMuestra capa={capa} onCambio={(formato) => cambiar({ formato })} />
      <SelectorFuente etiqueta={t("inspector.fuente")} valor={capa.fuente} muestra="52°" onCambio={(fuente) => cambiar({ fuente })} />
      <Deslizador etiqueta={t("inspector.tamano")} valor={capa.tamano} min={16} max={220} unidad=" px" onCambio={(tamano) => cambiar({ tamano })} />
      <Alinear valor={capa.alineacion} onCambio={(alineacion) => cambiar({ alineacion })} />
      <SelectorColor etiqueta={t("inspector.color")} valor={capa.color} onCambio={(color) => cambiar({ color })} />
      <Seccion titulo={t("inspector.etiqueta")}>
        <CampoTexto etiqueta={t("inspector.textoEtiqueta")} valor={capa.etiqueta} max={60} onCambio={(etiqueta) => cambiar({ etiqueta })} />
        <Segmentos<PosicionEtiqueta>
          etiqueta={t("inspector.posicion")}
          valor={capa.posicionEtiqueta}
          onCambio={(posicionEtiqueta) => cambiar({ posicionEtiqueta })}
          opciones={[
            { valor: "Encima", texto: t("inspector.encima") },
            { valor: "Debajo", texto: t("inspector.debajo") },
            { valor: "Lado", texto: t("inspector.lado") },
          ]}
        />
        <Deslizador etiqueta={t("inspector.tamano")} valor={capa.tamanoEtiqueta} min={10} max={120} unidad=" px" onCambio={(tamanoEtiqueta) => cambiar({ tamanoEtiqueta })} />
        <SelectorFuente etiqueta={t("inspector.fuente")} valor={capa.fuenteEtiqueta} muestra="CPU" onCambio={(fuenteEtiqueta) => cambiar({ fuenteEtiqueta })} />
        <SelectorColor etiqueta={t("inspector.colorEtiqueta")} valor={capa.colorEtiqueta} onCambio={(colorEtiqueta) => cambiar({ colorEtiqueta })} />
      </Seccion>
      <Legibilidad capa={capa} />
    </>
  );
}

export function CamposTexto({ capa }: { capa: CapaTexto }) {
  const t = useT();
  const cambiar = useCambiar(capa);
  return (
    <>
      <CampoTexto etiqueta={t("inspector.texto")} valor={capa.texto} onCambio={(texto) => cambiar({ texto })} />
      <SelectorFuente etiqueta={t("inspector.fuente")} valor={capa.fuente} muestra="Aa" onCambio={(fuente) => cambiar({ fuente })} />
      <Deslizador etiqueta={t("inspector.tamano")} valor={capa.tamano} min={10} max={200} unidad=" px" onCambio={(tamano) => cambiar({ tamano })} />
      <Alinear valor={capa.alineacion} onCambio={(alineacion) => cambiar({ alineacion })} />
      <SelectorColor etiqueta={t("inspector.color")} valor={capa.color} onCambio={(color) => cambiar({ color })} />
      <Legibilidad capa={capa} />
    </>
  );
}

export function CamposMedidor({ capa }: { capa: CapaMedidor }) {
  const t = useT();
  const cambiar = useCambiar(capa);
  return (
    <>
      <BuscadorSensor valor={capa.sensorId} onCambio={(sensorId) => cambiar({ sensorId })} />
      <Segmentos
        etiqueta={t("inspector.forma")}
        valor={capa.estilo}
        onCambio={(estilo) => cambiar({ estilo })}
        opciones={[{ valor: "Anillo", texto: t("inspector.anillo") }, { valor: "Barra", texto: t("inspector.barra") }]}
      />
      <Campo etiqueta={t("inspector.escala")}>
        <div className="flex gap-2">
          <CampoNumero etiqueta={t("inspector.desde")} valor={capa.minimo} onCambio={(minimo) => cambiar({ minimo })} />
          <CampoNumero etiqueta={t("inspector.hasta")} valor={capa.maximo} onCambio={(maximo) => cambiar({ maximo })} />
        </div>
      </Campo>
      <Deslizador etiqueta={t(capa.estilo === "Anillo" ? "inspector.diametro" : "inspector.largo")} valor={capa.ancho} min={40} max={600} unidad=" px" onCambio={(ancho) => cambiar({ ancho })} />
      <Deslizador etiqueta={t("inspector.grosor")} valor={capa.grosor} min={2} max={60} unidad=" px" onCambio={(grosor) => cambiar({ grosor })} />
      <SelectorColor etiqueta={t("inspector.color")} valor={capa.color} onCambio={(color) => cambiar({ color })} />
      <Casilla etiqueta={t("inspector.mostrarCifra")} marcada={capa.mostrarValor} onCambio={(mostrarValor) => cambiar({ mostrarValor })} />
      {capa.mostrarValor && (
        <>
          <ComoSeMuestra capa={capa} onCambio={(formato) => cambiar({ formato })} />
          <SelectorFuente etiqueta={t("inspector.fuente")} valor={capa.fuente} muestra="38%" onCambio={(fuente) => cambiar({ fuente })} />
        </>
      )}
    </>
  );
}
