import { IconUpload, IconX } from "@tabler/icons-react";
import { Dialog } from "radix-ui";
import { useEffect, useRef, useState } from "react";
import { cambiar, obtener, useEstado } from "../estado/tienda";
import { useT } from "../i18n";
import { esVideo, importarArchivo } from "../importar/importar";
import { enviar } from "../puente/puente";
import { formaDe, marcoDe, sugerir, tamanoDelFondo } from "../tema/distribuciones";
import type { Distribucion } from "../tema/tipos";

interface Elegido {
  archivo: File;
  url: string | null;
  video: boolean;
  aspecto: number | null;
}

function aspectoDe(archivo: File, url: string): Promise<number | null> {
  return new Promise((resolver) => {
    if (esVideo(archivo)) {
      const v = document.createElement("video");
      v.preload = "metadata";
      v.onloadedmetadata = () => resolver(v.videoHeight ? v.videoWidth / v.videoHeight : null);
      v.onerror = () => resolver(null);
      v.src = url;
      return;
    }
    const img = new Image();
    img.onload = () => resolver(img.naturalHeight ? img.naturalWidth / img.naturalHeight : null);
    img.onerror = () => resolver(null);
    img.src = url;
  });
}

function Muestra({ elegido, distribucion, ancho, alto }: { elegido: Elegido; distribucion: Distribucion; ancho: number; alto: number }) {
  const w = 168;
  const h = Math.round((w * alto) / ancho);
  const m = marcoDe(distribucion, ancho, alto) ?? { x: 0, y: 0, ancho, alto };
  const k = w / ancho;
  const caja = { left: m.x * k, top: m.y * k, width: m.ancho * k, height: m.alto * k };
  const medio = elegido.url ? (
    elegido.video ? (
      <video src={elegido.url} muted className="size-full object-cover" />
    ) : (
      <img src={elegido.url} alt="" className="size-full object-cover" draggable={false} />
    )
  ) : (
    <span className="flex size-full items-center justify-center bg-realce text-[11px] text-texto-3">Theme.zt</span>
  );
  return (
    <span aria-hidden className="relative block overflow-hidden rounded-[4px] bg-black" style={{ width: w, height: h }}>
      <span className="absolute overflow-hidden rounded-[2px]" style={caja}>{medio}</span>
    </span>
  );
}

export function NuevoTema() {
  const t = useT();
  const abierto = useEstado((e) => e.dialogo === "nuevo");
  const ancho = useEstado((e) => e.pantalla?.ancho ?? e.tema?.ancho ?? 640);
  const alto = useEstado((e) => e.pantalla?.alto ?? e.tema?.alto ?? 480);
  const edicionId = useEstado((e) => e.edicionId);
  const [elegido, setElegido] = useState<Elegido | null>(null);
  const [distribucion, setDistribucion] = useState<Distribucion>("Completa");
  const [encima, setEncima] = useState(false);
  const entrada = useRef<HTMLInputElement>(null);
  const pendiente = useRef<{ archivo: File; desde: string | null } | null>(null);

  useEffect(() => {
    const p = pendiente.current;
    const tema = obtener().tema;
    if (!p || edicionId === p.desde || !tema) return;
    pendiente.current = null;
    void importarArchivo(p.archivo, tamanoDelFondo(tema.fondo.marco, tema.ancho, tema.alto));
  }, [edicionId]);

  useEffect(() => () => {
    if (elegido?.url) URL.revokeObjectURL(elegido.url);
  }, [elegido]);

  async function elegir(archivo: File | undefined) {
    if (!archivo) return;
    const zt = /\.zt$/i.test(archivo.name);
    const url = zt ? null : URL.createObjectURL(archivo);
    const aspecto = url ? await aspectoDe(archivo, url) : null;
    const nuevo = { archivo, url, video: esVideo(archivo), aspecto };
    setElegido(nuevo);
    setDistribucion(sugerir(aspecto ?? ancho / alto, ancho / alto)[0]!);
  }

  function cerrar() {
    setElegido(null);
    cambiar({ dialogo: null });
  }

  function crear(d: Distribucion, archivo: File | null) {
    pendiente.current = archivo ? { archivo, desde: obtener().edicionId } : null;
    enviar({ tipo: "tema.nuevo", distribucion: d });
    cerrar();
  }

  const opciones = elegido ? sugerir(elegido.aspecto ?? ancho / alto, ancho / alto) : [];
  const forma = elegido ? formaDe(elegido.aspecto ?? ancho / alto, ancho / alto) : "parecido";

  return (
    <Dialog.Root open={abierto} onOpenChange={(v) => (v ? cambiar({ dialogo: "nuevo" }) : cerrar())}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-40 bg-black/60" />
        <Dialog.Content
          aria-describedby={undefined}
          className="fixed left-1/2 top-1/2 z-50 flex w-[min(640px,92vw)] -translate-x-1/2 -translate-y-1/2 flex-col rounded-panel border border-borde bg-panel"
        >
          <div className="flex h-12 shrink-0 items-center justify-between border-b border-borde px-5">
            <Dialog.Title className="text-[14px] font-semibold">{t("nuevo.titulo")}</Dialog.Title>
            <Dialog.Close aria-label={t("ajustes.cerrar")} className="flex size-8 items-center justify-center rounded-control text-texto-2 hover:bg-realce hover:text-texto">
              <IconX size={18} aria-hidden />
            </Dialog.Close>
          </div>
          <input
            ref={entrada}
            type="file"
            accept="image/*,video/*,.gif,.webp,.zt"
            className="sr-only"
            tabIndex={-1}
            aria-hidden
            onChange={(e) => {
              void elegir(e.target.files?.[0]);
              e.target.value = "";
            }}
          />
          {!elegido ? (
            <div className="flex flex-col gap-4 p-5">
              <h3 className="text-[13px] font-semibold text-texto-2">{t("nuevo.fondo")}</h3>
              <button
                type="button"
                onClick={() => entrada.current?.click()}
                onDragOver={(e) => {
                  e.preventDefault();
                  setEncima(true);
                }}
                onDragLeave={() => setEncima(false)}
                onDrop={(e) => {
                  e.preventDefault();
                  setEncima(false);
                  void elegir(e.dataTransfer.files[0]);
                }}
                className={`flex h-48 flex-col items-center justify-center gap-3 rounded-panel border border-dashed px-8 text-center text-texto-2 hover:border-texto-3 hover:text-texto ${encima ? "border-acento bg-realce text-texto" : "border-borde-fuerte"}`}
              >
                <IconUpload size={24} aria-hidden />
                <span className="max-w-80 leading-snug">{t("nuevo.soltar")}</span>
              </button>
              <div className="flex justify-end">
                <button type="button" onClick={() => crear("SoloDatos", null)} className="h-8 rounded-control px-3 text-texto-2 hover:bg-realce hover:text-texto">
                  {t("nuevo.sinFondo")}
                </button>
              </div>
            </div>
          ) : (
            <div className="flex flex-col gap-4 p-5">
              <p className="leading-snug text-texto-2">{t(`nuevo.${forma}`)}</p>
              <div role="radiogroup" aria-label={t("fondo.distribucion")} className="flex flex-wrap gap-3">
                {opciones.map((d, i) => (
                  <button
                    key={d}
                    type="button"
                    role="radio"
                    aria-checked={d === distribucion}
                    onClick={() => setDistribucion(d)}
                    className="flex flex-col gap-2 rounded-panel border border-borde p-2.5 text-left hover:border-texto-3 aria-checked:border-acento aria-checked:bg-realce"
                  >
                    <Muestra elegido={elegido} distribucion={d} ancho={ancho} alto={alto} />
                    <span className="flex items-baseline gap-1.5 text-[13px]">
                      {t(`dist.${d}`)}
                      {i === 0 && <span className="text-[12px] text-acento">{t("nuevo.recomendada")}</span>}
                    </span>
                  </button>
                ))}
              </div>
              <div className="flex items-center justify-between gap-3">
                <button type="button" onClick={() => entrada.current?.click()} className="h-8 rounded-control px-3 text-texto-2 hover:bg-realce hover:text-texto">
                  {t("nuevo.otro")}
                </button>
                <button
                  type="button"
                  onClick={() => crear(distribucion, elegido.archivo)}
                  className="h-8 rounded-control bg-acento px-3.5 font-semibold text-sobre-acento hover:brightness-110"
                >
                  {t("nuevo.crear")}
                </button>
              </div>
            </div>
          )}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
