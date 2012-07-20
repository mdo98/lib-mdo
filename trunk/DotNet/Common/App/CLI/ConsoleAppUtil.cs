using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
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

        public static bool FillInputBuffer(StringBuilder buffer, bool display)
        {
            if (display)
            {
                buffer.Append(Console.ReadLine());
                return true;
            }

            Console.WriteLine("Backspace removes the last character, Delete clears the buffer; Enter to finish, Esc to cancel.");
            bool filled = false;
            for (ConsoleKeyInfo keyInfo = Console.ReadKey(true);
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
                        if ('\0' != keyInfo.KeyChar)
                            buffer.Append(keyInfo.KeyChar);
                        break;
                }
            }
            Console.WriteLine();
            return filled;
        }
    }
}
