import { useId } from "react";
import { COLORES } from "../../tema/operaciones";

export function SelectorColor({ etiqueta, valor, onCambio }: { etiqueta: string; valor: string; onCambio: (v: string) => void }) {
  const id = useId();
  const propio = !COLORES.some((c) => c.valor === valor.toLowerCase());
  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-texto-2">{etiqueta}</span>
      <div role="radiogroup" aria-label={etiqueta} className="flex items-center gap-2.5">
        {COLORES.map((c) => (
          <button
            key={c.valor}
            type="button"
            role="radio"
            aria-checked={c.valor === valor.toLowerCase()}
            aria-label={c.nombre}
            title={c.nombre}
            onClick={() => onCambio(c.valor)}
            className="size-6 rounded-full border border-borde-fuerte aria-checked:outline-2 aria-checked:outline-offset-2 aria-checked:outline-acento"
            style={{ background: c.valor }}
          />
        ))}
        <label
          htmlFor={id}
          title="Otro color"
          className={`relative size-6 cursor-pointer overflow-hidden rounded-full border border-dashed border-borde-fuerte ${propio ? "outline-2 outline-offset-2 outline-acento" : ""}`}
          style={propio ? { background: valor } : undefined}
        >
          <span className="sr-only">Otro color</span>
          <input id={id} type="color" value={valor} onChange={(e) => onCambio(e.target.value)} className="absolute inset-0 cursor-pointer opacity-0" />
        </label>
      </div>
    </div>
  );
}
