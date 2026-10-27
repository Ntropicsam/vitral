# Contributing to Vitral

Thanks for wanting to help. Issues and pull requests are welcome in English or
Spanish. *¿Prefieres español? Escribe en español, sin problema.*

## Ways to help

- **Report your screen.** Even a "it works on my Peerless Vision 360" is useful.
  Use the [screen report](https://github.com/Ntropicsam/vitral/issues/new?template=pantalla.yml)
  template.
- **Report a bug** with the [bug template](https://github.com/Ntropicsam/vitral/issues/new?template=error.yml).
  The log at `%LOCALAPPDATA%\VitralDatos\vitral.log` usually says what went wrong.
- **Pick something from the list** in the README, or open an issue to propose
  your own idea before starting something big.

## Getting set up

You need Windows 10 or 11, the .NET 10 SDK and Node 24.

```bash
npm --prefix editor ci
npm --prefix editor run build
dotnet run --project src/Vitral.App
```

Close TRCC first: only one program can use the screen. No screen? The editor runs
in a browser against a simulated Vitral with `npm --prefix editor run dev`.

## Project layout

| Folder | What lives there |
|---|---|
| `src/Vitral.Core` | Protocol, model catalog and WinUSB, themes, backgrounds, drawing, the frame engine |
| `src/Vitral.Sensores` | LibreHardwareMonitor and the client for the sensor service |
| `src/Vitral.Servicio` | The sensor service (`VitralSensores.exe`) |
| `src/Vitral.App` | Tray app, updates and the editor window |
| `editor/` | The theme editor (React, Vite, Tailwind, Radix) |
| `tests/Vitral.Core.Tests` | Tests for the core |
| `docs/` | The screen protocol, architecture notes |

## Before opening a pull request

```bash
dotnet test tests/Vitral.Core.Tests
npm --prefix editor run typecheck
npm --prefix editor test
```

- Keep pull requests focused on one thing.
- Add tests for logic you add or change in the core or the editor.
- The code is written in Spanish (names, messages, UI text). Please follow that so
  it reads the same everywhere; translations will go through resource files.
- Anything that comes from a theme file is untrusted: validate it the way
  `ValidadorTemas` and `Medidas` do.
- Nothing in the app may need administrator rights. Privileged work belongs in the
  sensor service, which only reads.

## Adding support for a new screen

Most of the work is in `src/Vitral.Core/Dispositivo/Catalogo.cs` and
`docs/protocolo.md`. If your screen uses a different USB id or protocol, a USB
capture (USBPcap + Wireshark) of TRCC talking to it is the most useful thing you
can share.

By contributing, you agree that your work is released under the
[GPL-3.0](LICENSE).
