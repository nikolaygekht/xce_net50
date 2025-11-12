using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable S4487, IDE0052 // Unread "private" fields should be removed

namespace Scintilla.CellBuffer.Test
{
    public class SimpleListTest
    {
        private readonly ITestOutputHelper mOutput;

        public SimpleListTest(ITestOutputHelper output)
        {
            mOutput = output;
        }

        [Fact]
        public void NewList()
        {
            var list = new SimpleList<int>();
            list.Count.Should().Be(0);
            list.Capacity.Should().Be(32);
        }

        [Fact]
        public void GrowCapacity()
        {
            var list = new SimpleList<int>();
            for (int i = 0; i < 32; i++)
                list.Add(i);
            list.Count.Should().Be(32);
            list.Capacity.Should().Be(32);
            list.Add(-1);
            list.Count.Should().Be(33);
            list.Capacity.Should().Be(64);
        }

        [Fact]
        public void Add()
        {
            var list = new SimpleList<int>();
            for (int i = 0; i < 64; i++)
                list.Add(i * 2);

            for (int i = 0; i < 64; i++)
                list[i].Should().Be(i * 2);
        }

        [Fact]
        public void InsertAt()
        {
            var list = new SimpleList<int>();
            for (int i = 0; i < 64; i++)
                list.InsertAt(i, -i);

            for (int i = 0; i < 64; i++)
                list.InsertAt(i, i * 2);

            for (int i = 0; i < 64; i++)
                list[i].Should().Be(i * 2);

            for (int i = 64; i < 128; i++)
                list[i].Should().Be(-(i - 64));
        }

        [Fact]
        public void Remove()
        {
            var list = new SimpleList<int>();
            for (int i = 0; i < 64; i++)
                list.InsertAt(i, i);

            list.RemoveAt(0);                   //remove first element  (0, 1, 2, 3 .. -> 1, 2, 3)
            list.RemoveAt(1);                   //remove in-between     (v: 1, 2, 3, 4 -> 1, 3, 4)
            list.RemoveAt(list.Count - 1);      //remove last           (v: 63)

            list.Count.Should().Be(61);
            list[0].Should().Be(1);
            list[1].Should().Be(3);

            for (int i = 2; i < list.Count; i++)
                list[i].Should().Be(i + 2);
        }

        [Fact]
        public void AddEmpty()
        {
            var list = new SimpleList<int>
            {
                1
            };
            list.AddEmptyElements(8);
            list.Count.Should().Be(9);
            list[0].Should().Be(1);
            for (int i = 1; i < list.Count; i++)
                list[i].Should().Be(default);
        }

        [Fact]
        public void CopyForwardOverlapping()
        {
            var list = new SimpleList<int>();
            for (int i = 0; i < 64; i++)
                list.InsertAt(i, i);

            list.Move(1, 3, 10);
            list[0].Should().Be(0);
            list[1].Should().Be(1);
            list[2].Should().Be(2);
            for (int i = 3; i < 13; i++)
                list[i].Should().Be(i - 2);
            for (int i = 13; i < 64; i++)
                list[i].Should().Be(i);
        }

        [Fact]
        public void CopyBackwardOverlapping()
        {
            var list = new SimpleList<int>();
            for (int i = 0; i < 64; i++)
                list.InsertAt(i, i);

            list.Move(3, 1, 10);
            list[0].Should().Be(0);
            for (int i = 1; i < 11; i++)
                list[i].Should().Be(i + 2);
            for (int i = 11; i < 64; i++)
                list[i].Should().Be(i);
        }

        [Fact]
        public void Enumerator()
        {
            var list = new SimpleList<int>();
            for (int i = 0; i < 4; i++)
                list.InsertAt(i, i);

            list.Should().HaveCount(4);
            list.Should().BeEquivalentTo(new [] { 0, 1, 2, 3 });
        }

#if RELEASE
        [Fact]
        public void CompareSpeedWithList()
        {
            Stopwatch sw = new Stopwatch();

            var list = new List<int>();
            var slist = new SimpleList<int>();

            sw.Reset();
            sw.Start();
            for (int i = 0; i < 200000; i++)
                list.Add(i);
            for (int i = 0; i < 10000; i++)
                list.Insert(i, i);
            for (int i = 0; i < list.Count; i += 3)
                list.RemoveAt(i);
            sw.Stop();
            var listTiming = sw.Elapsed.TotalMilliseconds;

            sw.Reset();
            sw.Start();
            for (int i = 0; i < 200000; i++)
                slist.Add(i);
            for (int i = 0; i < 10000; i++)
                slist.InsertAt(i, i);
            for (int i = 0; i < slist.Count; i += 3)
                slist.RemoveAt(i);
            sw.Stop();
            var slistTiming = sw.Elapsed.TotalMilliseconds;
            slistTiming.Should().BeLessOrEqualTo(listTiming * 1.1);
            mOutput.WriteLine("vs list {0} {1}", listTiming, slistTiming);
        }
        
        [Fact]
        public void CompareSpeedWithString()
        {
            Stopwatch sw = new Stopwatch();

            var @string = new string("");
            var slist = new SimpleList<char>();

            sw.Reset();
            sw.Start();
            for (int i = 0; i < 20000; i++)
                @string += 'a';
            for (int i = 0; i < 1000; i++)
                @string = @string.Insert(i, "b");
            for (int i = 0; i < @string.Length; i += 3)
                @string = @string.Remove(i, 1);
            sw.Stop();
            var stringTiming = sw.Elapsed.TotalMilliseconds;

            sw.Reset();
            sw.Start();
            for (int i = 0; i < 20000; i++)
                slist.Add('a');
            for (int i = 0; i < 1000; i++)
                slist.InsertAt(i, 'b');
            for (int i = 0; i < slist.Count; i += 3)
                slist.RemoveAt(i);
            sw.Stop();
            var slistTiming = sw.Elapsed.TotalMilliseconds;
            slistTiming.Should().BeLessOrEqualTo(stringTiming / 2);
            mOutput.WriteLine("vs string {0} {1}", stringTiming, slistTiming);
        }
#endif
    }
}
