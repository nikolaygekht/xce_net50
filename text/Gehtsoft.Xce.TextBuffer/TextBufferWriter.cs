using System;
using System.IO;
using System.Text;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Writes a TextBuffer to a file using specified metadata
    /// </summary>
    public class TextBufferWriter
    {
        /// <summary>
        /// Writes a TextBuffer to a file using the metadata settings
        /// </summary>
        /// <param name="buffer">The text buffer to write</param>
        /// <param name="metadata">The metadata containing file name and formatting settings</param>
        public static void Write(TextBuffer buffer, TextBufferMetadata metadata)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            if (string.IsNullOrEmpty(metadata.FileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(metadata));

            // Determine the encoding to use
            Encoding encoding = metadata.Encoding ?? Encoding.UTF8;

            // Create encoding with or without BOM based on SkipBom flag
            Encoding encodingToUse;
            if (metadata.SkipBom)
            {
                // Create encoding without BOM
                if (encoding is UTF8Encoding)
                    encodingToUse = new UTF8Encoding(false);
                else if (encoding is UnicodeEncoding)
                    encodingToUse = new UnicodeEncoding(false, false);
                else if (encoding is UTF32Encoding)
                    encodingToUse = new UTF32Encoding(false, false);
                else
                    encodingToUse = encoding;
            }
            else
            {
                // Create encoding with BOM
                if (encoding is UTF8Encoding)
                    encodingToUse = new UTF8Encoding(true);
                else if (encoding is UnicodeEncoding)
                    encodingToUse = new UnicodeEncoding(false, true);
                else if (encoding is UTF32Encoding)
                    encodingToUse = new UTF32Encoding(false, true);
                else
                    encodingToUse = encoding;
            }

            // Determine EOL string
            string eol = GetEolString(metadata.EolMode);

            // Build the file content
            using (var stream = new FileStream(metadata.FileName, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream, encodingToUse))
            {
                for (int i = 0; i < buffer.LinesCount; i++)
                {
                    string line = buffer.GetLine(i);
                    writer.Write(line);

                    // Write EOL after each line except potentially the last one
                    // (we write EOL after every line to maintain consistency)
                    if (i < buffer.LinesCount - 1)
                        writer.Write(eol);
                }
            }
        }

        /// <summary>
        /// Writes a TextBuffer to a file using the specified file name and metadata
        /// </summary>
        /// <param name="buffer">The text buffer to write</param>
        /// <param name="fileName">The file name to write to</param>
        /// <param name="metadata">The metadata containing formatting settings</param>
        public static void Write(TextBuffer buffer, string fileName, TextBufferMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            var metadataWithFileName = new TextBufferMetadata(
                fileName,
                metadata.Encoding,
                metadata.SkipBom,
                metadata.EolMode
            );

            Write(buffer, metadataWithFileName);
        }

        /// <summary>
        /// Gets the EOL string for the specified mode
        /// </summary>
        private static string GetEolString(EolMode mode)
        {
            return mode switch
            {
                EolMode.CrLf => "\r\n",
                EolMode.Cr => "\r",
                EolMode.Lf => "\n",
                _ => "\r\n"
            };
        }
    }
}
