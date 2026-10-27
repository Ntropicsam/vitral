export interface FuenteVitral {
  id: string;
  nombre: string;
  familia: string;
}

export const FUENTES: FuenteVitral[] = [
  { id: "barlow", nombre: "Barlow Semi Condensed", familia: "V-barlow" },
  { id: "saira", nombre: "Saira Condensed", familia: "V-saira" },
  { id: "rajdhani", nombre: "Rajdhani", familia: "V-rajdhani" },
  { id: "chakra", nombre: "Chakra Petch", familia: "V-chakra" },
  { id: "bebas", nombre: "Bebas Neue", familia: "V-bebas" },
  { id: "anton", nombre: "Anton", familia: "V-anton" },
  { id: "plexmono", nombre: "IBM Plex Mono", familia: "V-plexmono" },
  { id: "russo", nombre: "Russo One", familia: "V-russo" },
];

export function familiaDe(id: string | undefined): string {
  return (FUENTES.find((f) => f.id === id) ?? FUENTES[0]!).familia;
}
