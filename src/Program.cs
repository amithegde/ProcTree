namespace ProcTree
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ProcTree <ProcessName>");
                return;
            }

            // Get the process name from the command line arguments
            string processName = $"{args[0]}.exe";

            // Create a configuration object with desired colors
            var config = new TreePrintConfig
            {
                ProcessNameColor = ConsoleColor.Blue,
                PidColor = ConsoleColor.Green,
                UserNameColor = ConsoleColor.DarkCyan,
                StartTimeColor = ConsoleColor.Green,
                TextColor = ConsoleColor.Gray,
                TreeLineColor = ConsoleColor.Red,
            };

            // Create a TreeLogger instance
            var logger = new TreeLogger(config);

            // Build the process tree and print it
            var processTreeBuilder = new ProcessTreeBuilder(processName, logger);
            processTreeBuilder.BuildAndPrintProcessTrees();
        }
    }
}
