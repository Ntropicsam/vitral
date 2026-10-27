import type { DeVitral, HaciaVitral } from "./tipos";

interface CanalWebView {
  postMessage(mensaje: unknown): void;
  addEventListener(tipo: "message", oyente: (e: { data: unknown }) => void): void;
}

declare global {
  interface Window {
    chrome?: { webview?: CanalWebView };
  }
}

type Oyente = (m: DeVitral) => void;

export interface Canal {
  enviar(m: HaciaVitral): void;
  escuchar(oyente: Oyente): () => void;
}

function canalWebView(webview: CanalWebView): Canal {
  const oyentes = new Set<Oyente>();
  webview.addEventListener("message", (e) => {
    const m = e.data as DeVitral;
    if (m && typeof m === "object" && "tipo" in m) oyentes.forEach((o) => o(m));
  });
  return {
    enviar: (m) => webview.postMessage(m),
    escuchar(oyente) {
      oyentes.add(oyente);
      return () => oyentes.delete(oyente);
    },
  };
}

let canal: Canal | null = null;

export async function abrirCanal(): Promise<Canal> {
  if (canal) return canal;
  const webview = window.chrome?.webview;
  canal = webview ? canalWebView(webview) : (await import("./simulado")).canalSimulado();
  return canal;
}

export function enviar(m: HaciaVitral): void {
  canal?.enviar(m);
}
