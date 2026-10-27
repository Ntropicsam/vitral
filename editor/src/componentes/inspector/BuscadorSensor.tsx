import { IconArrowBarToUp, IconChevronDown, IconMathAvg } from "@tabler/icons-react";
import { Command } from "cmdk";
import { Popover } from "radix-ui";
import { useId, useMemo, useState } from "react";
import { useEstado } from "../../estado/tienda";
import { useT } from "../../i18n";
import { textoSensor } from "../../tema/formato";
import type { SensorInfo, TipoSensor } from "../../tema/tipos";

const ORDEN: TipoSensor[] = ["Temperatura", "Carga", "Reloj", "Ventilador", "Potencia", "Memoria", "Datos", "Voltaje", "Otro"];

export function agrupar(sensores: SensorInfo[], hardware: string | null): { clave: string; hardware: string; tipo: TipoSensor; sensores: SensorInfo[] }[] {
  const grupos = new Map<string, { clave: string; hardware: string; tipo: TipoSensor; sensores: SensorInfo[] }>();
  for (const s of sensores) {
    if (hardware && s.hardware !== hardware) continue;
    const clave = `${s.hardware}|${s.tipo}`;
    const grupo = grupos.get(clave) ?? { clave, hardware: s.hardware, tipo: s.tipo, sensores: [] };
    grupo.sensores.push(s);
    grupos.set(clave, grupo);
  }
  const lista = [...grupos.values()];
  lista.forEach((g) => g.sensores.sort((a, b) => Number(Boolean(a.agregado)) - Number(Boolean(b.agregado))));
  const dispositivos = [...new Set(sensores.map((s) => s.hardware))];
  return lista.sort((a, b) => dispositivos.indexOf(a.hardware) - dispositivos.indexOf(b.hardware) || ORDEN.indexOf(a.tipo) - ORDEN.indexOf(b.tipo));
}

export function BuscadorSensor({ valor, onCambio }: { valor: string; onCambio: (id: string) => void }) {
  const t = useT();
  const id = useId();
  const sensores = useEstado((e) => e.sensores);
  const valores = useEstado((e) => e.valores);
  const [abierto, setAbierto] = useState(false);
  const [hardware, setHardware] = useState<string | null>(null);
  const dispositivos = useMemo(() => [...new Set(sensores.map((s) => s.hardware))], [sensores]);
  const grupos = useMemo(() => agrupar(sensores, hardware), [sensores, hardware]);
  const actual = sensores.find((s) => s.id === valor);

  const nombre = (s: SensorInfo) =>
    s.agregado ? `${t(s.agregado === "promedio" ? "sensor.promedio" : "sensor.maximo")} · ${s.nombre}` : s.nombre;

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-texto-2">{t("inspector.sensor")}</label>
      <Popover.Root open={abierto} onOpenChange={setAbierto}>
        <Popover.Trigger
          id={id}
          className="flex min-h-8 items-center justify-between gap-2 rounded-control border border-borde-fuerte bg-campo px-2.5 py-1 text-left hover:border-texto-3"
        >
          <span className="min-w-0">
            <span className="block truncate">{actual ? nombre(actual) : t(sensores.length ? "sensor.elegir" : "sensor.cargando")}</span>
            {actual && <span className="block truncate text-[12px] text-texto-3">{actual.hardware}</span>}
          </span>
          <IconChevronDown size={14} aria-hidden className="shrink-0 text-texto-2" />
        </Popover.Trigger>
        <Popover.Portal>
          <Popover.Content
            side="left"
            align="start"
            sideOffset={8}
            className="z-50 w-[360px] overflow-hidden rounded-panel border border-borde bg-realce"
          >
            <Command label={t("inspector.sensor")} loop>
              <Command.Input
                placeholder={t("sensor.buscar")}
                autoFocus
                className="h-10 w-full border-b border-borde bg-transparent px-3 text-texto outline-none placeholder:text-texto-3"
              />
              <div className="flex gap-1.5 overflow-x-auto border-b border-borde px-2 py-2">
                {[null, ...dispositivos].map((h) => (
                  <button
                    key={h ?? "todo"}
                    type="button"
                    aria-pressed={hardware === h}
                    onClick={() => setHardware(h)}
                    className="shrink-0 rounded-full border border-borde px-2.5 py-1 text-[12px] text-texto-2 aria-pressed:border-borde-fuerte aria-pressed:bg-campo aria-pressed:text-texto"
                  >
                    {h ?? t("sensor.todo")}
                  </button>
                ))}
              </div>
              <Command.List className="max-h-80 overflow-y-auto p-1">
                <Command.Empty className="px-3 py-4 text-texto-3">{t("sensor.ninguno")}</Command.Empty>
                {grupos.map((g) => (
                  <Command.Group
                    key={g.clave}
                    heading={`${g.hardware} · ${t(`tipo.${g.tipo}`)}`}
                    className="[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:pt-2.5 [&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:text-texto-3"
                  >
                    {g.sensores.map((s) => (
                      <Command.Item
                        key={s.id}
                        value={`${nombre(s)} ${s.hardware} ${t(`tipo.${s.tipo}`)} ${s.id}`}
                        onSelect={() => {
                          onCambio(s.id);
                          setAbierto(false);
                        }}
                        className={`flex cursor-default items-center gap-2 rounded-control px-2 py-1.5 data-[selected=true]:bg-campo ${s.agregado ? "text-texto" : "text-texto-2"} ${s.id === valor ? "outline outline-1 outline-acento" : ""}`}
                      >
                        {s.agregado === "promedio" && <IconMathAvg size={14} aria-hidden />}
                        {s.agregado === "maximo" && <IconArrowBarToUp size={14} aria-hidden />}
                        <span className="truncate">{nombre(s)}</span>
                        <span className="cifras ml-auto shrink-0 text-[13px] text-texto">{textoSensor(valores[s.id], s.tipo)}</span>
                      </Command.Item>
                    ))}
                  </Command.Group>
                ))}
              </Command.List>
            </Command>
          </Popover.Content>
        </Popover.Portal>
      </Popover.Root>
    </div>
  );
}
