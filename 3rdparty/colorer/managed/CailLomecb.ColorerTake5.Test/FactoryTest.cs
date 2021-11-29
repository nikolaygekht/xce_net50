using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Resources;
using System.Text;
using System.Threading;
using Xunit;

#pragma warning disable S2259 // Null pointers should not be dereferenced

namespace CailLomecb.ColorerTake5.Test
{
    public class FactoryTest
    {
        private string FindCatalogue(string path = null)
        {
            if (path == null)
                return FindCatalogue(this.GetType().Assembly.Location);

            string probe = Path.Combine(path, "native/data/catalog.xml");
            if (File.Exists(probe))
                return probe;
            var di = new DirectoryInfo(path);
            if (di.Parent == null)
                return null;
            return FindCatalogue(di.Parent.FullName);
        }

        [Fact]
        public void ErrorTest()
        {
            Exception ee = null;
            try
            {
                ParserFactory f = ParserFactory.Create("nonexisting", "nonexisting", "nonexisting", 0);
                f.Should().BeNull("We should get here");
                f?.Dispose();
            }
            catch (Exception e)
            {
                ee = e;
            }
            ee.Should().NotBeNull();
            ee.Message.Should().Be("ParserFactoryException: InputSourceException: Can't open file 'nonexisting'");
        }

        [Fact]
        public void HasCatalogue()
        {
            FindCatalogue().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void HandleCatalogue()
        {
            ParserFactory f = null;
            try
            {
                Action x = (Action)(() => f = ParserFactory.Create(FindCatalogue(), "console", "xce", 0));
                x.Should().NotThrow();
                f.Should().NotBeNull();

                var styledRegion = f.FindStyledRegion("xce:TextColor");
                styledRegion.Should().NotBeNull();
                styledRegion.ConsoleColor.Should().Be(0x70);
                styledRegion.Foreground.Should().Be(0);
                styledRegion.Background.Should().Be(0xc0c0c0);

                var syntaxRegion1 = f.FindSyntaxRegion("def:Symbol");
                var syntaxRegion2 = f.FindSyntaxRegion("def:StructKeyword");
                var syntaxRegion3 = f.FindSyntaxRegion("c:KeywordStructure");
                var syntaxRegion4 = f.FindSyntaxRegion("c:KeywordStdio");
                var syntaxRegion5 = f.FindSyntaxRegion("def:Symbol");

                syntaxRegion1.Should().NotBeNull();
                syntaxRegion2.Should().NotBeNull();
                syntaxRegion3.Should().NotBeNull();
                syntaxRegion4.Should().NotBeNull();

                syntaxRegion2.IsDerivedFrom(syntaxRegion1).Should().BeFalse();
                syntaxRegion2.IsDerivedFrom(syntaxRegion2).Should().BeTrue();
                syntaxRegion3.IsDerivedFrom(syntaxRegion2).Should().BeTrue();
                syntaxRegion4.IsDerivedFrom(syntaxRegion3).Should().BeTrue();
                syntaxRegion4.IsDerivedFrom(syntaxRegion2).Should().BeTrue();
                syntaxRegion3.IsDerivedFrom(syntaxRegion4).Should().BeFalse();

                syntaxRegion5.Equals(syntaxRegion1).Should().BeTrue();
                syntaxRegion2.Equals(syntaxRegion1).Should().BeFalse();
            }
            finally
            {
                f?.Dispose();
            }
        }

        [Fact]
        public void SimpleParse()
        {
            var src = new TestLineSource();
            src.Add("<root>");
            src.Add("  <tag value=\"value\">");
            src.Add("  </tag>");
            src.Add("</root>");

            Tuple<int, int, string>[] expectedRegions = new Tuple<int, int, string>[]
            {
                new Tuple<int, int, string>(0, 1, "xml:text"),
                new Tuple<int, int, string>(2, 2, "xml:element.start.lt"),
                new Tuple<int, int, string>(3, 5, "xml:element.start.name"),
                new Tuple<int, int, string>(6, 6, null),
                new Tuple<int, int, string>(7, 11, "xml:Attribute.name"),
                new Tuple<int, int, string>(12, 12, "xml:Attribute.eq"),
                new Tuple<int, int, string>(13, 13, "xml:AttValue.start"),
                new Tuple<int, int, string>(14, 18, "xml:AttValue"),
                new Tuple<int, int, string>(19, 19, "xml:AttValue.end"),
                new Tuple<int, int, string>(20, 20, "xml:element.start.gt"),
            };

            using var factory = ParserFactory.Create(FindCatalogue(), "console", "xce", 5000);
            using var editor = factory.CreateEditor(src);

            editor.NotifyNameChange("test.xml");
            editor.NotifyLineCount(src.LinesCount);
            editor.NotifyIdle();
            editor.ValidateLines(0, 4);

            editor.FileType.Should().Be("xml");
            editor.LastValid.Should().BeGreaterThan(0);

            LineRegion r = editor.GetFirstRegionInLine(1);
            for (int i = 0; i < expectedRegions.Length; i++, r = r.Next)
            {
                r.Should().NotBeNull($"region {i}");
                r.Line.Should().Be(1, $"region {i}");
                r.Start.Should().Be(expectedRegions[i].Item1, $"region {i}");
                r.End.Should().Be(expectedRegions[i].Item2, $"region {i}");
                if (expectedRegions[i].Item3 == null)
                    r.SyntaxRegion.Should().BeNull();
                else
                {
                    r.SyntaxRegion.Should().NotBeNull($"region {i}");
                    r.SyntaxRegion.Name.Should().Be(expectedRegions[i].Item3, $"region {i}");
                }
            }
        }

        [Fact]
        public void ComplexParse()
        {
            var src = new TestLineSource();
            using (Stream s = this.GetType().Assembly.GetManifestResourceStream("CailLomecb.ColorerTake5.Test.data.lparser.c"))
            {
                var content = new byte[s.Length];
                s.Read(content, 0, content.Length);
                var stringContent = Encoding.ASCII.GetString(content);
                foreach (string str in stringContent.Split('\n'))
                    src.Add(str);
            }

            using var factory = ParserFactory.Create(FindCatalogue(), "console", "xce", 5000);
            using var editor = factory.CreateEditor(src);
            editor.NotifyNameChange("lparser.c");
            editor.NotifyLineCount(src.LinesCount);

            //repeat 10 times
            for (int j = 0; j < 6; j++)
            {
                src.Change(1318, j % 2 == 0 ? "return 0;" : "//return 0;");
                if (j >= 4)
                    editor.NotifyMajorChange(1300);
                else
                    editor.NotifyLineChange(1318);

                for (int i = 0; i < src.LinesCount; i += 10)
                {
                    editor.ValidateLines(i, i + 10);
                    if (i == 30)
                    {
                        LineRegion r = editor.GetFirstRegionInLine(37);
                        r.Should().NotBeNull();
                        r.SyntaxRegion.Should().NotBeNull();
                        r.SyntaxRegion.Name.Should().Be("c:Comment");
                    }
                    else if (i == 1310)
                    {
                        LineRegion r = editor.GetFirstRegionInLine(1318);
                        r.Should().NotBeNull();
                        r.SyntaxRegion.Should().NotBeNull();
                        if (j % 2 == 0)
                            r.SyntaxRegion.Name.Should().Be("c:KeywordANSI");
                        else
                            r.SyntaxRegion.Name.Should().Be("c:LineComment");
                    }
                }
            }
        }
    }
}

