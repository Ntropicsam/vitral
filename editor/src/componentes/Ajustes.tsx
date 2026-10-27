import { IconX } from "@tabler/icons-react";
import { Dialog } from "radix-ui";
import { useState } from "react";
import { cambiar, useEstado } from "../estado/tienda";
import { useT } from "../i18n";
import { enviar } from "../puente/puente";
import type { Idioma } from "../puente/tipos";
import { Casilla, Selector } from "./ui/Controles";

const boton = "h-8 shrink-0 rounded-control border border-borde-fuerte px-3 text-texto hover:bg-campo disabled:opacity-60";

function Seccion({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-4 border-t border-borde px-5 py-4 first:border-t-0" aria-label={titulo}>
      <h3 className="text-[13px] font-semibold text-texto-2">{titulo}</h3>
      {children}
    </section>
  );
}

function Fila({ titulo, detalle, children }: { titulo: string; detalle?: string; children?: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <div className="flex min-w-0 flex-col gap-0.5">
        <span>{titulo}</span>
        {detalle && <span className="text-[12px] leading-snug text-texto-3">{detalle}</span>}
      </div>
      {children}
    </div>
  );
}

function Dispositivo() {
  const t = useT();
  const modelos = useEstado((e) => e.modelos);
  const forzado = useEstado((e) => e.ajustes.modeloForzado);
  const detectado = useEstado((e) => e.pantalla?.detectado ?? null);
  return (
    <div className="flex flex-col gap-1.5">
      <Selector
        etiqueta={t("ajustes.dispositivo")}
        valor={forzado ?? "auto"}
        grupos={[{ titulo: t("ajustes.dispositivo"), opciones: [
          { valor: "auto", texto: detectado ? t("ajustes.automatico", { modelo: detectado }) : t("ajustes.automaticoSolo") },
          ...modelos.map((m) => ({ valor: m.clave, texto: `${m.nombre} · ${m.ancho} × ${m.alto}` })),
        ] }]}
        onCambio={(v) => enviar({ tipo: "ajustes.cambiar", modeloForzado: v === "auto" ? null : v })}
      />
      <span className="text-[12px] text-texto-3">{detectado ? t("ajustes.detectado", { modelo: detectado }) : t("ajustes.sinDetectar")}</span>
    </div>
  );
}

function Lector() {
  const t = useT();
  const lector = useEstado((e) => e.ajustes.lector);
  const [pedido, setPedido] = useState(false);
  const pedir = () => {
    setPedido(true);
    enviar({ tipo: "lector.instalar" });
    setTimeout(() => setPedido(false), 15_000);
  };
  if (!lector.incluido) return <Fila titulo={t("ajustes.cpu")} detalle={t("ajustes.cpuNoIncluido")} />;
  if (!lector.activo)
    return (
      <Fila titulo={t("ajustes.cpu")} detalle={t("ajustes.cpuInactiva")}>
        <button type="button" disabled={pedido} onClick={pedir} className={boton}>{pedido ? t("cpu.esperando") : t("cpu.activar")}</button>
      </Fila>
    );
  return (
    <Fila titulo={t("ajustes.cpu")} detalle={t("ajustes.cpuActiva", { version: lector.version ?? "?" })}>
      {lector.desactualizado ? (
        <button type="button" disabled={pedido} onClick={pedir} className={boton}>{pedido ? t("cpu.esperando") : t("ajustes.actualizarLector")}</button>
      ) : (
        <span className="text-exito">{t("ajustes.activa")}</span>
      )}
    </Fila>
  );
}

function Version() {
  const t = useT();
  const version = useEstado((e) => e.ajustes.version);
  const act = useEstado((e) => e.ajustes.actualizacion);
  const detalle = !act.instalado ? t("ajustes.sinInstalar") : act.nueva ? t("ajustes.nueva", { version: act.nueva }) : t("ajustes.alDia");
  return (
    <Fila titulo={t("ajustes.version", { version })} detalle={detalle}>
      {act.instalado &&
        (act.nueva ? (
          <button type="button" onClick={() => enviar({ tipo: "actualizacion.aplicar" })} className="h-8 shrink-0 rounded-control bg-acento px-3 font-semibold text-sobre-acento hover:brightness-110">
            {t("ajustes.reiniciar")}
          </button>
        ) : (
          <button type="button" disabled={act.revisando} onClick={() => enviar({ tipo: "actualizacion.buscar" })} className={boton}>
            {act.revisando ? t("ajustes.buscando") : t("ajustes.buscar")}
          </button>
        ))}
    </Fila>
  );
}

export function Ajustes() {
  const t = useT();
  const abierto = useEstado((e) => e.dialogo === "ajustes");
  const ajustes = useEstado((e) => e.ajustes);

  return (
    <Dialog.Root open={abierto} onOpenChange={(v) => cambiar({ dialogo: v ? "ajustes" : null })}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-40 bg-black/60" />
        <Dialog.Content
          aria-describedby={undefined}
          className="fixed left-1/2 top-1/2 z-50 flex max-h-[min(640px,90vh)] w-[min(520px,92vw)] -translate-x-1/2 -translate-y-1/2 flex-col rounded-panel border border-borde bg-panel"
        >
          <div className="flex h-12 shrink-0 items-center justify-between border-b border-borde px-5">
            <Dialog.Title className="text-[14px] font-semibold">{t("ajustes.titulo")}</Dialog.Title>
            <Dialog.Close aria-label={t("ajustes.cerrar")} className="flex size-8 items-center justify-center rounded-control text-texto-2 hover:bg-realce hover:text-texto">
              <IconX size={18} aria-hidden />
            </Dialog.Close>
          </div>
          <div className="min-h-0 overflow-y-auto">
            <Seccion titulo={t("ajustes.pantalla")}>
              <Dispositivo />
            </Seccion>
            <Seccion titulo={t("ajustes.sensores")}>
              <Lector />
            </Seccion>
            <Seccion titulo={t("ajustes.general")}>
              <Casilla etiqueta={t("ajustes.arranque")} marcada={ajustes.arranque} onCambio={(arranque) => enviar({ tipo: "ajustes.cambiar", arranque })} />
              <div className="flex flex-col gap-0.5">
                <Casilla etiqueta={t("ajustes.juegos")} marcada={ajustes.modoJuego} onCambio={(modoJuego) => enviar({ tipo: "ajustes.cambiar", modoJuego })} />
                <span className="pl-6 text-[12px] text-texto-3">{t("ajustes.juegosAyuda")}</span>
              </div>
              {ajustes.trccArrancaSolo && (
                <Fila titulo={t("ajustes.trcc")} detalle={t("ajustes.trccAyuda")}>
                  <button type="button" onClick={() => enviar({ tipo: "trcc.desactivar" })} className={boton}>{t("ajustes.trccDesactivar")}</button>
                </Fila>
              )}
              <Selector
                etiqueta={t("ajustes.idioma")}
                valor={ajustes.idioma}
                grupos={[{ titulo: t("ajustes.idioma"), opciones: [
                  { valor: "auto", texto: t("ajustes.idiomaAuto") },
                  { valor: "es", texto: "Español" },
                  { valor: "en", texto: "English" },
                ] }]}
                onCambio={(v) => enviar({ tipo: "ajustes.cambiar", idioma: v as Idioma })}
              />
              <Version />
            </Seccion>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
