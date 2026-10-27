import { IconUpload } from "@tabler/icons-react";
import { useEffect, useRef, useState } from "react";
import { cambiar, editarTema, obtener, useEstado } from "../../estado/tienda";
import { useT } from "../../i18n";
import { importarArchivo } from "../../importar/importar";
import { enviar } from "../../puente/puente";
import { DISTRIBUCIONES, distribucionDe, marcoDe, tamanoDelFondo } from "../../tema/distribuciones";
import type { AjusteFondo, Capa, Distribucion } from "../../tema/tipos";
import { SelectorColor } from "../ui/Color";
import { CampoTexto, Deslizador, Segmentos, Selector } from "../ui/Controles";
import { ID_FONDO } from "../PanelCapas";
import { CamposMedidor, CamposSensor, CamposTexto } from "./Campos";

function CamposFondo() {
  const t = useT();
  const fondo = useEstado((e) => e.tema?.fondo);
  const ancho = useEstado((e) => e.tema?.ancho ?? 640);
  const alto = useEstado((e) => e.tema?.alto ?? 480);
  const importando = useEstado((e) => e.importando);
  const archivo = useRef<HTMLInputElement>(null);
  if (!fondo) return null;
  const actual = distribucionDe(fondo.marco, ancho, alto);

  return (
    <>
      <div className="flex flex-col gap-1.5">
        <span className="text-texto-2">{t("fondo.actual")}</span>
        <span>{t(`fondo.${fondo.tipo}`)}</span>
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
          if (f && tema) void importarArchivo(f, tamanoDelFondo(tema.fondo.marco, tema.ancho, tema.alto));
        }}
      />
      <button
        type="button"
        disabled={importando !== null}
        onClick={() => archivo.current?.click()}
        className="flex h-9 items-center justify-center gap-2 rounded-control border border-borde-fuerte text-texto hover:bg-realce disabled:opacity-60"
      >
        <IconUpload size={16} aria-hidden />
        {t("fondo.cambiar")}
      </button>
      <p className="text-[12px] leading-snug text-texto-3">{t("fondo.formatos")}</p>
      {importando && (
        <div className="flex flex-col gap-1.5" role="status">
          <span className="text-texto-2">{importando.mensaje}</span>
          <div className="h-1 overflow-hidden rounded-full bg-campo">
            <div className="h-full bg-acento" style={{ width: `${Math.round(importando.progreso * 100)}%` }} />
          </div>
        </div>
      )}
      <div className="flex flex-col gap-1.5">
        <span className="text-texto-2">{t("fondo.distribucion")}</span>
        <div role="radiogroup" aria-label={t("fondo.distribucion")} className="grid grid-cols-2 gap-1.5">
          {DISTRIBUCIONES.filter((d) => d !== "SoloDatos").map((d: Distribucion) => (
            <button
              key={d}
              type="button"
              role="radio"
              aria-checked={actual === d}
              onClick={() => editarTema((tm) => ({ ...tm, fondo: { ...tm.fondo, marco: marcoDe(d, tm.ancho, tm.alto) } }))}
              className="h-8 rounded-control border border-borde px-2 text-[12px] text-texto-2 hover:text-texto aria-checked:border-borde-fuerte aria-checked:bg-realce aria-checked:text-texto"
            >
              {t(`dist.${d}`)}
            </button>
          ))}
        </div>
        {fondo.marco && <p className="text-[12px] leading-snug text-texto-3">{t("fondo.marcoAyuda")}</p>}
      </div>
      {fondo.marco && (
        <Deslizador
          etiqueta={t("fondo.esquinas")}
          valor={fondo.radioMarco}
          min={0}
          max={60}
          unidad=" px"
          onCambio={(radioMarco) => editarTema((tm) => ({ ...tm, fondo: { ...tm.fondo, radioMarco } }))}
        />
      )}
      {fondo.tipo !== "Color" && (
        <Segmentos<AjusteFondo>
          etiqueta={t("fondo.ajuste")}
          valor={fondo.ajuste}
          onCambio={(ajuste) => editarTema((tm) => ({ ...tm, fondo: { ...tm.fondo, ajuste } }))}
          opciones={[{ valor: "Cubrir", texto: t("fondo.llenar") }, { valor: "Contener", texto: t("fondo.entero") }, { valor: "Estirar", texto: t("fondo.estirar") }]}
        />
      )}
      <SelectorColor etiqueta={t("fondo.color")} valor={fondo.color} onCambio={(color) => editarTema((tm) => ({ ...tm, fondo: { ...tm.fondo, color } }))} />
    </>
  );
}

function AvisoCpu() {
  const t = useT();
  const leeCpu = useEstado((e) => e.leeCpu);
  const [pedido, setPedido] = useState(false);
  if (leeCpu) return null;
  return (
    <section className="m-3 mb-0 flex flex-col gap-2 rounded-panel border border-borde bg-realce p-3" aria-label={t("cpu.falta")}>
      <span className="font-semibold">{t("cpu.falta")}</span>
      <p className="text-[12px] leading-snug text-texto-2">{t("cpu.explicacion")}</p>
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
        {pedido ? t("cpu.esperando") : t("cpu.activar")}
      </button>
    </section>
  );
}

export function CamposPantalla() {
  const t = useT();
  const ajustes = useEstado((e) => e.ajustes);
  return (
    <section className="flex flex-col gap-4 border-t border-borde px-4 py-4" aria-label={t("ajustes.pantalla")}>
      <Deslizador
        etiqueta={t("pantalla.brillo")}
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
        etiqueta={t("pantalla.girar")}
        valor={String(ajustes.rotacion)}
        grupos={[{ titulo: t("pantalla.segunMontaje"), opciones: [
          { valor: "0", texto: t("pantalla.sinGirar") },
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
  const etiqueta = capa.tipo === "sensor" ? `${capa.etiqueta}|${capa.tamanoEtiqueta}|${capa.posicionEtiqueta}|${capa.fuenteEtiqueta}` : "";
  return `${capa.color}|${capa.velo}|${capa.tamano}|${capa.x}|${capa.y}|${capa.fuente}|${etiqueta}|${capa.tipo === "texto" ? capa.texto : capa.sensorId}`;
}

export function Inspector() {
  const t = useT();
  const seleccion = useEstado((e) => e.seleccion);
  const capa = useEstado((e) => e.tema?.capas.find((c) => c.id === e.seleccion) ?? null);
  const capaId = capa?.id ?? null;
  const clave = claveLegibilidad(capa);

  useEffect(() => {
    if (!capaId || !clave) return;
    const espera = setTimeout(() => enviar({ tipo: "legibilidad", capaId }), 250);
    return () => clearTimeout(espera);
  }, [capaId, clave]);

  return (
    <aside className="flex min-h-0 flex-col border-l border-borde bg-panel" aria-label={t("inspector.nombreCapa")}>
      <AvisoCpu />
      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto px-4 py-4">
        {seleccion === ID_FONDO ? (
          <>
            <h2 className="text-[14px] font-semibold">{t("fondo.titulo")}</h2>
            <CamposFondo />
          </>
        ) : capa ? (
          <>
            <CampoTexto etiqueta={t("inspector.nombreCapa")} valor={capa.nombre} max={60} onCambio={(nombre) => editarTema((tm) => ({ ...tm, capas: tm.capas.map((c) => (c.id === capa.id ? { ...c, nombre } : c)) }))} />
            {capa.tipo === "sensor" && <CamposSensor capa={capa} />}
            {capa.tipo === "texto" && <CamposTexto capa={capa} />}
            {capa.tipo === "medidor" && <CamposMedidor capa={capa} />}
          </>
        ) : (
          <p className="text-texto-3">{t("inspector.vacio")}</p>
        )}
      </div>
      <CamposPantalla />
    </aside>
  );
}
