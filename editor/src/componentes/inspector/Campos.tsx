import { IconAlignCenter, IconAlignLeft, IconAlignRight } from "@tabler/icons-react";
import { editarTema, useEstado } from "../../estado/tienda";
import { textoSensor } from "../../tema/formato";
import { actualizarCapa } from "../../tema/operaciones";
import type { Alineacion, Capa, CapaMedidor, CapaSensor, CapaTexto, FormatoValor } from "../../tema/tipos";
import { SelectorColor } from "../ui/Color";
import { Campo, CampoNumero, CampoTexto, Casilla, Deslizador, Segmentos, Selector } from "../ui/Controles";

function useCambiar(capa: Capa) {
  return (cambios: Partial<Capa>) => editarTema((t) => actualizarCapa(t, capa.id, cambios));
}

function SelectorSensor({ valor, onCambio }: { valor: string; onCambio: (id: string) => void }) {
  const sensores = useEstado((e) => e.sensores);
  const valores = useEstado((e) => e.valores);
  const grupos = [...new Set(sensores.map((s) => s.hardware))].map((hardware) => ({
    titulo: hardware,
    opciones: sensores
      .filter((s) => s.hardware === hardware)
      .map((s) => ({ valor: s.id, texto: s.nombre, detalle: textoSensor(valores[s.id], s.tipo) })),
  }));
  return <Selector etiqueta="Sensor" valor={valor} grupos={grupos} vacio={sensores.length ? "Elige un sensor" : "Cargando sensores…"} onCambio={onCambio} />;
}

function Alinear({ valor, onCambio }: { valor: Alineacion; onCambio: (v: Alineacion) => void }) {
  return (
    <Segmentos
      etiqueta="Alineación"
      valor={valor}
      onCambio={onCambio}
      opciones={[
        { valor: "Izquierda", texto: <IconAlignLeft size={16} aria-hidden />, nombre: "A la izquierda" },
        { valor: "Centro", texto: <IconAlignCenter size={16} aria-hidden />, nombre: "Al centro" },
        { valor: "Derecha", texto: <IconAlignRight size={16} aria-hidden />, nombre: "A la derecha" },
      ]}
    />
  );
}

function ComoSeMuestra({ capa, onCambio }: { capa: CapaSensor | CapaMedidor; onCambio: (f: FormatoValor) => void }) {
  const tipo = useEstado((e) => e.sensores.find((s) => s.id === capa.sensorId)?.tipo ?? "Otro");
  const valor = useEstado((e) => e.valores[capa.sensorId]);
  return (
    <Segmentos
      etiqueta="Cómo se muestra"
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
  const medida = useEstado((e) => (e.legibilidad?.capaId === capa.id ? e.legibilidad : null));
  const cambiar = useCambiar(capa);
  return (
    <div className="flex flex-col gap-2 border-t border-borde pt-3">
      <div className="flex items-baseline justify-between">
        <span className="text-texto-2">Legibilidad</span>
        {medida && (
          <span className={`cifras text-[14px] ${medida.suficiente ? "text-exito" : "text-aviso"}`}>
            {medida.contraste.toFixed(1).replace(".", ",")} : 1
          </span>
        )}
      </div>
      <p className="text-[12px] leading-snug text-texto-3">
        {!medida
          ? "Midiendo contra el fondo…"
          : medida.suficiente
            ? "Se lee bien hasta en el cuadro más claro del fondo."
            : "En el cuadro más claro del fondo cuesta leer. Prueba el velo o un color más oscuro."}
      </p>
      <Casilla etiqueta="Velo oscuro detrás" marcada={capa.velo} onCambio={(velo) => cambiar({ velo })} />
    </div>
  );
}

export function CamposSensor({ capa }: { capa: CapaSensor }) {
  const cambiar = useCambiar(capa);
  return (
    <>
      <SelectorSensor valor={capa.sensorId} onCambio={(sensorId) => cambiar({ sensorId })} />
      <ComoSeMuestra capa={capa} onCambio={(formato) => cambiar({ formato })} />
      <CampoTexto etiqueta="Etiqueta encima" valor={capa.etiqueta} max={60} onCambio={(etiqueta) => cambiar({ etiqueta })} />
      <Deslizador etiqueta="Tamaño" valor={capa.tamano} min={16} max={220} unidad=" px" onCambio={(tamano) => cambiar({ tamano })} />
      <Alinear valor={capa.alineacion} onCambio={(alineacion) => cambiar({ alineacion })} />
      <SelectorColor etiqueta="Color" valor={capa.color} onCambio={(color) => cambiar({ color })} />
      <Legibilidad capa={capa} />
    </>
  );
}

export function CamposTexto({ capa }: { capa: CapaTexto }) {
  const cambiar = useCambiar(capa);
  return (
    <>
      <CampoTexto etiqueta="Texto" valor={capa.texto} onCambio={(texto) => cambiar({ texto })} />
      <Deslizador etiqueta="Tamaño" valor={capa.tamano} min={10} max={200} unidad=" px" onCambio={(tamano) => cambiar({ tamano })} />
      <Alinear valor={capa.alineacion} onCambio={(alineacion) => cambiar({ alineacion })} />
      <SelectorColor etiqueta="Color" valor={capa.color} onCambio={(color) => cambiar({ color })} />
      <Legibilidad capa={capa} />
    </>
  );
}

export function CamposMedidor({ capa }: { capa: CapaMedidor }) {
  const cambiar = useCambiar(capa);
  return (
    <>
      <SelectorSensor valor={capa.sensorId} onCambio={(sensorId) => cambiar({ sensorId })} />
      <Segmentos
        etiqueta="Forma"
        valor={capa.estilo}
        onCambio={(estilo) => cambiar({ estilo })}
        opciones={[{ valor: "Anillo", texto: "Anillo" }, { valor: "Barra", texto: "Barra" }]}
      />
      <Campo etiqueta="Escala">
        <div className="flex gap-2">
          <CampoNumero etiqueta="Desde" valor={capa.minimo} onCambio={(minimo) => cambiar({ minimo })} />
          <CampoNumero etiqueta="Hasta" valor={capa.maximo} onCambio={(maximo) => cambiar({ maximo })} />
        </div>
      </Campo>
      <Deslizador etiqueta={capa.estilo === "Anillo" ? "Diámetro" : "Largo"} valor={capa.ancho} min={40} max={600} unidad=" px" onCambio={(ancho) => cambiar({ ancho })} />
      <Deslizador etiqueta="Grosor" valor={capa.grosor} min={2} max={60} unidad=" px" onCambio={(grosor) => cambiar({ grosor })} />
      <SelectorColor etiqueta="Color" valor={capa.color} onCambio={(color) => cambiar({ color })} />
      <Casilla etiqueta="Mostrar la cifra" marcada={capa.mostrarValor} onCambio={(mostrarValor) => cambiar({ mostrarValor })} />
      {capa.mostrarValor && <ComoSeMuestra capa={capa} onCambio={(formato) => cambiar({ formato })} />}
    </>
  );
}
