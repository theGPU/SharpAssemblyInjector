namespace TestApp
{
    public class Program
    {
        public static int Counter { get; set; } = 0;

        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine(Counter++);
                Thread.Sleep(1000);
            }
        }
    }
}
