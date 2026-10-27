import { IconPlus } from "@tabler/icons-react";
import { ContextMenu } from "radix-ui";
import { cambiar, useEstado } from "../estado/tienda";
import { useT } from "../i18n";
import { enviar } from "../puente/puente";

const item = "flex cursor-default items-center rounded-control px-2.5 py-1.5 outline-none data-highlighted:bg-campo";

export function TusTemas() {
  const tr = useT();
  const temas = useEstado((e) => e.temas);
  const edicion = useEstado((e) => e.edicionId);
  const activo = useEstado((e) => e.activo);

  return (
    <section className="mt-auto border-t border-borde px-3 pb-3 pt-3" aria-label={tr("temas.titulo")}>
      <h2 className="pb-2 text-[13px] font-semibold text-texto-2">{tr("temas.titulo")}</h2>
      <ul className="flex flex-wrap gap-2">
        {temas.map((t) => (
          <li key={t.id}>
            <ContextMenu.Root>
              <ContextMenu.Trigger asChild>
                <button
                  type="button"
                  onClick={() => t.id !== edicion && enviar({ tipo: "tema.abrir", id: t.id })}
                  aria-current={t.id === edicion}
                  aria-label={`${t.nombre}${t.id === activo ? ` (${tr("temas.enPantalla")})` : ""}`}
                  title={`${t.nombre}${t.id === activo ? ` · ${tr("temas.enPantalla")}` : ""}. ${tr("temas.masOpciones")}`}
                  className="relative block h-[45px] w-[60px] overflow-hidden rounded-[4px] bg-fondo outline-offset-2 aria-[current=true]:outline-2 aria-[current=true]:outline-acento"
                >
                  {t.miniatura ? (
                    <img src={t.miniatura} alt="" className="size-full object-cover" draggable={false} />
                  ) : (
                    <span className="flex size-full items-center justify-center p-1 text-center text-[11px] leading-tight text-texto-3">{t.nombre}</span>
                  )}
                  {t.id === activo && <span aria-hidden className="absolute bottom-1 right-1 size-2 rounded-full bg-exito ring-2 ring-fondo" />}
                </button>
              </ContextMenu.Trigger>
              <ContextMenu.Portal>
                <ContextMenu.Content className="z-50 min-w-40 rounded-panel border border-borde bg-realce p-1">
                  <ContextMenu.Item className={item} onSelect={() => enviar({ tipo: "tema.abrir", id: t.id })}>{tr("temas.abrir")}</ContextMenu.Item>
                  <ContextMenu.Item className={item} onSelect={() => enviar({ tipo: "tema.duplicar", id: t.id })}>{tr("temas.duplicar")}</ContextMenu.Item>
                  <ContextMenu.Separator className="my-1 h-px bg-borde" />
                  <ContextMenu.Item
                    className={`${item} text-peligro`}
                    onSelect={() => {
                      if (window.confirm(tr("temas.confirmarPapelera", { nombre: t.nombre }))) enviar({ tipo: "tema.borrar", id: t.id });
                    }}
                  >
                    {tr("temas.papelera")}
                  </ContextMenu.Item>
                </ContextMenu.Content>
              </ContextMenu.Portal>
            </ContextMenu.Root>
          </li>
        ))}
        <li>
          <button
            type="button"
            onClick={() => cambiar({ dialogo: "nuevo" })}
            aria-label={tr("temas.nuevo")}
            title={tr("temas.nuevo")}
            className="flex h-[45px] w-[60px] items-center justify-center rounded-[4px] border border-dashed border-borde-fuerte text-texto-3 hover:text-texto"
          >
            <IconPlus size={18} aria-hidden />
          </button>
        </li>
      </ul>
    </section>
  );
}
