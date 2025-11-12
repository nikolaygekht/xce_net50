using System.Text;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Metadata for a text buffer including file information and formatting
    /// </summary>
    public class TextBufferMetadata
    {
        /// <summary>
        /// Gets or sets the file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the text encoding
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Gets or sets whether to skip writing BOM (Byte Order Mark)
        /// </summary>
        public bool SkipBom { get; set; }

        /// <summary>
        /// Gets or sets the end-of-line mode
        /// </summary>
        public EolMode EolMode { get; set; }

        /// <summary>
        /// Creates a new TextBufferMetadata with default values
        /// </summary>
        public TextBufferMetadata()
        {
            FileName = string.Empty;
            Encoding = Encoding.UTF8;
            SkipBom = false;
            EolMode = EolMode.CrLf;
        }

        /// <summary>
        /// Creates a new TextBufferMetadata with specified values
        /// </summary>
        public TextBufferMetadata(string fileName, Encoding encoding, bool skipBom, EolMode eolMode)
        {
            FileName = fileName ?? string.Empty;
            Encoding = encoding ?? Encoding.UTF8;
            SkipBom = skipBom;
            EolMode = eolMode;
        }
    }
}
