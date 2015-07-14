using System.IO;
using System.Threading.Tasks;

namespace PhotoOrganizerWebJob
{
    internal static class TextWriterExtensionMethods
    {

        public static async Task WriteFormattedLineAsync(this TextWriter writer, string format, object value)
        {
            await writer.WriteLineAsync(string.Format(format, value));
        }

        public static void WriteFormattedLine(this TextWriter writer, string format, object value)
        {
            writer.WriteLine(string.Format(format, value));
        }

        public static async Task WriteFormattedLineAsync(this TextWriter writer, string format, params object[] values)
        {
            await writer.WriteLineAsync(string.Format(format, values));
        }

        public static void WriteFormattedLine(this TextWriter writer, string format, params object[] values)
        {
            writer.WriteLine(string.Format(format, values));
        }
    }
}
