using System.ServiceProcess;
using Vitral.Core.Ejecucion;
using Vitral.Servicio;

Registro.Configurar(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vitral", "sensores.log"));
ServiceBase.Run(new ServicioSensores());
