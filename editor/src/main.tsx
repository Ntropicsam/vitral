import { StrictMode, useEffect } from "react";
import { createRoot } from "react-dom/client";
import { Ajustes } from "./componentes/Ajustes";
import { Barra } from "./componentes/Barra";
import { Inspector } from "./componentes/inspector/Inspector";
import { Lienzo } from "./componentes/Lienzo";
import { NuevoTema } from "./componentes/NuevoTema";
import { PanelCapas } from "./componentes/PanelCapas";
import { recibir } from "./estado/tienda";
import { abrirCanal } from "./puente/puente";
import "./estilos.css";

function App() {
  useEffect(() => {
    let soltar: (() => void) | undefined;
    void abrirCanal().then((canal) => {
      soltar = canal.escuchar(recibir);
      canal.enviar({ tipo: "listo" });
    });
    return () => soltar?.();
  }, []);

  return (
    <div className="grid h-full grid-cols-[236px_minmax(0,1fr)_292px] grid-rows-[48px_minmax(0,1fr)]">
      <Barra />
      <PanelCapas />
      <Lienzo />
      <Inspector />
      <Ajustes />
      <NuevoTema />
    </div>
  );
}

createRoot(document.getElementById("raiz")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
