import { IconCheck, IconSettings } from "@tabler/icons-react";
import { cambiar, editarTema, useEstado } from "../estado/tienda";
import { useT, type Clave } from "../i18n";
import { enviar } from "../puente/puente";
import type { EstadoPantalla } from "../puente/tipos";

type Traductor = (clave: Clave, datos?: Record<string, string | number>) => string;

function textoPantalla(p: EstadoPantalla | null, t: Traductor): { texto: string; bien: boolean } {
  if (!p) return { texto: t("estado.buscando"), bien: false };
  switch (p.conexion) {
    case "Conectada": {
      const base = t("estado.conectada", { modelo: p.modelo ?? "", ancho: p.ancho ?? 0, alto: p.alto ?? 0, fps: p.fps });
      return { texto: p.forzado ? `${base} · ${t("estado.forzada")}` : base, bien: true };
    }
    case "Ocupada":
      return { texto: t("estado.ocupada"), bien: false };
    case "NoConectada":
      return { texto: t("estado.noConectada"), bien: false };
    case "NoSoportada":
      return { texto: t("estado.noSoportada", { modelo: p.modelo ?? "" }), bien: false };
    case "Buscando":
      return { texto: t("estado.buscando"), bien: false };
    default:
      return { texto: t("estado.error"), bien: false };
  }
}

export function Barra() {
  const t = useT();
  const nombre = useEstado((e) => e.tema?.nombre ?? "");
  const guardado = useEstado((e) => e.guardado);
  const pantalla = useEstado((e) => e.pantalla);
  const enPantalla = useEstado((e) => e.edicionId !== null && e.edicionId === e.activo);
  const error = useEstado((e) => e.error);
  const hayNueva = useEstado((e) => e.ajustes.actualizacion.nueva !== null || e.ajustes.lector.desactualizado);
  const estado = textoPantalla(pantalla, t);

  return (
    <header className="col-span-3 flex h-12 items-center gap-3 border-b border-borde px-3">
      <label className="sr-only" htmlFor="nombre-tema">{t("barra.nombre")}</label>
      <input
        id="nombre-tema"
        value={nombre}
        maxLength={60}
        onChange={(e) => editarTema((tm) => ({ ...tm, nombre: e.target.value }))}
        className="h-8 w-56 rounded-control border border-transparent bg-transparent px-2 text-[14px] font-semibold text-texto outline-none hover:border-borde focus-visible:border-acento"
      />
      <span className="text-texto-3" aria-live="polite">{guardado === "guardado" ? t("barra.guardado") : t("barra.guardando")}</span>

      {error && (
        <button type="button" onClick={() => cambiar({ error: null })} className="ml-4 truncate rounded-control bg-[#3a1f1c] px-2.5 py-1 text-peligro" title={t("barra.cerrarAviso")}>
          {error}
        </button>
      )}

      <div className="ml-auto flex items-center gap-2 text-texto-2">
        <span aria-hidden className={`size-2 rounded-full ${estado.bien ? "bg-exito" : "bg-aviso"}`} />
        <span className="cifras text-[14px]">{estado.texto}</span>
      </div>
      {enPantalla ? (
        <span className="flex h-8 items-center gap-1.5 px-3 text-texto-2">
          <IconCheck size={16} aria-hidden className="text-exito" />
          {t("barra.enPantalla")}
        </span>
      ) : (
        <button
          type="button"
          onClick={() => enviar({ tipo: "tema.aplicar" })}
          className="h-8 rounded-control bg-acento px-3.5 font-semibold text-sobre-acento hover:brightness-110"
        >
          {t("barra.aplicar")}
        </button>
      )}
      <button
        type="button"
        aria-label={t("barra.ajustes")}
        title={t("barra.ajustes")}
        onClick={() => cambiar({ dialogo: "ajustes" })}
        className="relative flex size-8 items-center justify-center rounded-control text-texto-2 hover:bg-realce hover:text-texto"
      >
        <IconSettings size={18} aria-hidden />
        {hayNueva && <span aria-hidden className="absolute right-1.5 top-1.5 size-2 rounded-full bg-acento" />}
      </button>
    </header>
  );
}
