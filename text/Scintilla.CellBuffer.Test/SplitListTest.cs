using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable S4487, IDE0052 // Unread "private" fields should be removed

namespace Scintilla.CellBuffer.Test
{
    public class SplitListTest
    {
        private readonly ITestOutputHelper mOutput;

        public SplitListTest(ITestOutputHelper output)
        {
            mOutput = output;
        }

        [Fact]
        public void NewList()
        {
            var list = new SplitList<int>();
            list.Count.Should().Be(0);
            list.GapLength.Should().Be(16);
            list.Part1Length.Should().Be(0);
            list.Part2Length.Should().Be(0);
        }

        [Fact]
        public void AppendToEnd()
        {
            var list = new SplitList<int>();
            for (int i = 0; i < 16; i++)
            {
                list.Add(i);
                list.Count.Should().Be(i + 1);
                list.GapLength.Should().Be(16 - list.Count);
                list.Part1Length.Should().Be(i + 1);
                list.Part2Length.Should().Be(0);
            }

            list.Add(17);
            list.GapLength.Should().Be(15);
            list.Part1Length.Should().Be(17);
            list.Part2Length.Should().Be(0);

            list.Should().BeEquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17 });
        }

        [Fact]
        public void Insert()
        {
            var list = new SplitList<int> { 0, 100 };
            for (int i = 1; i <= 16; i++)
            {
                list.InsertAt(i, i);
                list.Count.Should().Be(i + 2);
                list.GapLength.Should().Be(16 - i);
                list.Part1Length.Should().Be(i + 1);
                list.Part2Length.Should().Be(1);
            }

            list.GapLength.Should().Be(0);
            list.Should().BeEquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 100 });
        }

        [Fact]
        public void Set()
        {
            var list = new SplitList<int> { 0, 100 };
            list.InsertAt(1, 101);

            list[0] = 1;
            list[1] = 2;
            list[2] = 3;
            list.Part1Length.Should().Be(2);
            list.Part2Length.Should().Be(1);
            list[0].Should().Be(1);
            list[1].Should().Be(2);
            list[2].Should().Be(3);
        }

        [Fact]
        public void InsertCollection()
        {
            var list = new SplitList<int>() { 0, 100 };
            var list1 = new SplitList<int>() { 101, 102 };
            list.InsertAt(1, list1);

            list.Should().BeEquivalentTo(new[] { 0, 101, 102, 100 });
        }

        [Fact]
        public void InsertArray()
        {
            var list = new SplitList<int> { 0, 100 };

            var list1 = new int[] { 101, 102 };
            list.InsertAt(1, list1);

            list.Should().BeEquivalentTo(new[] { 0, 101, 102, 100 });
        }

        [Fact]
        public void MoveGap()
        {
            var list = new SplitList<int>();
            for (int i = 0; i < 8; i++)
                list.Add(i);

            list.Should().BeEquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            list.ToArray().Should().BeEquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });

            list.InsertAt(1, 101);
            list.Part1Length.Should().Be(2);
            list.GapLength.Should().Be(15);

            list[0].Should().Be(0);
            list[1].Should().Be(101);
            list[2].Should().Be(1);

            list.InsertAt(5, 102);
            list.Part1Length.Should().Be(6);
            list.GapLength.Should().Be(15);

            list.Should().BeEquivalentTo(new[] { 0, 101, 1, 2, 3, 102, 4, 5, 6, 7 });
            list.ToArray().Should().BeEquivalentTo(new[] { 0, 101, 1, 2, 3, 102, 4, 5, 6, 7 });

            list.Add(103);
            list.Part1Length.Should().Be(list.Count);
            list.Part2Length.Should().Be(0);
            list.GapLength.Should().Be(15);
            list.Should().BeEquivalentTo(new[] { 0, 101, 1, 2, 3, 102, 4, 5, 6, 7, 103 });
            list.ToArray().Should().BeEquivalentTo(new[] { 0, 101, 1, 2, 3, 102, 4, 5, 6, 7, 103 });
        }

        [Fact]
        public void Remove()
        {
            var list = new SplitList<int>();
            for (int i = 0; i < 8; i++)
                list.Add(i);

            list.Should().BeEquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });

            list.RemoveAt(1, 2);
            list.Part1Length.Should().Be(1);
            list.GapLength.Should().Be(18);

            list.Should().BeEquivalentTo(new[] { 0, 3, 4, 5, 6, 7 });
        }

        [Fact]
        public void Enumerator()
        {
            var list = new SplitList<int>();
            for (int i = 0; i < 4; i++)
                list.InsertAt(i, i);

            list.Should().HaveCount(4);
            list.Should().BeEquivalentTo(new[] { 0, 1, 2, 3 });
        }

        [Fact]
        public void StressTest()
        {
            var example = new List<int>();
            var test = new SplitList<int>();

            var r = new Random();

            for (int i = 0; i < 100; i++)
            {
                example.Add(i);
                test.Add(i);
            }

            for (int i = 0; i < 500; i++)
            {
                var pos = r.Next(example.Count);
                example.Insert(pos, i);
                test.InsertAt(pos, i);
            }

            example.RemoveAt(0);
            test.RemoveAt(0);
            example.RemoveAt(example.Count - 1);
            test.RemoveAt(test.Count - 1);

            for (int i = 0; i < 100; i++)
            {
                var pos = r.Next(example.Count);
                example.RemoveAt(pos);
                test.RemoveAt(pos);
            }
            test.Should().BeEquivalentTo(example);
        }
#if RELEASE
        [Fact]
        public void CompareInsertSpeedWithList()
        {
            Stopwatch sw = new Stopwatch();

            var list = new List<int>();
            var slist = new SplitList<int>();

            for (int i = 0; i < 10000; i++)
                list.Add(i);
            sw.Reset();
            sw.Start();
            for (int i = 0; i < 200000; i++)
                list.Insert(i, i);
            sw.Stop();
            var listTiming = sw.Elapsed.TotalMilliseconds;

            for (int i = 0; i < 10000; i++)
                slist.Add(i);
            sw.Reset();
            sw.Start();
            for (int i = 0; i < 200000; i++)
                slist.InsertAt(i, i);
            sw.Stop();
            var slistTiming = sw.Elapsed.TotalMilliseconds;
            slistTiming.Should().BeLessOrEqualTo(listTiming / 2);
            mOutput.WriteLine("vs list {0} {1}", listTiming, slistTiming);

            slist.Should().BeEquivalentTo(list);
        }
#endif
    }
}
