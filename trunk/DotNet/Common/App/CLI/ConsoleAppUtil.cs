using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.App.CLI
{
    public static class ConsoleAppUtil
    {
        public static string GetInteractiveInput(string purpose, bool display = false)
        {
            Console.WriteLine("Please enter the value for {0}.", purpose);
            StringBuilder input = new StringBuilder();
            if (!FillInputBuffer(input, display))
                return null;
            else
                return input.ToString();
        }

        private static bool FillInputBuffer(StringBuilder buffer, bool display)
        {
            Console.WriteLine("Backspace removes the last character, Delete clears the buffer; Enter to finish, Esc to cancel.");

            bool filled = false;
            for (ConsoleKeyInfo keyInfo = Console.ReadKey(!display);
                 !(filled = (keyInfo.Key == ConsoleKey.Enter)) && keyInfo.Key != ConsoleKey.Escape;
                 keyInfo = Console.ReadKey(true))
            {
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Backspace:
                        if (buffer.Length > 0)
                            buffer.Remove(buffer.Length - 1, 1);
                        break;

                    case ConsoleKey.Delete:
                        buffer.Clear();
                        break;

                    default:
                        buffer.Append(keyInfo.KeyChar);
                        break;
                }
            }
            return filled;
        }
    }
}
