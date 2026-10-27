import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { cambiar, editarTema, obtener, useEstado } from "../estado/tienda";
import { pegar, type Guia } from "../lienzo/guias";
import { actualizarCapa, conTamano, moverCapa, tamanoDe } from "../tema/operaciones";
import type { Caja } from "../tema/tipos";
import { useT } from "../i18n";
import { LineaTiempo } from "./LineaTiempo";
import { accionesDeCapa, ID_FONDO } from "./PanelCapas";

interface Arrastre {
  id: string;
  modo: "mover" | "tamano";
  inicioX: number;
  inicioY: number;
  caja: Caja;
  tamano: number;
  aplicado: { dx: number; dy: number };
}

function useEscala(ancho: number, alto: number) {
  const ref = useRef<HTMLDivElement>(null);
  const [escala, setEscala] = useState(1);
  useLayoutEffect(() => {
    const el = ref.current;
    if (!el) return;
    const medir = () => setEscala(Math.max(0.1, Math.min((el.clientWidth - 48) / ancho, (el.clientHeight - 48) / alto, 2)));
    medir();
    const obs = new ResizeObserver(medir);
    obs.observe(el);
    return () => obs.disconnect();
  }, [ancho, alto]);
  return { ref, escala };
}

export function Lienzo() {
  const t = useT();
  const ancho = useEstado((e) => e.tema?.ancho ?? 640);
  const alto = useEstado((e) => e.tema?.alto ?? 480);
  const vista = useEstado((e) => e.vista);
  const seleccion = useEstado((e) => e.seleccion);
  const { ref, escala } = useEscala(ancho, alto);
  const [arrastre, setArrastre] = useState<Arrastre | null>(null);
  const [guias, setGuias] = useState<Guia[]>([]);

  const cajas = (vista?.cajas ?? []).map((c) =>
    arrastre?.modo === "mover" && c.id === arrastre.id ? { ...c, x: arrastre.caja.x + arrastre.aplicado.dx, y: arrastre.caja.y + arrastre.aplicado.dy } : c,
  );

  useEffect(() => {
    const alTeclado = (e: KeyboardEvent) => {
      const { seleccion: id } = obtener();
      if (!id || id === ID_FONDO || (e.target as HTMLElement).closest("input, textarea, [role=slider], [role=combobox]")) return;
      const paso = e.shiftKey ? 10 : 1;
      const flechas: Record<string, [number, number]> = { ArrowLeft: [-paso, 0], ArrowRight: [paso, 0], ArrowUp: [0, -paso], ArrowDown: [0, paso] };
      const mov = flechas[e.key];
      if (mov) {
        e.preventDefault();
        editarTema((t) => moverCapa(t, id, mov[0], mov[1]));
      } else if (e.key === "Delete") {
        accionesDeCapa(id).borrar();
      } else if (e.key === "d" && e.ctrlKey) {
        e.preventDefault();
        accionesDeCapa(id).duplicar();
      } else if (e.key === "Escape") {
        cambiar({ seleccion: null });
      }
    };
    window.addEventListener("keydown", alTeclado);
    return () => window.removeEventListener("keydown", alTeclado);
  }, []);

  function empezar(e: React.PointerEvent, caja: Caja, modo: Arrastre["modo"]) {
    e.stopPropagation();
    e.preventDefault();
    const capa = obtener().tema?.capas.find((c) => c.id === caja.id);
    if (!capa && caja.id !== ID_FONDO) return;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    cambiar({ seleccion: caja.id });
    setArrastre({ id: caja.id, modo, inicioX: e.clientX, inicioY: e.clientY, caja, tamano: capa ? tamanoDe(capa) : 0, aplicado: { dx: 0, dy: 0 } });
  }

  function moverMarco(dx: number, dy: number, modo: Arrastre["modo"], caja: Caja) {
    editarTema((t) => {
      const x = modo === "mover" ? Math.round(Math.min(Math.max(0, caja.x + dx), t.ancho - caja.ancho)) : caja.x;
      const y = modo === "mover" ? Math.round(Math.min(Math.max(0, caja.y + dy), t.alto - caja.alto)) : caja.y;
      const w = modo === "tamano" ? Math.round(Math.min(Math.max(32, caja.ancho + dx), t.ancho - caja.x)) : caja.ancho;
      const h = modo === "tamano" ? Math.round(Math.min(Math.max(32, caja.alto + dy), t.alto - caja.y)) : caja.alto;
      return { ...t, fondo: { ...t.fondo, marco: { x, y, ancho: w, alto: h } } };
    });
  }

  function mover(e: React.PointerEvent) {
    if (!arrastre) return;
    const dx = (e.clientX - arrastre.inicioX) / escala;
    const dy = (e.clientY - arrastre.inicioY) / escala;
    if (arrastre.id === ID_FONDO) {
      moverMarco(dx, dy, arrastre.modo, arrastre.caja);
      setArrastre({ ...arrastre, aplicado: arrastre.modo === "mover" ? { dx, dy } : { dx: 0, dy: 0 } });
      return;
    }
    if (arrastre.modo === "tamano") {
      const capa = obtener().tema?.capas.find((c) => c.id === arrastre.id);
      if (!capa) return;
      const factor = 1 + Math.max(dx / Math.max(arrastre.caja.ancho, 1), dy / Math.max(arrastre.caja.alto, 1));
      editarTema((t) => actualizarCapa(t, arrastre.id, conTamano(capa, arrastre.tamano * factor)));
      return;
    }
    const otras = (vista?.cajas ?? []).filter((c) => c.id !== arrastre.id);
    const ajuste = e.altKey ? { dx, dy, guias: [] } : pegar(arrastre.caja, dx, dy, otras, { ancho, alto });
    const delta = { dx: Math.round(ajuste.dx), dy: Math.round(ajuste.dy) };
    editarTema((t) => moverCapa(t, arrastre.id, delta.dx - arrastre.aplicado.dx, delta.dy - arrastre.aplicado.dy));
    setArrastre({ ...arrastre, aplicado: delta });
    setGuias(ajuste.guias);
  }

  function terminar() {
    setArrastre(null);
    setGuias([]);
  }

  return (
    <main className="flex min-h-0 min-w-0 flex-col bg-fondo">
      <div ref={ref} className="relative flex min-h-0 flex-1 items-center justify-center" onPointerDown={() => cambiar({ seleccion: null })}>
        <div className="relative rounded-[6px] bg-black" style={{ width: ancho * escala, height: alto * escala }}>
          {vista ? (
            <img src={vista.url} alt={t("lienzo.vista")} draggable={false} className="pointer-events-none size-full rounded-[6px]" />
          ) : (
            <div className="flex size-full items-center justify-center text-texto-3">{t("lienzo.preparando")}</div>
          )}
          {cajas.map((c) => {
            const elegida = c.id === seleccion;
            return (
              <div
                key={c.id}
                role="button"
                aria-label={t("lienzo.mover")}
                tabIndex={-1}
                onPointerDown={(e) => empezar(e, c, "mover")}
                onPointerMove={mover}
                onPointerUp={terminar}
                onPointerCancel={terminar}
                className={`absolute cursor-move rounded-[3px] ${elegida ? "outline-[1.5px] outline-acento" : "hover:outline hover:outline-acento/50"} ${c.id === ID_FONDO && !elegida ? "outline-dashed outline-1 outline-texto-3/40" : ""}`}
                style={{ left: c.x * escala - 4, top: c.y * escala - 4, width: c.ancho * escala + 8, height: c.alto * escala + 8 }}
              >
                {elegida && (
                  <div
                    aria-label={t("lienzo.tamano")}
                    onPointerDown={(e) => empezar(e, c, "tamano")}
                    onPointerMove={mover}
                    onPointerUp={terminar}
                    className="absolute -bottom-1.5 -right-1.5 size-3 cursor-nwse-resize rounded-[2px] bg-acento"
                  />
                )}
              </div>
            );
          })}
          {guias.map((g) => (
            <div
              key={`${g.eje}${g.posicion}`}
              aria-hidden
              className={`pointer-events-none absolute border-dashed border-acento/80 ${g.eje === "x" ? "top-0 h-full border-l" : "left-0 w-full border-t"}`}
              style={g.eje === "x" ? { left: g.posicion * escala } : { top: g.posicion * escala }}
            />
          ))}
        </div>
      </div>
      <LineaTiempo />
      <p className="px-4 pb-3 text-[12px] text-texto-3">
        {t("lienzo.ayuda")}
      </p>
    </main>
  );
}
