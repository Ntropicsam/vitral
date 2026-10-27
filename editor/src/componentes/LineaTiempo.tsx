import { IconPlayerPause, IconPlayerPlay } from "@tabler/icons-react";
import { Slider } from "radix-ui";
import { useEstado } from "../estado/tienda";
import { enviar } from "../puente/puente";

export function LineaTiempo() {
  const cuadros = useEstado((e) => e.pantalla?.cuadros ?? 1);
  const pausado = useEstado((e) => e.pantalla?.pausado ?? false);
  const cuadro = useEstado((e) => e.vista?.cuadro ?? 0);
  if (cuadros <= 1) return null;

  return (
    <div className="flex items-center gap-3 px-4 pb-2">
      <button
        type="button"
        aria-label={pausado ? "Reproducir" : "Pausar"}
        onClick={() => enviar({ tipo: "animacion.pausar", pausado: !pausado })}
        className="flex size-8 items-center justify-center rounded-control border border-borde text-texto hover:border-borde-fuerte"
      >
        {pausado ? <IconPlayerPlay size={16} aria-hidden /> : <IconPlayerPause size={16} aria-hidden />}
      </button>
      <Slider.Root
        className="relative flex h-6 flex-1 touch-none items-center"
        min={0}
        max={cuadros - 1}
        step={1}
        value={[cuadro]}
        onValueChange={([v]) => {
          if (v === undefined) return;
          if (!pausado) enviar({ tipo: "animacion.pausar", pausado: true });
          enviar({ tipo: "animacion.ir", cuadro: v });
        }}
        aria-label="Cuadro del fondo"
      >
        <Slider.Track className="relative h-1 grow rounded-full bg-campo">
          <Slider.Range className="absolute h-full rounded-full bg-borde-fuerte" />
        </Slider.Track>
        <Slider.Thumb className="block size-3.5 rounded-full bg-texto" />
      </Slider.Root>
      <span className="cifras w-20 text-right text-[14px] text-texto-2">
        {cuadro + 1} / {cuadros}
      </span>
      <span className="w-40 text-[12px] text-texto-3">{pausado ? "En pausa: la pantalla va a 1 fps" : ""}</span>
    </div>
  );
}
