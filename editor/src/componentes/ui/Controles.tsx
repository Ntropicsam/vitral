import { Checkbox, Select, Slider } from "radix-ui";
import { IconCheck, IconChevronDown } from "@tabler/icons-react";
import { useId, type ReactNode } from "react";
import { useT } from "../../i18n";

export function Campo({ etiqueta, valor, children }: { etiqueta: string; valor?: ReactNode; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline justify-between text-texto-2">
        <span>{etiqueta}</span>
        {valor != null && <span className="cifras text-[13px] text-texto">{valor}</span>}
      </div>
      {children}
    </div>
  );
}

export function Deslizador(props: {
  etiqueta: string;
  valor: number;
  min: number;
  max: number;
  paso?: number;
  unidad?: string;
  onCambio: (v: number) => void;
}) {
  return (
    <Campo etiqueta={props.etiqueta} valor={`${Math.round(props.valor)}${props.unidad ?? ""}`}>
      <Slider.Root
        className="relative flex h-6 touch-none items-center"
        min={props.min}
        max={props.max}
        step={props.paso ?? 1}
        value={[props.valor]}
        onValueChange={([v]) => v !== undefined && props.onCambio(v)}
        aria-label={props.etiqueta}
      >
        <Slider.Track className="relative h-1 grow rounded-full bg-campo">
          <Slider.Range className="absolute h-full rounded-full bg-acento" />
        </Slider.Track>
        <Slider.Thumb className="block size-4 rounded-full border-2 border-acento bg-texto shadow-none" />
      </Slider.Root>
    </Campo>
  );
}

export interface Opcion {
  valor: string;
  texto: string;
  detalle?: string;
}

export function Selector(props: {
  etiqueta: string;
  valor: string;
  grupos: { titulo: string; opciones: Opcion[] }[];
  vacio?: string;
  onCambio: (v: string) => void;
}) {
  const t = useT();
  const id = useId();
  const actual = props.grupos.flatMap((g) => g.opciones).find((o) => o.valor === props.valor);
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-texto-2">{props.etiqueta}</label>
      <Select.Root value={props.valor || undefined} onValueChange={props.onCambio}>
        <Select.Trigger
          id={id}
          className="flex h-8 items-center justify-between gap-2 rounded-control border border-borde-fuerte bg-campo px-2.5 text-left hover:border-texto-3"
        >
          <span className="truncate">{actual?.texto ?? props.vacio ?? t("control.elige")}</span>
          <IconChevronDown size={14} aria-hidden className="shrink-0 text-texto-2" />
        </Select.Trigger>
        <Select.Portal>
          <Select.Content position="popper" sideOffset={4} className="z-50 max-h-80 min-w-(--radix-select-trigger-width) overflow-hidden rounded-panel border border-borde bg-realce">
            <Select.Viewport className="p-1">
              {props.grupos.map((g) => (
                <Select.Group key={g.titulo}>
                  <Select.Label className="px-2 pb-1 pt-2 text-[12px] text-texto-3">{g.titulo}</Select.Label>
                  {g.opciones.map((o) => (
                    <Select.Item
                      key={o.valor}
                      value={o.valor}
                      className="flex cursor-default items-center justify-between gap-4 rounded-control px-2 py-1.5 outline-none data-highlighted:bg-campo"
                    >
                      <Select.ItemText>{o.texto}</Select.ItemText>
                      {o.detalle && <span className="cifras text-texto-2">{o.detalle}</span>}
                    </Select.Item>
                  ))}
                </Select.Group>
              ))}
            </Select.Viewport>
          </Select.Content>
        </Select.Portal>
      </Select.Root>
    </div>
  );
}

export function Segmentos<T extends string>(props: {
  etiqueta: string;
  valor: T;
  opciones: { valor: T; texto: ReactNode; nombre?: string }[];
  onCambio: (v: T) => void;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-texto-2">{props.etiqueta}</span>
      <div role="radiogroup" aria-label={props.etiqueta} className="flex gap-1 rounded-control bg-campo p-0.5">
        {props.opciones.map((o) => (
          <button
            key={o.valor}
            type="button"
            role="radio"
            aria-checked={o.valor === props.valor}
            aria-label={o.nombre}
            onClick={() => props.onCambio(o.valor)}
            className="flex h-7 flex-1 items-center justify-center rounded-[5px] text-texto-2 hover:text-texto aria-checked:bg-realce aria-checked:text-texto"
          >
            {o.texto}
          </button>
        ))}
      </div>
    </div>
  );
}

export function Casilla({ etiqueta, marcada, onCambio }: { etiqueta: string; marcada: boolean; onCambio: (v: boolean) => void }) {
  const id = useId();
  return (
    <div className="flex items-center gap-2">
      <Checkbox.Root
        id={id}
        checked={marcada}
        onCheckedChange={(v) => onCambio(v === true)}
        className="flex size-4 items-center justify-center rounded-[4px] border border-borde-fuerte bg-campo data-[state=checked]:border-acento data-[state=checked]:bg-acento"
      >
        <Checkbox.Indicator>
          <IconCheck size={12} stroke={3} className="text-sobre-acento" aria-hidden />
        </Checkbox.Indicator>
      </Checkbox.Root>
      <label htmlFor={id} className="text-texto-2">{etiqueta}</label>
    </div>
  );
}

export function CampoTexto(props: { etiqueta: string; valor: string; max?: number; onCambio: (v: string) => void }) {
  const id = useId();
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-texto-2">{props.etiqueta}</label>
      <input
        id={id}
        value={props.valor}
        maxLength={props.max ?? 200}
        onChange={(e) => props.onCambio(e.target.value)}
        className="h-8 rounded-control border border-borde-fuerte bg-campo px-2.5 text-texto outline-none hover:border-texto-3 focus-visible:border-acento"
      />
    </div>
  );
}

export function CampoNumero(props: { etiqueta: string; valor: number; onCambio: (v: number) => void }) {
  const id = useId();
  return (
    <div className="flex flex-1 flex-col gap-1.5">
      <label htmlFor={id} className="text-texto-2">{props.etiqueta}</label>
      <input
        id={id}
        type="number"
        value={props.valor}
        onChange={(e) => Number.isFinite(e.target.valueAsNumber) && props.onCambio(e.target.valueAsNumber)}
        className="cifras h-8 w-full rounded-control border border-borde-fuerte bg-campo px-2.5 text-[14px] text-texto outline-none hover:border-texto-3 focus-visible:border-acento"
      />
    </div>
  );
}
