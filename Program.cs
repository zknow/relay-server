using RelayServer.Core;

namespace RelayServer;

public class Program
{
    static void Main(string[] args)
    {
        Config.LoadConfig();
        Server.Run();
        ConsoleInputListen();
        Close();
    }

    private static void ConsoleInputListen()
    {
        while (true)
        {
            string? line = Console.ReadLine();
            if (line == "!")
                break;
            switch (line)
            {
                case "session":
                    Console.WriteLine($"Session Count:{Server.Instance.SessionCount}");
                    break;
                default:
                    break;
            }
            Console.Clear();
            Console.WriteLine(line);
        }
    }

    private static void Close()
    {
        // Stop the server
        Console.Write("Server stopping...");
        Server.Instance.Stop();
        Server.Instance.Dispose();
        Console.WriteLine("Done!");
    }
}