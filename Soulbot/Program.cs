namespace ProjectSoulbot
{
    class Program
    {
        static void Main(string[] args)
        {
            new Soulbot().MainAsync().GetAwaiter().GetResult();
        }
    }
}