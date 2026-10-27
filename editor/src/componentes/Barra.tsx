import { IconCheck } from "@tabler/icons-react";
import { cambiar, editarTema, useEstado } from "../estado/tienda";
import { enviar } from "../puente/puente";
import type { EstadoPantalla } from "../puente/tipos";

function textoPantalla(p: EstadoPantalla | null): { texto: string; bien: boolean } {
  if (!p) return { texto: "Buscando la pantalla…", bien: false };
  switch (p.conexion) {
    case "Conectada":
      return { texto: `${p.modelo ?? "Pantalla"} · ${p.ancho} × ${p.alto} · ${p.fps} fps`, bien: true };
    case "Ocupada":
      return { texto: "Otro programa tiene la pantalla (¿TRCC?)", bien: false };
    case "NoConectada":
      return { texto: "No encuentro la pantalla", bien: false };
    case "NoSoportada":
      return { texto: `${p.modelo ?? "Esta pantalla"}: todavía no es compatible`, bien: false };
    case "Buscando":
      return { texto: "Buscando la pantalla…", bien: false };
    default:
      return { texto: "La pantalla no responde", bien: false };
  }
}

export function Barra() {
  const nombre = useEstado((e) => e.tema?.nombre ?? "");
  const guardado = useEstado((e) => e.guardado);
  const pantalla = useEstado((e) => e.pantalla);
  const enPantalla = useEstado((e) => e.edicionId !== null && e.edicionId === e.activo);
  const error = useEstado((e) => e.error);
  const estado = textoPantalla(pantalla);

  return (
    <header className="col-span-3 flex h-12 items-center gap-3 border-b border-borde px-3">
      <label className="sr-only" htmlFor="nombre-tema">Nombre del tema</label>
      <input
        id="nombre-tema"
        value={nombre}
        maxLength={60}
        onChange={(e) => editarTema((t) => ({ ...t, nombre: e.target.value }))}
        className="h-8 w-56 rounded-control border border-transparent bg-transparent px-2 text-[14px] font-semibold text-texto outline-none hover:border-borde focus-visible:border-acento"
      />
      <span className="text-texto-3" aria-live="polite">{guardado === "guardado" ? "Guardado" : "Guardando…"}</span>

      {error && (
        <button type="button" onClick={() => cambiar({ error: null })} className="ml-4 truncate rounded-control bg-[#3a1f1c] px-2.5 py-1 text-peligro" title="Cerrar el aviso">
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
          Es el tema de la pantalla
        </span>
      ) : (
        <button
          type="button"
          onClick={() => enviar({ tipo: "tema.aplicar" })}
          className="h-8 rounded-control bg-acento px-3.5 font-semibold text-sobre-acento hover:brightness-110"
        >
          Aplicar a la pantalla
        </button>
      )}
    </header>
  );
}
