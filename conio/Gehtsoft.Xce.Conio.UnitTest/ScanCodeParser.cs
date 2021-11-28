using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Gehtsoft.Xce.Conio.UnitTest
{
    public class ScanCodeParserTest
    {
        [Theory]
        [InlineData(ScanCode.F1, false, false, false, "F1")]
        [InlineData(ScanCode.F1, true, false, false, "shift-F1")]
        [InlineData(ScanCode.F1, false, true, false, "ctrl-F1")]
        [InlineData(ScanCode.F1, false, false, true, "alt-F1")]
        [InlineData(ScanCode.F1, true, true, true, "shift-ctrl-alt-F1")]
        public void ScanAndBack(ScanCode code, bool shift, bool control, bool alt, string expected)
        {
            ScanCodeParser.KeyCodeToName(code, shift, control, alt).Should().Be(expected);

            var sc = ScanCodeParser.NameToKeyCode(expected, out bool shift1, out bool control1, out bool alt1);
            sc.Should().Be(sc);
            shift1.Should().Be(shift);
            control1.Should().Be(control);
            alt1.Should().Be(alt);
        }
    }
}
