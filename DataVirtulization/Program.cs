using System;

namespace DataVirtulization
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (VisualData game = new VisualData())
            {
                game.Run();
            }
        }
    }
#endif
}

