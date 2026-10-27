import { IconUpload } from "@tabler/icons-react";
import { useEffect, useRef, useState } from "react";
import { cambiar, editarTema, obtener, useEstado } from "../../estado/tienda";
import { importarArchivo } from "../../importar/importar";
import { enviar } from "../../puente/puente";
import type { AjusteFondo, Capa } from "../../tema/tipos";
import { SelectorColor } from "../ui/Color";
import { CampoTexto, Deslizador, Segmentos, Selector } from "../ui/Controles";
import { ID_FONDO } from "../PanelCapas";
import { CamposMedidor, CamposSensor, CamposTexto } from "./Campos";

const NOMBRE_FONDO: Record<string, string> = { Color: "Solo color", Imagen: "Imagen", Animacion: "Animación" };

function CamposFondo() {
  const fondo = useEstado((e) => e.tema?.fondo);
  const importando = useEstado((e) => e.importando);
  const archivo = useRef<HTMLInputElement>(null);
  if (!fondo) return null;

  return (
    <>
      <div className="flex flex-col gap-1.5">
        <span className="text-texto-2">Fondo actual</span>
        <span>{NOMBRE_FONDO[fondo.tipo]}</span>
      </div>
      <input
        ref={archivo}
        type="file"
        accept="image/*,video/*,.gif,.webp,.zt"
        className="sr-only"
        tabIndex={-1}
        aria-hidden
        onChange={(e) => {
          const f = e.target.files?.[0];
          const tema = obtener().tema;
          e.target.value = "";
          if (f && tema) void importarArchivo(f, { ancho: tema.ancho, alto: tema.alto });
        }}
      />
      <button
        type="button"
        disabled={importando !== null}
        onClick={() => archivo.current?.click()}
        className="flex h-9 items-center justify-center gap-2 rounded-control border border-borde-fuerte text-texto hover:bg-realce disabled:opacity-60"
      >
        <IconUpload size={16} aria-hidden />
        Cambiar el fondo
      </button>
      <p className="text-[12px] leading-snug text-texto-3">Imagen, GIF, video (hasta cinco minutos) o el Theme.zt de un tema de TRCC.</p>
      {importando && (
        <div className="flex flex-col gap-1.5" role="status">
          <span className="text-texto-2">{importando.mensaje}</span>
          <div className="h-1 overflow-hidden rounded-full bg-campo">
            <div className="h-full bg-acento" style={{ width: `${Math.round(importando.progreso * 100)}%` }} />
          </div>
        </div>
      )}
      {fondo.tipo !== "Color" && (
        <Segmentos<AjusteFondo>
          etiqueta="Ajuste"
          valor={fondo.ajuste}
          onCambio={(ajuste) => editarTema((t) => ({ ...t, fondo: { ...t.fondo, ajuste } }))}
          opciones={[{ valor: "Cubrir", texto: "Llenar" }, { valor: "Contener", texto: "Entero" }, { valor: "Estirar", texto: "Estirar" }]}
        />
      )}
      <SelectorColor etiqueta="Color de fondo" valor={fondo.color} onCambio={(color) => editarTema((t) => ({ ...t, fondo: { ...t.fondo, color } }))} />
    </>
  );
}

function AvisoCpu() {
  const leeCpu = useEstado((e) => e.leeCpu);
  const [pedido, setPedido] = useState(false);
  if (leeCpu) return null;
  return (
    <section className="m-3 mb-0 flex flex-col gap-2 rounded-panel border border-borde bg-realce p-3" aria-label="Temperatura de la CPU">
      <span className="font-semibold">Falta la temperatura de la CPU</span>
      <p className="text-[12px] leading-snug text-texto-2">
        Windows solo la deja leer con permisos de administrador. Vitral instala un lector pequeño que solo entrega lecturas; el resto sigue sin permisos.
      </p>
      <button
        type="button"
        disabled={pedido}
        onClick={() => {
          setPedido(true);
          enviar({ tipo: "lector.instalar" });
          setTimeout(() => setPedido(false), 15_000);
        }}
        className="h-8 self-start rounded-control border border-borde-fuerte px-3 text-texto hover:bg-campo disabled:opacity-60"
      >
        {pedido ? "Esperando el permiso de Windows…" : "Activarla"}
      </button>
    </section>
  );
}

function CamposPantalla() {
  const ajustes = useEstado((e) => e.ajustes);
  return (
    <section className="flex flex-col gap-4 border-t border-borde px-4 py-4" aria-label="Pantalla">
      <Deslizador
        etiqueta="Brillo de la pantalla"
        valor={ajustes.brillo}
        min={10}
        max={100}
        paso={5}
        unidad=" %"
        onCambio={(brillo) => {
          cambiar({ ajustes: { ...ajustes, brillo } });
          enviar({ tipo: "ajustes.cambiar", brillo });
        }}
      />
      <Selector
        etiqueta="Girar la imagen"
        valor={String(ajustes.rotacion)}
        grupos={[{ titulo: "Según cómo quedó montada la bomba", opciones: [
          { valor: "0", texto: "Sin girar" },
          { valor: "90", texto: "90°" },
          { valor: "180", texto: "180°" },
          { valor: "270", texto: "270°" },
        ] }]}
        onCambio={(v) => enviar({ tipo: "ajustes.cambiar", rotacion: Number(v) })}
      />
    </section>
  );
}

function claveLegibilidad(capa: Capa | null): string | null {
  if (!capa || capa.tipo === "medidor") return null;
  return `${capa.color}|${capa.velo}|${capa.tamano}|${capa.x}|${capa.y}|${capa.tipo === "texto" ? capa.texto : capa.sensorId}`;
}

export function Inspector() {
  const seleccion = useEstado((e) => e.seleccion);
  const capa = useEstado((e) => e.tema?.capas.find((c) => c.id === e.seleccion) ?? null);
  const capaId = capa?.id ?? null;
  const clave = claveLegibilidad(capa);

  useEffect(() => {
    if (!capaId || !clave) return;
    const t = setTimeout(() => enviar({ tipo: "legibilidad", capaId }), 250);
    return () => clearTimeout(t);
  }, [capaId, clave]);

  return (
    <aside className="flex min-h-0 flex-col border-l border-borde bg-panel" aria-label="Ajustes de la capa">
      <AvisoCpu />
      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto px-4 py-4">
        {seleccion === ID_FONDO ? (
          <>
            <h2 className="text-[14px] font-semibold">Fondo</h2>
            <CamposFondo />
          </>
        ) : capa ? (
          <>
            <CampoTexto etiqueta="Nombre de la capa" valor={capa.nombre} max={60} onCambio={(nombre) => editarTema((t) => ({ ...t, capas: t.capas.map((c) => (c.id === capa.id ? { ...c, nombre } : c)) }))} />
            {capa.tipo === "sensor" && <CamposSensor capa={capa} />}
            {capa.tipo === "texto" && <CamposTexto capa={capa} />}
            {capa.tipo === "medidor" && <CamposMedidor capa={capa} />}
          </>
        ) : (
          <p className="text-texto-3">Elige una capa en la lista o en la pantalla para ajustarla.</p>
        )}
      </div>
      <CamposPantalla />
    </aside>
  );
}
