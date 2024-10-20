namespace ProcTree
{
    public class TreePrintConfig
    {
        public ConsoleColor ProcessNameColor { get; set; } = ConsoleColor.Blue;
        public ConsoleColor PidColor { get; set; } = ConsoleColor.Green;
        public ConsoleColor UserNameColor { get; set; } = ConsoleColor.Magenta;
        public ConsoleColor StartTimeColor { get; set; } = ConsoleColor.Green;
        public ConsoleColor TextColor { get; set; } = ConsoleColor.Gray;
        public ConsoleColor TreeLineColor { get; set; } = ConsoleColor.Red;
    }
}
