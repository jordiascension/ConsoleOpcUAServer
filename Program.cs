namespace ConsoleOpcUAServer
{
    using Opc.UaFx;
    using Opc.UaFx.Server;

    using System;
    using System.Timers;

    class Program
    {
        private static OpcServer _server;
        private static OpcDataVariableNode<int> _counterNode;
        private static Timer _timer;
        private static int _counter = 0;

        static void Main(string[] args)
        {
            // 1) Define árbol de nodos
            var demoFolder = new OpcFolderNode("Demo");
            _counterNode = new OpcDataVariableNode<int>(demoFolder, "MyChangingValue", 0);

            // 2) Crea el servidor y registra el root (puedes cambiar el endpoint)
            _server = new OpcServer("opc.tcp://localhost:4840/", demoFolder);

            // Opcional para pruebas locales:
            //_server.Security.AutoAcceptUntrustedCertificates = true;

            // 3) Arranca el servidor
            _server.Start();

            // 4) Lanza un timer que actualiza el valor cada segundo
            _timer = new Timer(1000);
            _timer.Elapsed += (s, e) =>
            {
                _counter++;
                _counterNode.Value = _counter;

                Console.WriteLine("Node value " + _counterNode.Value);

                // Notifica el cambio a los clientes suscritos
                _counterNode.ApplyChanges(_server.SystemContext);
            };
            _timer.AutoReset = true;
            _timer.Start();

            Console.WriteLine("Servidor OPC UA en marcha: opc.tcp://localhost:4840/");
            Console.WriteLine("Nodo: Objects/Demo/MyChangingValue (Int32). Pulsa ENTER para salir.");
            Console.ReadLine();

            // 5) Limpieza
            _timer.Stop();
            _server.Stop();
            _server.Dispose();
        }
    }

}
