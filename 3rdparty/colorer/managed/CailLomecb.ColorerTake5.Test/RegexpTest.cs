using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using FluentAssertions;
using System.Diagnostics;

namespace CailLomecb.ColorerTake5.Test
{

    public class RegexpTest
    {
        [Fact]
        public void CreateDeleteNoCrash()
        {
            try
            {
                using (ColorerRegex re = ColorerRegex.Parse(@"/\w+/"))
                    re.IsDiposed().Should().BeFalse();
            }
            catch (Exception e)
            {
                e.Should().BeNull("The operation must not cause an exception");
            }
        }

        [Theory]
        [InlineData(@"/\w+/", "abcdef", true, 0, 6)]
        [InlineData(@"/abcdef/", "abcdef", true, 0, 6)]
        [InlineData(@"/Abcdef/", "abcdef", false)]
        [InlineData(@"/Abcdef/i", "abcdef", true, 0, 6)]
        [InlineData(@"/\w+/", "слово", true, 0, 5)]
        [InlineData(@"/слово/", "слово", true, 0, 5)]
        [InlineData(@"/слово/i", "СЛОВО", true, 0, 5)]
        [InlineData(@"/\w+/", "   abcdef    ", false)]
        public void SimpleParse(string expression, string text, bool success, int s = 0, int e = 0)
        {
            using (ColorerRegex re = ColorerRegex.Parse(expression))
            {
                using (ColorerMatches m = re.Parse(text))
                {
                    m.Success.Should().Be(success);
                    if (success)
                    {
                        m.Count.Should().Be(1);
                        ColorerMatch mx = m[0];
                        mx.Start.Should().Be(s);
                        mx.End.Should().Be(e);
                    }
                }
            }
        }
        [Theory]
        [InlineData(@"/\w+/", "abcdef", true, 0, 6)]
        [InlineData(@"/\w+/", "   abcdef    ", true, 3, 9)]
        public void SimpleFind(string expression, string text, bool success, int s = 0, int e = 0)
        {
            using (ColorerRegex re = ColorerRegex.Parse(expression))
            {
                using (ColorerMatches m = re.Find(text))
                {
                    m.Success.Should().Be(success);
                    if (success)
                    {
                        m.Count.Should().Be(1);
                        ColorerMatch mx = m[0];
                        mx.Start.Should().Be(s);
                        mx.End.Should().Be(e);
                    }
                }
            }
        }

        [Fact]
        public void FindAll()
        {
            using (ColorerRegex re = ColorerRegex.Parse(@"/\w+/")) 
            {
                string text = "   aaa bbb ccc";
                List<string> r = new List<string>();
                re.FindAll((s, e) =>
                {
                    r.Add(text.Substring(s, e - s));
                    return true;
                }, text);
                r.Count.Should().Be(3);
                r[0].Should().Be("aaa");
                r[1].Should().Be("bbb");
                r[2].Should().Be("ccc");

            }
        }

        [Fact]
        public void FindAllSpeed()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 1000; i++)
            {
                using (ColorerRegex re = ColorerRegex.Parse(@"/\w+/"))
                {
                    string text = "   aaa bbb ccc";
                    List<string> r = new List<string>();
                    re.FindAll((s, e) =>
                    {
                        r.Add(text.Substring(s, e - s));
                        return true;
                    }, text);
                    r.Count.Should().Be(3);
                    r[0].Should().Be("aaa");
                    r[1].Should().Be("bbb");
                    r[2].Should().Be("ccc");

                }
            }
            sw.Stop();
            sw.ElapsedMilliseconds.Should().BeLessThan(250);
                
        }


    }
}
