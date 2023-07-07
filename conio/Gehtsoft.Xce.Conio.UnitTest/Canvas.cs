using FluentAssertions;
using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Gehtsoft.Xce.Conio.UnitTest
{
    public class CanvasTest
    {
        [Fact]
        public void Size()
        {
            Canvas cv = new Canvas(4, 20);
            cv.Data.Count.Should().Be(80);
            cv.ForegroundColor.Count.Should().Be(80);
            cv.BackgroundColor.Count.Should().Be(80);
            cv.Style.Count.Should().Be(80);
        }

        [Fact]
        public void ReadWrite()
        {
            Canvas cv = new Canvas(4, 20);
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 20; c++)
                {
                    int ix = r * 20 + c;
                    cv.Write(r, c, (char)('A' + ix), new CanvasColor((ushort)ix, 0x1000 + ix, 0x2000 + ix, (CanvasColor.ConsoleStyles)ix));
                }

            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 20; c++)
                {
                    CanvasCell cell = cv.Get(r, c);
                    int ix = r * 20 + c;
                    cell.Character.Should().Be((char)('A' + ix));
                    cell.Color.Should().Be(new CanvasColor((ushort)ix, 0x1000 + ix, 0x2000 + ix, (CanvasColor.ConsoleStyles)ix));
                }
        }

        [Fact]
        public void FillAll()
        {
            Canvas cv = new Canvas(4, 20);
            cv.Fill(0, 0, 4, 20, '0', new CanvasColor(0x03, 3, 0));
            cv.Fill(1, 1, 2, 18, '1', new CanvasColor(0x40, 0, 4));

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 20; j++)
                {
                    CanvasCell cell = cv.Get(i, j);
                    cell.Character.Should().Be((i == 0 || i == 3 || j == 0 || j == 19) ? '0' : '1');
                    cell.Color.Should().Be(i == 0 || i == 3 || j == 0 || j == 19 ? new CanvasColor(0x03, 3, 0) : new CanvasColor(0x40, 0, 4));
                }
        }

        [Fact]
        public void FillColor()
        {
            Canvas cv = new Canvas(4, 20);
            cv.Fill(0, 0, 4, 20, '0', new CanvasColor(0x13, 1, 3));
            cv.Fill(1, 1, 2, 18, new CanvasColor(0x25, 5, 5));

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 20; j++)
                {
                    CanvasCell cell = cv.Get(i, j);
                    cell.Character.Should().Be('0');
                    cell.Color.Should().Be(i == 0 || i == 3 || j == 0 || j == 19 ? new CanvasColor(0x13, 1, 3) : new CanvasColor(0x25, 5, 5));
                }
        }

        [Fact]
        public void FillFg()
        {
            Canvas cv = new Canvas(4, 20);
            cv.Fill(0, 0, 4, 20, '0', new CanvasColor(0x13, 1, 3));
            cv.FillFg(1, 1, 2, 18, new CanvasColor(0x25, 5, 5));

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 20; j++)
                {
                    CanvasCell cell = cv.Get(i, j);
                    cell.Character.Should().Be('0');
                    cell.Color.Should().Be(i == 0 || i == 3 || j == 0 || j == 19 ? new CanvasColor(0x13, 1, 3) : new CanvasColor(0x15, 5, 3));
                }
        }

        [Fact]
        public void FillBg()
        {
            Canvas cv = new Canvas(4, 20);
            cv.Fill(0, 0, 4, 20, '0', new CanvasColor(0x13, 1, 3));
            cv.FillBg(1, 1, 2, 18, new CanvasColor(0x25, 5, 5));

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 20; j++)
                {
                    CanvasCell cell = cv.Get(i, j);
                    cell.Character.Should().Be('0');
                    cell.Color.Should().Be(i == 0 || i == 3 || j == 0 || j == 19 ? new CanvasColor(0x13, 1, 3) : new CanvasColor(0x23, 1, 5));
                }
        }

        [Theory]
        [InlineData(1, 0, 6, "abcdef", 1, "abcdef", "Normal line")]
        [InlineData(1, 0, 5, "abcdef", 1, "abcde", "Cut line end")]
        [InlineData(1, 1, 5, "abcdef", 1, "bcdef", "Cut line start")]
        [InlineData(1, 0, 7, "abcdef", 1, "abcdef", "Longer than text")]
        [InlineData(15, 0, 6, "abcdef", 15, "abcde", "Out of screen to right")]
        [InlineData(-1, 0, 6, "abcdef", 0, "bcdef", "Out of screen to left")]
        [InlineData(20, 0, 6, "abcdef", 15, "", "Completely out of screen to right")]
        [InlineData(-6, 0, 6, "abcdef", 0, "", "Completely out of screen to left")]
        [InlineData(-1, 0, 22, "0123456789012345678901", 0, "12345678901234567890", "Cut at both side")]
        public void Write(int column, int offset, int length, string text, int probeColumn, string expectedText, string comment)
        {
            Canvas cv = new Canvas(4, 20);
            cv.Fill(0, 0, 4, 20, '0', new CanvasColor(0x03, 0, 3));
            cv.Write(1, column, text, offset, length, new CanvasColor(0x30, 3, 0));
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 19; j++)
                {
                    bool probe = (i == 1 && j >= probeColumn && j < probeColumn + expectedText.Length);
                    var cell = cv.Get(i, j);
                    if (!probe)
                    {
                        cell.Character.Should().Be('0', $"in {comment}");
                        cell.Color.Should().Be(new CanvasColor(0x03, 0, 3), $"in {comment}");
                    }
                    else
                    {
                        int pi = j - probeColumn;
                        cell.Character.Should().Be(expectedText[pi], $"in {comment}");
                        cell.Color.Should().Be(new CanvasColor(0x30, 3, 0), $"in {comment}");
                    }
                }
            }
        }

        [Fact]
        public void Box()
        {
            CanvasColor bg = new CanvasColor(0x03, 0, 3);
            CanvasColor box = new CanvasColor(0x30, 3, 0);
            Canvas cv = new Canvas(5, 20);
            cv.Fill(0, 0, 5, 20, '0', bg);
            cv.Box(1, 1, 3, 18, BoxBorder.Single, box);

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 19; j++)
                {
                    char c = '0';
                    CanvasColor color = box;
                    if (i == 1 && j == 1)
                        c = BoxBorder.Single.TopLeft;
                    else if (i == 1 && j == 18)
                        c = BoxBorder.Single.TopRight;
                    else if (i == 1 && j > 1 && j < 18)
                        c = BoxBorder.Single.Top;
                    else if (i == 3 && j == 1)
                        c = BoxBorder.Single.BottomLeft;
                    else if (i == 3 && j == 18)
                        c = BoxBorder.Single.BottomRight;
                    else if (i == 3 && j > 1 && j < 18)
                        c = BoxBorder.Single.Bottom;
                    else if (i == 2 && j == 1)
                        c = BoxBorder.Single.Left;
                    else if (i == 2 && j == 18)
                        c = BoxBorder.Single.Right;
                    else
                        color = bg;

                    var cell = cv.Get(i, j);
                    cell.Character.Should().Be(c);
                    cell.Color.Should().Be(color);
                }
            }
        }

        [Fact]
        public void FillBox()
        {
            CanvasColor bg = new CanvasColor(0x03, 0, 3);
            CanvasColor box = new CanvasColor(0x30, 3, 0);
            Canvas cv = new Canvas(5, 20);
            cv.Fill(0, 0, 5, 20, '0', bg);
            cv.Box(1, 1, 3, 18, BoxBorder.Single, box, ' ');

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 19; j++)
                {
                    char c = '0';
                    CanvasColor color = box;
                    if (i == 1 && j == 1)
                        c = BoxBorder.Single.TopLeft;
                    else if (i == 1 && j == 18)
                        c = BoxBorder.Single.TopRight;
                    else if (i == 1 && j > 1 && j < 18)
                        c = BoxBorder.Single.Top;
                    else if (i == 3 && j == 1)
                        c = BoxBorder.Single.BottomLeft;
                    else if (i == 3 && j == 18)
                        c = BoxBorder.Single.BottomRight;
                    else if (i == 3 && j > 1 && j < 18)
                        c = BoxBorder.Single.Bottom;
                    else if (i == 2 && j == 1)
                        c = BoxBorder.Single.Left;
                    else if (i == 2 && j == 18)
                        c = BoxBorder.Single.Right;
                    else if (i == 2 && j > 0 && j < 18)
                        c = ' ';
                    else
                        color = bg;

                    var cell = cv.Get(i, j);
                    cell.Character.Should().Be(c);
                    cell.Color.Should().Be(color);
                }
            }
        }

        [Theory]
        [InlineData(1, 1, 1, 3, 1, 10, "fit inside")]
        [InlineData(1, -1, 1, 3, 0, 9, "overlap left")]
        [InlineData(1, 35, 1, 3, 35, 5, "overlap right")]
        [InlineData(-1, 1, 0, 2, 1, 10, "overlap top")]
        [InlineData(-1, -1, 0, 2, 0, 9, "overlap top-left")]
        [InlineData(4, 1, 4, 1, 1, 10, "overlap bottom")]
        [InlineData(4, 35, 4, 1, 35, 5, "overlap bottom-right")]
        [InlineData(5, 50, 0, 0, 0, 0, "miss bottom-right")]
        [InlineData(-5, -10, 0, 0, 0, 0, "miss top-left")]
        public void Paint(int row, int column, int expectedRow, int expectedRows, int expectedColumn, int expectedColumns, string comment)
        {
            Canvas cv1 = new Canvas(5, 40);
            Canvas cv2 = new Canvas(3, 10);

            CanvasColor c1 = new CanvasColor(0x01, 1, 2);
            CanvasColor c2 = new CanvasColor(0x10, 10, 20);
            cv1.Fill(0, 0, 5, 50, '0', c1);
            cv2.Fill(0, 0, 3, 10, '1', c2);
            cv1.Paint(row, column, cv2);
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 40; j++)
                {
                    var cell = cv1.Get(i, j);
                    if (i >= expectedRow && i <= expectedRow + expectedRows - 1 && j >= expectedColumn && j <= expectedColumn + expectedColumns - 1)
                    {
                        cell.Character.Should().Be('1', $"expected copied data @{i},{j} in {comment}");
                        cell.Color.Should().Be(c2, $"expected copied color @{i},{j} in {comment}");
                    }
                    else
                    {
                        cell.Character.Should().Be('0', $"expected orirignal data @{i},{j} in {comment}");
                        cell.Color.Should().Be(c1, $"expected orirignal color @{i},{j} in {comment}");
                    }
                }
            }
        }
    }
}
