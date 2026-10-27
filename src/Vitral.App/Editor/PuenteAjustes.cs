using System.Text.Json.Nodes;
using Vitral.Core.Dispositivo;

namespace Vitral.App.Editor;

internal sealed class PuenteAjustes : IDisposable
{
    private readonly Nucleo _nucleo;
    private readonly Actualizador _actualizador;
    private readonly Action<string> _enviar;
    private readonly SynchronizationContext _ui;
    private bool _trccArrancaSolo;
    private bool _revisando;

    public PuenteAjustes(Nucleo nucleo, Actualizador actualizador, Action<string> enviar)
    {
        _nucleo = nucleo;
        _actualizador = actualizador;
        _enviar = enviar;
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _nucleo.AjustesCambiaron += Enviar;
        _nucleo.LectorCpuListo += Enviar;
        _actualizador.Lista += AlActualizar;
    }

    public void Arrancar()
    {
        _enviar(Mensajes.Modelos(Catalogo.Elegibles.Select(c => (c, Catalogo.PorClave(c)!))));
        Task.Run(Arranque.TrccArrancaSolo).ContinueWith(t => _ui.Post(_ =>
        {
            _trccArrancaSolo = t.Result;
            Enviar();
        }, null), TaskScheduler.Default);
        Enviar();
    }

    public bool Atender(string? tipo, JsonObject m)
    {
        switch (tipo)
        {
            case "ajustes.cambiar":
                Cambiar(m);
                return true;
            case "lector.instalar":
                _ = _nucleo.InstalarLectorCpuAsync();
                return true;
            case "trcc.desactivar":
                Task.Run(Arranque.DesactivarTrcc).ContinueWith(t => _ui.Post(_ =>
                {
                    _trccArrancaSolo = !t.Result;
                    Enviar();
                }, null), TaskScheduler.Default);
                return true;
            case "actualizacion.buscar":
                _revisando = true;
                Enviar();
                _actualizador.BuscarAhoraAsync().ContinueWith(_ => _ui.Post(_ =>
                {
                    _revisando = false;
                    Enviar();
                }, null), TaskScheduler.Default);
                return true;
            case "actualizacion.aplicar":
                _actualizador.ReiniciarYActualizar();
                return true;
            default:
                return false;
        }
    }

    public void Dispose()
    {
        _nucleo.AjustesCambiaron -= Enviar;
        _nucleo.LectorCpuListo -= Enviar;
        _actualizador.Lista -= AlActualizar;
    }

    private void Cambiar(JsonObject m)
    {
        if (Mensajes.Logico(m, "arranque") is { } arranque)
        {
            if (arranque)
            {
                Arranque.Activar();
            }
            else
            {
                Arranque.Desactivar();
            }
        }
        var brillo = Mensajes.Entero(m, "brillo");
        var rotacion = Mensajes.Entero(m, "rotacion");
        var modoJuego = Mensajes.Logico(m, "modoJuego");
        var idioma = Mensajes.Texto(m, "idioma");
        var cambiaModelo = m.ContainsKey("modeloForzado");
        var modelo = Mensajes.Texto(m, "modeloForzado");
        _nucleo.CambiarAjustes(a => a with
        {
            Brillo = brillo ?? a.Brillo,
            Rotacion = rotacion ?? a.Rotacion,
            ModoJuego = modoJuego ?? a.ModoJuego,
            Idioma = idioma ?? a.Idioma,
            ModeloForzado = cambiaModelo ? modelo : a.ModeloForzado,
        });
    }

    private void AlActualizar(string version) => _ui.Post(_ => Enviar(), null);

    private void Enviar()
    {
        var a = _nucleo.Ajustes;
        _enviar(Mensajes.Ajustes(new
        {
            brillo = a.Brillo,
            rotacion = a.Rotacion,
            modoJuego = a.ModoJuego,
            idioma = a.Idioma,
            idiomaEfectivo = Textos.Idioma,
            modeloForzado = a.ModeloForzado,
            arranque = Arranque.Activo(),
            trccArrancaSolo = _trccArrancaSolo,
            version = Nucleo.Version,
            lector = new
            {
                incluido = ServicioSensores.Incluido,
                activo = _nucleo.ConServicio,
                version = _nucleo.VersionServicio,
                desactualizado = _nucleo.ConServicio && _nucleo.VersionServicio is { } v && v != Nucleo.Version,
            },
            actualizacion = new
            {
                instalado = _actualizador.Instalado,
                nueva = _actualizador.VersionLista,
                revisando = _revisando,
            },
        }));
    }
}
