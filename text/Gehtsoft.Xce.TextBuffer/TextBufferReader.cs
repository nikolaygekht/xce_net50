using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Reads a text file and creates a TextBuffer with detected metadata
    /// </summary>
    public class TextBufferReader
    {
        /// <summary>
        /// Reads a text file and returns a TextBuffer with metadata
        /// </summary>
        /// <param name="fileName">The file to read</param>
        /// <param name="encoding">The encoding to use (if null, will attempt to detect)</param>
        /// <returns>Tuple of TextBuffer and TextBufferMetadata</returns>
        public static (TextBuffer buffer, TextBufferMetadata metadata) Read(string fileName, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            if (!File.Exists(fileName))
                throw new FileNotFoundException("File not found", fileName);

            // Read all bytes from file
            byte[] fileBytes = File.ReadAllBytes(fileName);

            // Detect BOM and encoding if not specified
            bool hasBom = false;
            if (encoding == null)
            {
                (encoding, hasBom) = DetectEncoding(fileBytes);
            }
            else
            {
                hasBom = HasBom(fileBytes, encoding);
            }

            // Decode the text
            string text;
            if (hasBom)
            {
                // Skip BOM when decoding
                var preamble = encoding.GetPreamble();
                text = encoding.GetString(fileBytes, preamble.Length, fileBytes.Length - preamble.Length);
            }
            else
            {
                text = encoding.GetString(fileBytes);
            }

            // Detect EOL mode and split into lines
            EolMode eolMode = DetectEolMode(text);
            List<string> lines = SplitLines(text, eolMode);

            // Create TextBuffer
            var buffer = new TextBuffer(lines.ToArray());

            // Create metadata
            var metadata = new TextBufferMetadata(
                fileName,
                encoding,
                !hasBom, // SkipBom is true if file didn't have BOM
                eolMode
            );

            return (buffer, metadata);
        }

        /// <summary>
        /// Detects encoding from BOM, defaults to UTF-8 if no BOM found
        /// </summary>
        private static (Encoding encoding, bool hasBom) DetectEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return (Encoding.UTF8, true);

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                // Could be UTF-16 LE or UTF-32 LE
                if (bytes.Length >= 4 && bytes[2] == 0x00 && bytes[3] == 0x00)
                    return (Encoding.UTF32, true);
                return (Encoding.Unicode, true); // UTF-16 LE
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return (Encoding.BigEndianUnicode, true); // UTF-16 BE

            if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
                return (new UTF32Encoding(true, true), true); // UTF-32 BE

            // No BOM detected, default to UTF-8 without BOM
            return (new UTF8Encoding(false), false);
        }

        /// <summary>
        /// Checks if the file has a BOM for the specified encoding
        /// </summary>
        private static bool HasBom(byte[] bytes, Encoding encoding)
        {
            var preamble = encoding.GetPreamble();
            if (bytes.Length < preamble.Length)
                return false;

            for (int i = 0; i < preamble.Length; i++)
            {
                if (bytes[i] != preamble[i])
                    return false;
            }

            return preamble.Length > 0;
        }

        /// <summary>
        /// Detects the EOL mode from the text content
        /// </summary>
        private static EolMode DetectEolMode(string text)
        {
            // Count different line ending types
            int crlfCount = 0;
            int lfCount = 0;
            int crCount = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        crlfCount++;
                        i++; // Skip the \n
                    }
                    else
                    {
                        crCount++;
                    }
                }
                else if (text[i] == '\n')
                {
                    lfCount++;
                }
            }

            // Return the most common line ending
            if (crlfCount >= lfCount && crlfCount >= crCount)
                return EolMode.CrLf;
            else if (lfCount >= crCount)
                return EolMode.Lf;
            else
                return EolMode.Cr;
        }

        /// <summary>
        /// Splits text into lines based on EOL mode
        /// </summary>
        private static List<string> SplitLines(string text, EolMode eolMode)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text))
                return lines;

            var currentLine = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];

                if (ch == '\r')
                {
                    // Check if this is CRLF
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        // CRLF found
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                        i++; // Skip the \n
                    }
                    else
                    {
                        // CR only
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                }
                else if (ch == '\n')
                {
                    // LF only
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }
                else
                {
                    currentLine.Append(ch);
                }
            }

            // Add the last line if it doesn't end with EOL
            if (currentLine.Length > 0 || text.Length > 0)
                lines.Add(currentLine.ToString());

            return lines;
        }
    }
}
