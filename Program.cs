namespace ConsoleOpcUAServer
{
    using Opc.UaFx;
    using Opc.UaFx.Server;

    using System;
    using System.Timers;

    class Program
    {
        private static OpcServer _server;
        private static Timer _timer;

        private static int _counter = 0;

        // Colección de nodos (por nombre)
        private static readonly Dictionary<string, OpcDataVariableNode<int>> _intNodes = new();

        // Evita reentrancia del timer
        private static int _tickRunning = 0;

        static void Main(string[] args)
        {
            // 1) Árbol
            var demoFolder = new OpcFolderNode("Demo");

            // 2) Crea N nodos de forma declarativa
            // Puedes añadir tantos como quieras
            AddIntNode(demoFolder, "MyChangingValue", 0);
            AddIntNode(demoFolder, "Variable1", 0);
            AddIntNode(demoFolder, "Variable2", 0);

            // Ejemplo: crear 20 nodos más
            for (int i = 1; i <= 20; i++)
            {
                AddIntNode(demoFolder, $"Tag{i:000}", 0);
            }

            // 3) Servidor
            _server = new OpcServer("opc.tcp://localhost:4840/", demoFolder);

            // _server.Security.AutoAcceptUntrustedCertificates = true; // si procede
            _server.Start();

            // 4) Timer
            _timer = new Timer(1000);
            _timer.AutoReset = true;
            _timer.Elapsed += (_, __) => Tick();
            _timer.Start();

            Console.WriteLine("Servidor OPC UA en marcha: opc.tcp://localhost:4840/");
            Console.WriteLine("Nodo ejemplo: Objects/Demo/MyChangingValue (Int32). Pulsa ENTER para salir.");
            Console.ReadLine();

            // 5) Limpieza
            _timer.Stop();
            _timer.Dispose();

            _server.Stop();
            _server.Dispose();
        }

        private static void AddIntNode(OpcFolderNode folder, string name, int initialValue)
        {
            var node = new OpcDataVariableNode<int>(folder, name, initialValue);
            _intNodes[name] = node;
        }

        private static void Tick()
        {
            // Evita que se pisen ticks si uno tarda
            if (System.Threading.Interlocked.Exchange(ref _tickRunning, 1) == 1)
                return;

            try
            {
                _counter++;

                // Acumula qué nodos cambiaron en este tick
                var changed = new List<OpcDataVariableNode<int>>(capacity: 16);

                // 1) Actualiza el contador principal
                if (TrySet("MyChangingValue", _counter, changed))
                {
                    // opcional
                }

                // 2) Tu lógica de negocio: cuando counter < 100, cambia otros
                if (_counter < 100)
                {
                    TrySet("Variable1", _counter, changed);
                    TrySet("Variable2", _counter, changed);
                }

                // 3) Ejemplo: actualizar tags masivos
                // (aquí puedes aplicar cualquier fórmula)
                /*for (int i = 1; i <= 20; i++)
                {
                    var name = $"Tag{i:000}";
                    var value = _counter + i; // ejemplo
                    TrySet(name, value, changed);
                }*/

                // 4) Notifica a clientes suscritos SOLO los nodos que cambiaron
                // (si tu librería permite batch, mejor aún, pero esto ya escala)
                foreach (var n in changed)
                {
                    n.ApplyChanges(_server.SystemContext);
                }

                // Logs (ojo: loggear 2000 tags cada segundo mata rendimiento)
                Console.WriteLine($"Tick {_counter} | Cambios: {changed.Count}");
                if (_intNodes.TryGetValue("MyChangingValue", out var c))
                    Console.WriteLine($"MyChangingValue = {c.Value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en Tick: " + ex);
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _tickRunning, 0);
            }
        }

        private static bool TrySet(string nodeName, int newValue, List<OpcDataVariableNode<int>> changed)
        {
            if (!_intNodes.TryGetValue(nodeName, out var node))
                return false;

            // Evita “ApplyChanges” si no cambió el valor (reduce tráfico/CPU)
            if (node.Value == newValue)
                return false;

            node.Value = newValue;
            changed.Add(node);
            return true;
        }
    }

}
