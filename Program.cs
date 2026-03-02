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
            for (int i = 1; i <= 7000; i++)
            {
                AddIntNode(demoFolder, $"Variable{i}", 0);
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
                TrySet("MyChangingValue", _counter, changed);
                TrySet("Variable1", _counter, changed);
                TrySet("Variable2", _counter, changed);
                TrySet("Variable1500", _counter, changed);
                TrySet("Variable3000", _counter, changed);
                TrySet("Variable4000", _counter, changed);
                TrySet("Variable5000", _counter, changed);
                TrySet("Variable6000", _counter, changed);
                TrySet("Variable7000", _counter, changed);
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
                if (_intNodes.TryGetValue("Variable1", out var v1))
                    Console.WriteLine($"Variable1 = {v1.Value}");
                if (_intNodes.TryGetValue("Variable2", out var v2))
                    Console.WriteLine($"Variable2 = {v2.Value}");
                if (_intNodes.TryGetValue("Variable1500", out var v1500))
                    Console.WriteLine($"Variable1500 = {v1500.Value}");
                if (_intNodes.TryGetValue("Variable3000", out var v3000))
                    Console.WriteLine($"Variable3000 = {v3000.Value}");
                if (_intNodes.TryGetValue("Variable4000", out var v4000))
                    Console.WriteLine($"Variable4000 = {v4000.Value}");
                if (_intNodes.TryGetValue("Variable5000", out var v5000))
                    Console.WriteLine($"Variable5000 = {v5000.Value}");
                if (_intNodes.TryGetValue("Variable6000", out var v6000))
                    Console.WriteLine($"Variable6000 = {v6000.Value}");
                if (_intNodes.TryGetValue("Variable7000", out var v7000))
                    Console.WriteLine($"Variable7000 = {v7000.Value}");
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
