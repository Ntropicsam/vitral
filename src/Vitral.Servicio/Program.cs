using System.ServiceProcess;
using Vitral.Core.Ejecucion;
using Vitral.Servicio;

Registro.Configurar(Path.Combine(AppContext.BaseDirectory, "sensores.log"));
ServiceBase.Run(new ServicioSensores());
