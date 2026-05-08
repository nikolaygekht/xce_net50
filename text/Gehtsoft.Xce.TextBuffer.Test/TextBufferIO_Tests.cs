using System;
using System.IO;
using System.Text;
using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBufferIO_Tests : IDisposable
    {
        private readonly string mTempPath;
        private int mFileCounter = 0;

        public TextBufferIO_Tests()
        {
            mTempPath = Path.Combine(Path.GetTempPath(), "TextBufferTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(mTempPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(mTempPath))
            {
                Directory.Delete(mTempPath, true);
            }
        }

        private string GetTempFileName()
        {
            return Path.Combine(mTempPath, $"test_{mFileCounter++}.txt");
        }

        #region Metadata Tests

        [Fact]
        public void TextBufferMetadata_DefaultConstructor_SetsDefaults()
        {
            // Act
            var metadata = new TextBufferMetadata();

            // Assert
            metadata.FileName.Should().Be(string.Empty);
            metadata.Encoding.Should().Be(Encoding.UTF8);
            metadata.SkipBom.Should().Be(false);
            metadata.EolMode.Should().Be(EolMode.CrLf);
        }

        [Fact]
        public void TextBufferMetadata_ParameterizedConstructor_SetsProperties()
        {
            // Act
            var metadata = new TextBufferMetadata("test.txt", Encoding.ASCII, true, EolMode.Lf);

            // Assert
            metadata.FileName.Should().Be("test.txt");
            metadata.Encoding.Should().Be(Encoding.ASCII);
            metadata.SkipBom.Should().Be(true);
            metadata.EolMode.Should().Be(EolMode.Lf);
        }

        #endregion

        #region Writer Basic Tests

        [Fact]
        public void Writer_SimpleText_WritesCorrectly()
        {
            // Arrange
            var fileName = GetTempFileName();
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3" });
            var metadata = new TextBufferMetadata(fileName, Encoding.UTF8, true, EolMode.Lf);

            // Act
            TextBufferWriter.Write(buffer, metadata);

            // Assert
            var content = File.ReadAllText(fileName, Encoding.UTF8);
            content.Should().Be("line1\nline2\nline3");
        }

        [Fact]
        public void Writer_CrLfMode_WritesWindowsLineEndings()
        {
            // Arrange
            var fileName = GetTempFileName();
            var buffer = new TextBuffer(new[] { "line1", "line2" });
            var metadata = new TextBufferMetadata(fileName, Encoding.UTF8, true, EolMode.CrLf);

            // Act
            TextBufferWriter.Write(buffer, metadata);

            // Assert
            var content = File.ReadAllText(fileName, Encoding.UTF8);
            content.Should().Be("line1\r\nline2");
        }

        [Fact]
        public void Writer_CrMode_WritesMacLineEndings()
        {
            // Arrange
            var fileName = GetTempFileName();
            var buffer = new TextBuffer(new[] { "line1", "line2" });
            var metadata = new TextBufferMetadata(fileName, Encoding.UTF8, true, EolMode.Cr);

            // Act
            TextBufferWriter.Write(buffer, metadata);

            // Assert
            var content = File.ReadAllText(fileName, Encoding.UTF8);
            content.Should().Be("line1\rline2");
        }

        [Fact]
        public void Writer_WithBom_WritesUtf8Bom()
        {
            // Arrange
            var fileName = GetTempFileName();
            var buffer = new TextBuffer(new[] { "test" });
            var metadata = new TextBufferMetadata(fileName, Encoding.UTF8, false, EolMode.Lf);

            // Act
            TextBufferWriter.Write(buffer, metadata);

            // Assert
            var bytes = File.ReadAllBytes(fileName);
            bytes[0].Should().Be(0xEF);
            bytes[1].Should().Be(0xBB);
            bytes[2].Should().Be(0xBF);
        }

        [Fact]
        public void Writer_SkipBom_DoesNotWriteBom()
        {
            // Arrange
            var fileName = GetTempFileName();
            var buffer = new TextBuffer(new[] { "test" });
            var metadata = new TextBufferMetadata(fileName, Encoding.UTF8, true, EolMode.Lf);

            // Act
            TextBufferWriter.Write(buffer, metadata);

            // Assert
            var bytes = File.ReadAllBytes(fileName);
            bytes[0].Should().NotBe(0xEF);
        }

        [Fact]
        public void Writer_EmptyBuffer_CreatesEmptyFile()
        {
            // Arrange
            var fileName = GetTempFileName();
            var buffer = new TextBuffer();
            var metadata = new TextBufferMetadata(fileName, Encoding.UTF8, true, EolMode.Lf);

            // Act
            TextBufferWriter.Write(buffer, metadata);

            // Assert
            var content = File.ReadAllText(fileName);
            content.Should().Be(string.Empty);
        }

        #endregion

        #region Reader Basic Tests

        [Fact]
        public void Reader_SimpleUtf8File_ReadsCorrectly()
        {
            // Arrange
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "line1\nline2\nline3", new UTF8Encoding(false));

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line3");
            metadata.EolMode.Should().Be(EolMode.Lf);
            metadata.SkipBom.Should().Be(true);
        }

        [Fact]
        public void Reader_CrLfFile_DetectsWindowsLineEndings()
        {
            // Arrange
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "line1\r\nline2\r\nline3", new UTF8Encoding(false));

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert
            buffer.LinesCount.Should().Be(3);
            metadata.EolMode.Should().Be(EolMode.CrLf);
        }

        [Fact]
        public void Reader_CrFile_DetectsMacLineEndings()
        {
            // Arrange
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "line1\rline2\rline3", new UTF8Encoding(false));

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert
            buffer.LinesCount.Should().Be(3);
            metadata.EolMode.Should().Be(EolMode.Cr);
        }

        [Fact]
        public void Reader_Utf8WithBom_DetectsBom()
        {
            // Arrange
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "test", new UTF8Encoding(true));

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert
            metadata.SkipBom.Should().Be(false); // Had BOM, so SkipBom is false
            buffer.GetLine(0).Should().Be("test");
        }

        [Fact]
        public void Reader_Utf8WithoutBom_DetectsNoBom()
        {
            // Arrange
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "test", new UTF8Encoding(false));

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert
            metadata.SkipBom.Should().Be(true); // No BOM, so SkipBom is true
        }

        [Fact]
        public void Reader_EmptyFile_ReturnsEmptyBuffer()
        {
            // Arrange
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, string.Empty);

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert
            buffer.LinesCount.Should().Be(0);
        }

        [Fact]
        public void Reader_FileNotFound_Throws()
        {
            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                TextBufferReader.Read("nonexistent_file.txt"));
        }

        #endregion

        #region Round-Trip Tests

        [Fact]
        public void RoundTrip_Utf8LfNoBom_Preserves()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "line1", "line2", "line3" });
            var metadata = new TextBufferMetadata(fileName, Encoding.UTF8, true, EolMode.Lf);

            // Act - write and read back
            TextBufferWriter.Write(originalBuffer, metadata);
            var (readBuffer, readMetadata) = TextBufferReader.Read(fileName);

            // Assert
            readBuffer.LinesCount.Should().Be(3);
            readBuffer.GetLine(0).Should().Be("line1");
            readBuffer.GetLine(1).Should().Be("line2");
            readBuffer.GetLine(2).Should().Be("line3");
            readMetadata.EolMode.Should().Be(EolMode.Lf);
            readMetadata.SkipBom.Should().Be(true);
        }

        [Fact]
        public void RoundTrip_Utf8CrLfWithBom_Preserves()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "Hello", "World" });
            var metadata = new TextBufferMetadata(fileName, Encoding.UTF8, false, EolMode.CrLf);

            // Act - write and read back
            TextBufferWriter.Write(originalBuffer, metadata);
            var (readBuffer, readMetadata) = TextBufferReader.Read(fileName);

            // Assert
            readBuffer.LinesCount.Should().Be(2);
            readBuffer.GetLine(0).Should().Be("Hello");
            readBuffer.GetLine(1).Should().Be("World");
            readMetadata.EolMode.Should().Be(EolMode.CrLf);
            readMetadata.SkipBom.Should().Be(false);
        }

        [Fact]
        public void RoundTrip_CrMode_Preserves()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "a", "b", "c" });
            var metadata = new TextBufferMetadata(fileName, Encoding.UTF8, true, EolMode.Cr);

            // Act - write and read back
            TextBufferWriter.Write(originalBuffer, metadata);
            var (readBuffer, readMetadata) = TextBufferReader.Read(fileName);

            // Assert
            readBuffer.LinesCount.Should().Be(3);
            readBuffer.GetLine(0).Should().Be("a");
            readBuffer.GetLine(1).Should().Be("b");
            readBuffer.GetLine(2).Should().Be("c");
            readMetadata.EolMode.Should().Be(EolMode.Cr);
        }

        [Fact]
        public void RoundTrip_UnicodeWithBom_Preserves()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "Hello 世界", "Привет мир" });
            var metadata = new TextBufferMetadata(fileName, Encoding.Unicode, false, EolMode.CrLf);

            // Act - write and read back
            TextBufferWriter.Write(originalBuffer, metadata);
            var (readBuffer, readMetadata) = TextBufferReader.Read(fileName);

            // Assert
            readBuffer.LinesCount.Should().Be(2);
            readBuffer.GetLine(0).Should().Be("Hello 世界");
            readBuffer.GetLine(1).Should().Be("Привет мир");
            readMetadata.Encoding.Should().Be(Encoding.Unicode);
        }

        #endregion

        #region EOL Detection Tests

        [Fact]
        public void Reader_MixedEol_DetectsMostCommon()
        {
            // Arrange - mostly CRLF with one LF
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "line1\r\nline2\r\nline3\nline4\r\nline5", new UTF8Encoding(false));

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert - should detect CRLF as most common
            metadata.EolMode.Should().Be(EolMode.CrLf);
            buffer.LinesCount.Should().Be(5);
        }

        [Fact]
        public void Reader_SingleLine_DefaultsToEol()
        {
            // Arrange
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "single line no eol", new UTF8Encoding(false));

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("single line no eol");
        }

        #endregion

        #region Encoding Detection Tests

        [Fact]
        public void Reader_Utf16LE_DetectsEncoding()
        {
            // Arrange
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "test", new UnicodeEncoding(false, true));

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert
            metadata.Encoding.Should().Be(Encoding.Unicode);
            buffer.GetLine(0).Should().Be("test");
        }

        [Fact]
        public void Reader_Utf16BE_DetectsEncoding()
        {
            // Arrange
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "test", new UnicodeEncoding(true, true));

            // Act
            var (buffer, metadata) = TextBufferReader.Read(fileName);

            // Assert
            metadata.Encoding.Should().Be(Encoding.BigEndianUnicode);
            buffer.GetLine(0).Should().Be("test");
        }

        [Fact]
        public void RoundTrip_Utf16BE_WithBom_PreservesEndianness()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "Hello 世界", "Test данные" });
            var bigEndianEncoding = new UnicodeEncoding(true, true); // Big-endian with BOM
            var metadata = new TextBufferMetadata(fileName, bigEndianEncoding, false, EolMode.CrLf);

            // Act - write and read back
            TextBufferWriter.Write(originalBuffer, metadata);
            var (readBuffer, readMetadata) = TextBufferReader.Read(fileName);

            // Assert
            readBuffer.LinesCount.Should().Be(2);
            readBuffer.GetLine(0).Should().Be("Hello 世界");
            readBuffer.GetLine(1).Should().Be("Test данные");
            readMetadata.Encoding.CodePage.Should().Be(1201); // UTF-16BE code page
        }

        [Fact]
        public void RoundTrip_Utf16LE_WithBom_PreservesEndianness()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "Hello 世界", "Test данные" });
            var littleEndianEncoding = new UnicodeEncoding(false, true); // Little-endian with BOM
            var metadata = new TextBufferMetadata(fileName, littleEndianEncoding, false, EolMode.CrLf);

            // Act - write and read back
            TextBufferWriter.Write(originalBuffer, metadata);
            var (readBuffer, readMetadata) = TextBufferReader.Read(fileName);

            // Assert
            readBuffer.LinesCount.Should().Be(2);
            readBuffer.GetLine(0).Should().Be("Hello 世界");
            readBuffer.GetLine(1).Should().Be("Test данные");
            readMetadata.Encoding.CodePage.Should().Be(1200); // UTF-16LE code page
        }

        [Fact]
        public void RoundTrip_Utf32BE_WithBom_PreservesEndianness()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "Hello 世界 🌍", "Test данные 😀" });
            var bigEndianEncoding = new UTF32Encoding(true, true); // Big-endian with BOM
            var metadata = new TextBufferMetadata(fileName, bigEndianEncoding, false, EolMode.Lf);

            // Act - write and read back
            TextBufferWriter.Write(originalBuffer, metadata);
            var (readBuffer, readMetadata) = TextBufferReader.Read(fileName);

            // Assert
            readBuffer.LinesCount.Should().Be(2);
            readBuffer.GetLine(0).Should().Be("Hello 世界 🌍");
            readBuffer.GetLine(1).Should().Be("Test данные 😀");
            readMetadata.Encoding.CodePage.Should().Be(12001); // UTF-32BE code page
        }

        [Fact]
        public void RoundTrip_Utf32LE_WithBom_PreservesEndianness()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "Hello 世界 🌍", "Test данные 😀" });
            var littleEndianEncoding = new UTF32Encoding(false, true); // Little-endian with BOM
            var metadata = new TextBufferMetadata(fileName, littleEndianEncoding, false, EolMode.Lf);

            // Act - write and read back
            TextBufferWriter.Write(originalBuffer, metadata);
            var (readBuffer, readMetadata) = TextBufferReader.Read(fileName);

            // Assert
            readBuffer.LinesCount.Should().Be(2);
            readBuffer.GetLine(0).Should().Be("Hello 世界 🌍");
            readBuffer.GetLine(1).Should().Be("Test данные 😀");
            readMetadata.Encoding.CodePage.Should().Be(12000); // UTF-32LE code page
        }

        [Fact]
        public void RoundTrip_Utf16BE_NoBom_PreservesEndianness()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "Hello 世界" });
            var bigEndianEncoding = new UnicodeEncoding(true, true); // Big-endian with BOM
            var metadata = new TextBufferMetadata(fileName, bigEndianEncoding, true, EolMode.Lf); // Skip BOM

            // Act - write without BOM
            TextBufferWriter.Write(originalBuffer, metadata);

            // Manually read back with BigEndianUnicode
            var readBackText = File.ReadAllText(fileName, Encoding.BigEndianUnicode);

            // Assert
            readBackText.Should().Be("Hello 世界");
        }

        [Fact]
        public void RoundTrip_Utf32BE_NoBom_PreservesEndianness()
        {
            // Arrange
            var fileName = GetTempFileName();
            var originalBuffer = new TextBuffer(new[] { "Hello 🌍" });
            var bigEndianEncoding = new UTF32Encoding(true, true); // Big-endian with BOM
            var metadata = new TextBufferMetadata(fileName, bigEndianEncoding, true, EolMode.Lf); // Skip BOM

            // Act - write without BOM
            TextBufferWriter.Write(originalBuffer, metadata);

            // Manually read back with UTF-32 BE
            var readBackText = File.ReadAllText(fileName, new UTF32Encoding(true, false));

            // Assert
            readBackText.Should().Be("Hello 🌍");
        }

        #endregion

        #region D5 — Final newline byte-for-byte (PR3.2)

        // Pinning the trailing-empty-line convention: a file ending with N trailing
        // newline sequences is read into a buffer with N corresponding trailing
        // empty lines, and writing that buffer back produces the same file bytes.
        // Files with no trailing newline round-trip with no trailing newline.

        [Fact]
        public void FinalNewline_NoTrailingNewline_RoundTripsWithNoTrailingNewline_Lf()
        {
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "line1\nline2\nline3", new UTF8Encoding(false));

            var (buffer, metadata) = TextBufferReader.Read(fileName);
            buffer.LinesCount.Should().Be(3);                              // no trailing empty line
            buffer.GetLine(2).Should().Be("line3");

            var outFile = GetTempFileName();
            TextBufferWriter.Write(buffer, new TextBufferMetadata(outFile, metadata.Encoding, metadata.SkipBom, metadata.EolMode));
            File.ReadAllText(outFile, new UTF8Encoding(false)).Should().Be("line1\nline2\nline3");
        }

        [Fact]
        public void FinalNewline_NoTrailingNewline_RoundTripsWithNoTrailingNewline_CrLf()
        {
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "line1\r\nline2", new UTF8Encoding(false));

            var (buffer, metadata) = TextBufferReader.Read(fileName);
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(1).Should().Be("line2");

            var outFile = GetTempFileName();
            TextBufferWriter.Write(buffer, new TextBufferMetadata(outFile, metadata.Encoding, metadata.SkipBom, metadata.EolMode));
            File.ReadAllText(outFile, new UTF8Encoding(false)).Should().Be("line1\r\nline2");
        }

        [Fact]
        public void FinalNewline_OneTrailingNewline_RoundTripsExactly_Lf()
        {
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "line1\nline2\n", new UTF8Encoding(false));

            var (buffer, metadata) = TextBufferReader.Read(fileName);
            buffer.LinesCount.Should().Be(3);                              // line1, line2, ""
            buffer.GetLine(2).Should().Be("");

            var outFile = GetTempFileName();
            TextBufferWriter.Write(buffer, new TextBufferMetadata(outFile, metadata.Encoding, metadata.SkipBom, metadata.EolMode));
            File.ReadAllText(outFile, new UTF8Encoding(false)).Should().Be("line1\nline2\n");
        }

        [Fact]
        public void FinalNewline_OneTrailingNewline_RoundTripsExactly_CrLf()
        {
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "line1\r\nline2\r\n", new UTF8Encoding(false));

            var (buffer, metadata) = TextBufferReader.Read(fileName);
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(2).Should().Be("");

            var outFile = GetTempFileName();
            TextBufferWriter.Write(buffer, new TextBufferMetadata(outFile, metadata.Encoding, metadata.SkipBom, metadata.EolMode));
            File.ReadAllText(outFile, new UTF8Encoding(false)).Should().Be("line1\r\nline2\r\n");
        }

        [Fact]
        public void FinalNewline_TwoTrailingNewlines_RoundTripsExactly_Lf()
        {
            // Two trailing newlines = one extra empty line beyond the "trailing
            // newline" convention. Buffer holds the empty line explicitly.
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "abc\n\n", new UTF8Encoding(false));

            var (buffer, metadata) = TextBufferReader.Read(fileName);
            buffer.LinesCount.Should().Be(3);                              // "abc", "", ""
            buffer.GetLine(0).Should().Be("abc");
            buffer.GetLine(1).Should().Be("");
            buffer.GetLine(2).Should().Be("");

            var outFile = GetTempFileName();
            TextBufferWriter.Write(buffer, new TextBufferMetadata(outFile, metadata.Encoding, metadata.SkipBom, metadata.EolMode));
            File.ReadAllText(outFile, new UTF8Encoding(false)).Should().Be("abc\n\n");
        }

        #endregion

        #region Reader explicit-encoding override (PR3.2)

        [Fact]
        public void Reader_ExplicitEncodingOverride_DetectsBom_ButReturnsTheOverride()
        {
            // File has a UTF-8 BOM. Caller explicitly passes UTF-8. Reader must
            // detect+skip the BOM (because the override's preamble matches) AND
            // return the override as metadata.Encoding (not whatever DetectEncoding
            // would have re-derived).
            var fileName = GetTempFileName();
            File.WriteAllText(fileName, "test", new UTF8Encoding(true));   // BOM included

            var explicitEncoding = new UTF8Encoding(true);
            var (buffer, metadata) = TextBufferReader.Read(fileName, explicitEncoding);

            buffer.GetLine(0).Should().Be("test");
            metadata.Encoding.Should().Be(explicitEncoding);                // identity preserved
            metadata.SkipBom.Should().BeFalse();                            // BOM detected
        }

        [Fact]
        public void Reader_ExplicitEncodingOverride_NoBomMatch_OverrideStillWins()
        {
            // File has UTF-8 BOM but caller asks for ASCII (no preamble). The
            // override wins regardless: the reader uses ASCII to decode, the BOM
            // is not skipped (ASCII has no preamble to match), and metadata
            // reports ASCII as the encoding.
            var fileName = GetTempFileName();
            File.WriteAllBytes(fileName, new byte[] { 0xEF, 0xBB, 0xBF, (byte)'a', (byte)'b' });

            var (_, metadata) = TextBufferReader.Read(fileName, Encoding.ASCII);

            metadata.Encoding.Should().Be(Encoding.ASCII);
            metadata.SkipBom.Should().BeTrue();                             // ASCII has no BOM
        }

        [Fact]
        public void Reader_ExplicitEncoding_Utf16Le_ReadsCorrectlyWithoutAutoDetect()
        {
            // BOM-less UTF-16 LE — the reader cannot auto-detect this. Explicit
            // encoding is the only way.
            var fileName = GetTempFileName();
            var encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
            File.WriteAllText(fileName, "hello", encoding);

            var (buffer, metadata) = TextBufferReader.Read(fileName, encoding);

            buffer.GetLine(0).Should().Be("hello");
            metadata.Encoding.Should().Be(encoding);
        }

        #endregion

        #region Reader/writer argument validation (PR3.2)

        [Fact]
        public void Reader_NullFileName_Throws()
        {
            Assert.Throws<ArgumentException>(() => TextBufferReader.Read(null));
        }

        [Fact]
        public void Reader_EmptyFileName_Throws()
        {
            Assert.Throws<ArgumentException>(() => TextBufferReader.Read(""));
        }

        [Fact]
        public void Writer_NullBuffer_Throws()
        {
            var metadata = new TextBufferMetadata(GetTempFileName(), Encoding.UTF8, true, EolMode.Lf);
            Assert.Throws<ArgumentNullException>(() => TextBufferWriter.Write(null, metadata));
        }

        [Fact]
        public void Writer_NullMetadata_Throws()
        {
            var buffer = new TextBuffer(new[] { "x" });
            Assert.Throws<ArgumentNullException>(() => TextBufferWriter.Write(buffer, (TextBufferMetadata)null));
        }

        [Fact]
        public void Writer_EmptyFileNameInMetadata_Throws()
        {
            var buffer = new TextBuffer(new[] { "x" });
            var metadata = new TextBufferMetadata("", Encoding.UTF8, true, EolMode.Lf);
            Assert.Throws<ArgumentException>(() => TextBufferWriter.Write(buffer, metadata));
        }

        [Fact]
        public void Writer_UnwritableTarget_PropagatesIoException()
        {
            // Parent directory does not exist — file create fails.
            var buffer = new TextBuffer(new[] { "x" });
            var bogusPath = Path.Combine(mTempPath, "nonexistent_dir", "out.txt");
            var metadata = new TextBufferMetadata(bogusPath, Encoding.UTF8, true, EolMode.Lf);

            Assert.Throws<DirectoryNotFoundException>(() => TextBufferWriter.Write(buffer, metadata));
        }

        #endregion
    }
}
