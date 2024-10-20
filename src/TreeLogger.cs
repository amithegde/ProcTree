namespace ProcTree
{
    public class TreeLogger
    {
        private readonly TreePrintConfig config;

        public TreeLogger(TreePrintConfig config)
        {
            this.config = config;
        }

        public void PrintProcessLine(string indent, bool isLast, bool isRoot, ProcessInfo processInfo, string startTimeStr, string userName)
        {
            // Build the line parts
            string treeBranch = GetTreeBranch(indent, isLast, isRoot);

            // Print the line
            WriteColoredLine(treeBranch, processInfo, startTimeStr, userName);
        }

        private string GetTreeBranch(string indent, bool isLast, bool isRoot)
        {
            string branch = indent;

            if (!isRoot)
            {
                branch += isLast ? "└──" : "├──";
            }

            return branch;
        }

        private void WriteColoredLine(string treeBranch, ProcessInfo processInfo, string startTimeStr, string userName)
        {
            // Set tree line color and print the tree branch
            Console.ForegroundColor = config.TreeLineColor;
            Console.Write(treeBranch);

            // Reset color before process info
            Console.ResetColor();

            // Process name
            Console.ForegroundColor = config.ProcessNameColor;
            Console.Write(processInfo.Name);

            // User Name
            Console.ForegroundColor = config.TextColor;
            Console.Write(" (User: ");

            Console.ForegroundColor = config.UserNameColor;
            Console.Write(userName);

            // PID
            Console.ForegroundColor = config.TextColor;
            Console.Write(", PID: ");

            Console.ForegroundColor = config.PidColor;
            Console.Write(processInfo.ProcessId);

            // Start time
            Console.ForegroundColor = config.TextColor;
            Console.Write(", Started: ");

            Console.ForegroundColor = config.StartTimeColor; // Set start time color
            Console.Write(startTimeStr);

            Console.ForegroundColor = config.TextColor; // Reset to text color
            Console.WriteLine(")");

            // Reset color
            Console.ResetColor();
        }
    }
}
