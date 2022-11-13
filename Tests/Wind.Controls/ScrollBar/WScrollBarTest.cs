/**** BEGIN LICENSE BLOCK ****

BSD 3-Clause License

Copyright (c) 2022, the wind.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

**** END LICENCE BLOCK ****/

using System;
using NUnit.Framework;
//TODO WScorllBarTest.cs=codegen(table)

/* Go see the "diagram" at the beginning of "public class WScroller".
Test plan:  [x] initial state;
            [x] "Model" as a property;
            [x] parameters: [x] Size [x] Length [x] MinMaxVisible [x] MidOffset;
            [x] read-only-ies:  [x] MidSize [x] DisctinctPixels;
            [x] functions: [x] ScrollDistinctPixels [x] ScrollMinSmall [x] ScrollMaxSmall [x] ScrollMinLarge [x] ScrollMaxLarge; */

// each test fixture goes into its own namespace;
// "bug++" indicates a bug was found and fixed thanks to the test case
namespace Wind.Controls.ScrollBar // follows code-being-tested, paths
{
    using TestNS = Wind.Controls;
    using Wind; // the target namespace will always be an alias

    [TestFixture]
    public class NScrollerTest
    {
        TestNS.WScroller _s;

        [SetUp]
        public void setup() { _s = new TestNS.WScroller (); }

        // Model base used for testing the contract between IWScrollerModel and WScroller.
        class TestModel : TestNS.IWScrollerModel
        {
            public virtual int ComputeMidSize(TestNS.WScroller scroller, int local) { return local; }
            public virtual int ComputeMidOffset(TestNS.WScroller scroller, int local) { return 0; }
            public virtual void ScrollMinSmall(TestNS.WScroller scroller) { }
            public virtual void ScrollMaxSmall(TestNS.WScroller scroller) { }
            public virtual void ScrollMinLarge(TestNS.WScroller scroller) { }
            public virtual void ScrollMaxLarge(TestNS.WScroller scroller) { }
            public virtual int ScrollDistinctPixels(TestNS.WScroller scroller, int local, int d) { return scroller.MidOffset; }
            public event TestNS.WScrollerEventHandler SizeChanged = new TestNS.WScrollerEventHandler ((a, b) => { });
            public event TestNS.WScrollerEventHandler Scroll;
            void HandleMove() { Scroll (this, new TestNS.WScrollerEventArgs (TestNS.WScrollerEvent.Distinct)); }
            protected void NotifySizeChanged() { SizeChanged (this, null); }
        }

        [Test, Category ("InitialState")]
        public void InitialState_DisctinctPixels() { Assert.Zero (_s.DisctinctPixels); }
        [Test, Category ("InitialState")]
        public void InitialState_Length() { Assert.Zero (_s.Length); }
        [Test, Category ("InitialState")]
        public void InitialState_MidOffset() { Assert.Zero (_s.MidOffset); }
        [Test, Category ("InitialState")]
        public void InitialState_MidSize() { Assert.Zero (_s.MidSize); }
        [Test, Category ("InitialState")]
        public void InitialState_MinMaxVisible() { Assert.True (_s.MinMaxVisible); }
        [Test, Category ("InitialState")]
        public void InitialState_Model() { Assert.NotNull (_s.Model); }
        [Test, Category ("InitialState")]
        public void InitialState_Size() { Assert.Zero (_s.Size); }
        [Test, Category ("InitialState")]
        public void InitialState()
        {
            Assert.Zero (_s.DisctinctPixels);
            Assert.Zero (_s.Length);
            Assert.Zero (_s.MidOffset);
            Assert.Zero (_s.MidSize);
            Assert.True (_s.MinMaxVisible);
            Assert.NotNull (_s.Model);
            Assert.Zero (_s.Size);
        }

        // --[Model]-----------------------------------------------------------------------------------------------------------
        [Test, Category (".Model")]
        public void ModelCantBeNull()
        {
            Assert.NotNull (_s.Model);
            _s.Model = null;
            Assert.NotNull (_s.Model);
        }
        [Test, Category (".Model")]
        public void ModelCanBeAssigned()
        {
            var model = new TestModel ();
            _s.Model = model;
            Assert.AreEqual (expected: model, actual: _s.Model);
            var q = new TestNS.WScroller (model);
            Assert.AreEqual (expected: model, actual: q.Model);
        }

        // --[Size]------------------------------------------------------------------------------------------------------------
        [Test, Sequential, Category (".Size")]
        public void InitialState_Size_ValidValues([Values (0, 1, 10, 4321, 1 << 17)] int x)
        {
            _s.Size = x;
            Assert.AreEqual (x, _s.Size);
            // changing Size while everything else is 0, results in everything else remaining 0
            Assert.Zero (_s.DisctinctPixels);
            Assert.Zero (_s.Length);
            Assert.Zero (_s.MidOffset);
            Assert.Zero (_s.MidSize);
        }
        [Test, Sequential, Category (".Size")]
        public void Size_NoNo([Values (-1, -10, -4321, int.MinValue, int.MinValue >> 2, int.MaxValue, int.MaxValue >> 2)] int x)
        {
            Assert.That (() => _s.Size = x, Throws.InstanceOf<WArgumentException> ());
        }
        [Test, Sequential, Category (".Size")]
        public void InitialState_Size_ValidValues_LengthSet(
            [Values (0, 1, 10, 31, 32, 33, 34, 35, 48, 49, 50, 51, 52, 98, 99, 100, 101, 102, 4321, 1 << 17)] int x)
        {
            // note: modifying this, modifies the Values above:
            //       orderby: 0, random, random, range(length/3, -2, +2), range(length/2, -2, +2), range(length, -2, +2), random, random,
            //       and the switch below: length/3, length/3 + 1
            var length = 100;
            _s.Length = length;
            Assume.That (_s.DisctinctPixels, Is.EqualTo (0));
            Assume.That (_s.Length, Is.EqualTo (length));
            Assume.That (_s.MidOffset, Is.EqualTo (0));
            Assume.That (_s.MidSize, Is.EqualTo (length));
            _s.Size = x;
            Assert.AreEqual (expected: x, actual: _s.Size);
            Assert.AreEqual (expected: 0, actual: _s.DisctinctPixels); // model "length" equals scroller "local" - nothing to scroll
            // when Size becomes too large, the object enters NoRender state
            Assert.AreEqual (expected: length, actual: _s.Length, message: "Size shall not modify Length");
            Assert.AreEqual (expected: 0, actual: _s.MidOffset); // model "length" equals scroller "local" - nothing to scroll
            // The WScroller does not handle Size > Length scenarios - the renderer does; when 0 < _model.ComputeMidSize () < Size,
            // the middle button size is set to Size, because the model shall not concern itself with scrollbar rendering
            var local = length - 2 * x;
            switch (_s.Size)
            {
                case 33: Assert.AreEqual (34, _s.MidSize); break; // (local = 34) > (Size = 33) => MidSize = local
                case 34: Assert.AreEqual (34, _s.MidSize); break; // (local = 32) < (Size = 34) => MidSize = Size
            }
            // the default model fills the entire "local": "return local"
            Assert.AreEqual (expected: (local <= 0 ? 0 : (local < x ? x : local)), actual: _s.MidSize);
        }

        // --[Length]----------------------------------------------------------------------------------------------------------
        [Test, Sequential, Category (".Length")]
        public void InitialState_Length_ValidValues([Values (0, 1, 10, 4321, 1 << 17)] int x)
        {
            _s.Length = x;
            Assert.Zero (_s.DisctinctPixels); // no distinct pixels when Length is set only (Size is 0)
            Assert.AreEqual (expected: x, actual: _s.Length);
            Assert.Zero (_s.MidOffset);
            // MidSize fills "local"=Length (Size is 0) because the default model Length equals the scroller one - nothing to scroll
            Assert.AreEqual (expected: _s.Length, actual: _s.MidSize);
        }
        [Test, Sequential, Category (".Length")]
        public void Length_NoNo([Values (-1, -10, -4321, int.MinValue, int.MinValue >> 2, int.MaxValue, int.MaxValue >> 2)] int x)
        {
            Assert.That (() => _s.Size = x, Throws.InstanceOf<WArgumentException> ());
        }
        [Test, Sequential, Category (".Length")]
        public void InitialState_Length_ValidValues_SizeSet(
            [Values (0, 1, 5, 8, 9, 10, 11, 12, 18, 19, 20, 21, 22, 28, 29, 30, 31, 32, 4321, 1 << 17)] int x)
        {
            // note: modifying this, modifies the Values above:
            //       orderby: 0, random, random, range(size, -2, +2), range(2*size, -2, +2), range(3*size, -2, +2), random, random,
            //       and the switch below: range(2*size, -1, +1), range(3*size, -1, +1)
            var size = 10;
            _s.Size = size;
            Assume.That (_s.DisctinctPixels, Is.EqualTo (0));
            Assume.That (_s.Length, Is.EqualTo (0));
            Assume.That (_s.MidOffset, Is.EqualTo (0));
            Assume.That (_s.MidSize, Is.EqualTo (0));
            _s.Length = x;
            Assert.AreEqual (expected: size, actual: _s.Size, message: "Length shall not modify Size");
            Assert.AreEqual (expected: 0, actual: _s.DisctinctPixels); // model "length" equals scroller "local" - nothing to scroll
            // when Length becomes too small, the object enters NoRender state
            Assert.AreEqual (expected: x, actual: _s.Length);
            Assert.AreEqual (expected: 0, actual: _s.MidOffset); // model "length" equals scroller "local" - nothing to scroll
            // when MinMaxVisible is true:
            //  - for Length <= 2*Size         : MidSize = 0 - nothing to render
            //  - for 2*Size < Length < 3*Size : (local is [1;Size)) MidSize = Size, because defaultmodel.ComputeMidSize () < Size
            //  - when Length >= 3*Size        : MidSize = "local", because defaultmodel.ComputeMidSize () returns "local"
            var local = x - 2 * size;
            switch (_s.Length)
            {
                case 19: Assert.AreEqual (0, _s.MidSize); break; // (local =  -1)
                case 20: Assert.AreEqual (0, _s.MidSize); break; // (local =   0)
                case 21: Assert.AreEqual (_s.Size, _s.MidSize); break; // (local =   1) | [1;Size)
                case 29: Assert.AreEqual (_s.Size, _s.MidSize); break; // (local =   9) |
                case 30: Assert.AreEqual (local, _s.MidSize); break; // (local =  10)
                case 31: Assert.AreEqual (local, _s.MidSize); break; // (local =  11)
            }
            Assert.AreEqual (expected: (local <= 0 ? 0 : (local < size ? size : local)), actual: _s.MidSize, message: x.ToString ());
        }

        // --[MinMaxVisible]---------------------------------------------------------------------------------------------------
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible_DontModifyMe_Size()
        {
            var sentinel = _s.MinMaxVisible;
            _s.Size = 100;
            Assert.AreEqual (expected: sentinel, actual: _s.MinMaxVisible);
        }
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible_DontModifyMe_Length()
        {
            var sentinel = _s.MinMaxVisible;
            _s.Length = 100;
            Assert.AreEqual (expected: sentinel, actual: _s.MinMaxVisible);
        }
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible1_Length_Size() // Set order: MinMaxVisible=true, Size, Length
        {
            _s.Length = 100; Assert.AreEqual (expected: 100, actual: _s.MidSize);
            _s.Size = 10; Assert.AreEqual (expected: 80, actual: _s.MidSize);
        }
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible1_Size_Length() // Set order: MinMaxVisible=true, Size, Length
        {
            _s.Size = 10; Assert.AreEqual (expected: 0, actual: _s.MidSize);
            _s.Length = 100; Assert.AreEqual (expected: 80, actual: _s.MidSize);
        }
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible1_Length_Size_MinMaxVisible0() // Set order: MinMaxVisible=true, Length, Size, MinMaxVisible=false
        {
            _s.Length = 100; Assert.AreEqual (expected: 100, actual: _s.MidSize);
            _s.Size = 10; _s.MinMaxVisible = false; Assert.AreEqual (expected: 100, actual: _s.MidSize);
        }
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible1_MinMaxVisible0_Length_Size() // Set order: MinMaxVisible=true, MinMaxVisible=false, Length, Size
        {
            _s.MinMaxVisible = false; _s.Length = 100; Assert.AreEqual (expected: 100, actual: _s.MidSize);
            _s.Size = 10; Assert.AreEqual (expected: 100, actual: _s.MidSize);
        }
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible1_Size_Length_MinMaxVisible0() // Set order: MinMaxVisible=true, Size, Length, MinMaxVisible=false
        {
            _s.Size = 10; Assert.AreEqual (expected: 0, actual: _s.MidSize);
            _s.Length = 100; _s.MinMaxVisible = false; Assert.AreEqual (expected: 100, actual: _s.MidSize);
        }
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible1_MinMaxVisible0_Size_Length() // Set order: MinMaxVisible=true, MinMaxVisible=false, Size, Length
        {
            _s.MinMaxVisible = false; _s.Size = 10; Assert.AreEqual (expected: 0, actual: _s.MidSize);
            _s.Length = 100; Assert.AreEqual (expected: 100, actual: _s.MidSize);
        }
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible_Trigger_Length_Size() // true, Length, Size=Length, false, true
        {
            // when Size = Length, "middle_button.Visible = ! MinMaxVisible;"
            _s.Length = 10; _s.Size = _s.Length; Assert.AreEqual (expected: 0, actual: _s.MidSize);
            _s.MinMaxVisible = false; Assert.AreEqual (expected: _s.Length, actual: _s.MidSize); // now you see me
            _s.MinMaxVisible = true; Assert.AreEqual (expected: 0, actual: _s.MidSize); // now you don't
        }
        [Test, Category (".MinMaxVisible")]
        public void MinMaxVisible_Trigger_Size_Length() // true, Size, Length=Size, false, true
        {
            // when Size = Length, "middle_button.Visible = ! MinMaxVisible;"
            _s.Size = 10; _s.Length = _s.Size; Assert.AreEqual (expected: 0, actual: _s.MidSize);
            _s.MinMaxVisible = false; Assert.AreEqual (expected: _s.Length, actual: _s.MidSize); // now you see me
            _s.MinMaxVisible = true; Assert.AreEqual (expected: 0, actual: _s.MidSize); // now you don't
        }
        [Test, Category (".MinMaxVisible"), Description ("Part of the MinMaxVisible_ suite that found* a bug. Left here because of the bug")]
        public void MinMaxVisible_Trigger_local0_Size_Length() // Size, Length, {bug check: Length, Size}, false, true
        {
            // border case ("local" is 0) MinMaxVisible trigger;
            // what's "local" ?! - see the diagram at "public class WScroller"
            _s.Size = 10;
            _s.Length = 20;
            Assert.AreEqual (expected: 0, actual: _s.MidSize);
            // bug++: MidSize differs: "_s.Size = 10; _s.Length = 20;" and "_s.Length = 20; _s.Size = 10;"
            //        fix: UpdateMixSize() now knows that "0 < Size" and handles the 0 properly
            var _s1 = new TestNS.WScroller (); // allowed because this test requires 2 distinct instances
            {
                _s1.Length = 20;
                _s1.Size = 10;
                Assert.AreEqual (expected: _s.MidSize, actual: _s1.MidSize);
            }
            _s.MinMaxVisible = false;
            Assert.AreEqual (expected: 20, actual: _s.MidSize);
            _s.MinMaxVisible = true;
            Assert.AreEqual (expected: 0, actual: _s.MidSize);
        }

        // --[MidOffset]-------------------------------------------------------------------------------------------------------
        [Test, Category (".MidOffset")]
        public void MidOffset_ShallNotChange()
        {
            Assert.Zero (_s.DisctinctPixels); // MidOffset can't be moved when DisctinctPixels is 0
            // I should really create that "range" class; the long check takes care of the DisctinctPixels=0 case
            Assert.That (_s.MidOffset < _s.DisctinctPixels ? true : _s.MidOffset >= _s.DisctinctPixels ? true : false);

            var sentinel = _s.MidOffset;
            _s.MidOffset = 10;
            Assert.That (_s.MidOffset < _s.DisctinctPixels ? true : _s.MidOffset >= _s.DisctinctPixels ? true : false);
            Assert.AreEqual (expected: sentinel, actual: _s.MidOffset);

            sentinel = _s.MidOffset;
            _s.MidOffset = -10;
            Assert.That (_s.MidOffset < _s.DisctinctPixels ? true : _s.MidOffset >= _s.DisctinctPixels ? true : false);
            Assert.AreEqual (expected: sentinel, actual: _s.MidOffset);
        }
        class TestModel_MidOffset_Tests : TestModel
        {
            public override int ComputeMidSize(TestNS.WScroller scroller, int local) { return scroller.Size; }
        }
        [Test, Category (".MidOffset")]
        public void MidOffset_MoveAround()
        {
            var model = new TestModel_MidOffset_Tests ();
            _s.Model = model;
            Assert.AreEqual (expected: model, actual: _s.Model);
            _s.Length = 100;
            _s.Size = 10;
            Assert.AreEqual (expected: 10, actual: _s.MidSize);
            Assert.AreEqual (expected: 70, actual: _s.DisctinctPixels); // min max are visible

            Assert.Zero (_s.MidOffset);
            _s.MidOffset = 0; Assert.AreEqual (expected: 0, actual: _s.MidOffset); // move to min
            _s.MidOffset = 10; Assert.AreEqual (expected: 10, actual: _s.MidOffset); // move to in-between
            _s.MidOffset = 69; Assert.AreEqual (expected: 69, actual: _s.MidOffset); // move to max

            _s.MidOffset = 11; Assert.AreEqual (expected: 11, actual: _s.MidOffset);
            // bug++: stays on the last valid place, but it should't; it should move to the nearest possible valid place
            _s.MidOffset = -1; Assert.AreEqual (expected: 0, actual: _s.MidOffset); // move to min
            _s.MidOffset = 70; Assert.AreEqual (expected: 69, actual: _s.MidOffset); // move to max

            // bug++: MidOffset not in range when DisctinctPixels changes to 0; because "UpdateMidOffset()" was updating
            //        MidOffset when _dp > 0, only;
            _s.Size = 34;
            Assert.Zero (_s.DisctinctPixels);
            Assert.Zero (_s.MidOffset);
        }

        // --[MidSize]---------------------------------------------------------------------------------------------------------
        class TestModel_MidSize_Tests : TestModel
        {
            private long _large_size;
            public override int ComputeMidSize(TestNS.WScroller scroller, int local)
            {
                if (SmallSize >= LargeSize) return 0; // nothing to scroll - nothing to render
                var tmp = (int)(local * SmallSize / LargeSize); // 0 means other things
                return tmp < 1 ? 1 : tmp;
            }
            public int SmallSize { get; set; }

            public long LargeSize
            {
                get { return _large_size; }
                set
                {
                    if (value != _large_size)
                    {
                        _large_size = value;
                        NotifySizeChanged ();
                    }
                }
            }
        }
        [Test, Category (".MidSize")]
        public void MidSize_Computing()
        {
            var model = new TestModel_MidSize_Tests ();
            model.SmallSize = 20;
            model.LargeSize = 100;
            _s.Length = 100;
            _s.Size = 10;
            _s.Model = model;
            Assert.AreEqual (expected: model, actual: _s.Model);
            // bug++: assigning a model after setup ain't causing the expected update of the scroller state
            //        fix: UpdateLocal learned to react to model changed
            Assert.AreEqual (expected: 16, actual: _s.MidSize);
            model.LargeSize = 19;
            Assert.AreEqual (expected: 0, actual: _s.MidSize);
            model.LargeSize = 200;
            Assert.AreEqual (expected: 100, actual: _s.Length);
            Assert.AreEqual (expected: 10, actual: _s.Size);
            // its actually 8, but it can't be < Size; distinct pixels should be 72 but are 70 -
            // see the 4th "not-obvious thing" at WScrollBar.cs
            Assert.AreEqual (expected: _s.Size, actual: _s.MidSize);
            Assert.AreEqual (expected: 70, actual: _s.DisctinctPixels);
        }


        // --[DistinctPixels]--------------------------------------------------------------------------------------------------
        [Test, Category (".MidOffset")]
        public void DisctinctPixels_Computing()
        {
            _s.Model = new TestModel_MidOffset_Tests ();
            _s.Length = 100;
            _s.Size = 10;
            Assert.AreEqual (expected: 70, actual: _s.DisctinctPixels);
            _s.Size = 33; Assert.AreEqual (expected: 1, actual: _s.DisctinctPixels);
            _s.Size = 34; Assert.AreEqual (expected: 0, actual: _s.DisctinctPixels);
        }

        // --[ScrollDistinctPixels]--------------------------------------------------------------------------------------------
        [Test, Sequential, Category ("ScrollDistinctPixels")]
        public void InitialState_ScrollDistinctPixels_ValidValues([Values (0, -1, 1, -10, 10, 4321, -4321)] int x)
        {
            // bug++: _s.ScrollDistinctPixels (0) modifies state, and it shouldn't;
            // bug++: _s.ScrollDistinctPixels (1) fails to properly check [a;b) range when a=b=0
            var sentinel = _s.MidOffset;
            Assert.Zero (_s.DisctinctPixels);
            Assert.That (() => _s.ScrollDistinctPixels (x), Throws.Nothing);
            Assert.AreEqual (expected: sentinel, actual: _s.MidOffset, message: "MidOffset shall not be modified by ScrollDistinctPixels ()");
            Assert.Zero (_s.MidOffset, "MidOffset shall not be modified at all, when DisctinctPixels is 0");
        }
        [Test, Sequential, Category ("ScrollDistinctPixels")]
        public void InitialState_ScrollDistinctPixels_NoNo([Values (int.MinValue, int.MaxValue, int.MinValue >> 2, int.MaxValue >> 2)] int x)
        {
            Assert.That (() => _s.ScrollDistinctPixels (x), Throws.InstanceOf<WArgumentException> ());
        }

        class TestModel_ScrollDistinctPixels_Contract : TestModel
        {
            public override int ScrollDistinctPixels(TestNS.WScroller scroller, int local, int d)
            {
                // bug++: the contract was "its value is (-dp;dp)" - and it is wrong;
                // "dp" is "distinct pixels" >= 0; "d" is "delta" - the "delta" can be "dp";
                // contract corrected: "its value is |d| <= dp"
                Assert.That (scroller.DisctinctPixels >= 0);
                Assert.That (Math.Abs (d) <= scroller.DisctinctPixels);
                return base.ScrollDistinctPixels (scroller, local, d);
            }
        }
        [Test, Category ("ScrollDistinctPixels")]
        public void InitialState_ScrollDistinctPixels_Contract()
        {
            var model = new TestModel_ScrollDistinctPixels_Contract ();
            _s.Model = model;
            Assert.AreEqual (expected: model, actual: _s.Model);
            _s.ScrollDistinctPixels (0);
        }

        class TestModel_ScrollDistinctPixels_Contract_WrongComputing : TestModel
        {
            public override int ScrollDistinctPixels(TestNS.WScroller sender, int dp, int d)
            {
                Assert.NotNull (sender);
                return sender.MidOffset + 1; // the model fails to compute MidOffset
            }
        }
        [Test, Category ("ScrollDistinctPixels")]
        public void InitialState_ScrollDistinctPixels_Contract_WrongComputing()
        {
            var model = new TestModel_ScrollDistinctPixels_Contract_WrongComputing ();
            _s.Model = model;
            Assert.AreEqual (expected: model, actual: _s.Model);
            Assert.That (() => _s.ScrollDistinctPixels (0), Throws.Exception);
        }

        // --[Scroll*]---------------------------------------------------------------------------------------------------------
        class ScrollFunctions_Model : TestModel
        {
            public int SmallSize { get; set; }
            public int LargeSize { get; set; }
            public int SmallPosition { get; set; }
            public override int ComputeMidOffset(TestNS.WScroller scroller, int local)
            {
                return SmallPosition < 0 ? -1 : (LargeSize > 0 ? (int)Math.Round (local * SmallPosition / (double)LargeSize) : 0);
            }
            public override int ComputeMidSize(TestNS.WScroller scroller, int local)
            {
                if (SmallSize > LargeSize)
                    return 0;
                if (SmallSize <= 0 || LargeSize <= 0)
                    return 0;
                var tmp = LargeSize > 0 ? (int)Math.Round (local * SmallSize / (double)LargeSize) : 0;
                return tmp < 1 ? 1 : tmp;
            }
            public override void ScrollMinSmall(TestNS.WScroller scroller)
            {
                if (SmallPosition > 0) SmallPosition--;
            }
            // page: 1 SmallSize
            public int LastVisiblePage { get { return LargeSize > SmallSize ? LargeSize - SmallSize : 0; } }
            public override void ScrollMaxSmall(TestNS.WScroller scroller)
            {
                if (SmallPosition >= 0 && SmallPosition < LastVisiblePage) SmallPosition++;
            }
            public override void ScrollMinLarge(TestNS.WScroller scroller)
            {
                if ((SmallPosition -= SmallSize) < 0) SmallPosition = 0; // snap to 1st visible page start offset
            }
            public override void ScrollMaxLarge(TestNS.WScroller scroller)
            {
                if ((SmallPosition += SmallSize) > LastVisiblePage) SmallPosition = LastVisiblePage; // snap to last visible page start offset
            }
        }//class ScrollFunctions_Model
        ScrollFunctions_Model SetUp_Scroll_Test()
        {
            var model = new ScrollFunctions_Model ();
            model.LargeSize = 150;
            model.SmallSize = 50;
            model.SmallPosition = 0;
            _s.Model = model;
            Assert.AreEqual (expected: model, actual: _s.Model);
            _s.Size = 10;
            _s.Length = model.SmallSize + 2 * _s.Size; // let "local" be model.SmallSize
            return model;
        }
        [Test, Category ("Scroll*")]
        public void ScrollMinSmall_Computing()
        {
            var model = SetUp_Scroll_Test ();

            _s.ScrollMinSmall ();
            Assert.Zero (model.SmallPosition);
            Assert.Zero (_s.MidOffset);

            model.SmallPosition = 1; _s.ScrollMinSmall ();
            Assert.Zero (model.SmallPosition);
            Assert.Zero (_s.MidOffset);                          // 0/3
            model.SmallPosition = 2; _s.ScrollMinSmall ();
            Assert.AreEqual (expected: 1, actual: model.SmallPosition);
            Assert.Zero (_s.MidOffset);                          // 1/3
            model.SmallPosition = 3; _s.ScrollMinSmall ();
            Assert.AreEqual (expected: 2, actual: model.SmallPosition);
            Assert.AreEqual (expected: 1, actual: _s.MidOffset); // 2/3
            model.SmallPosition = 4; _s.ScrollMinSmall ();
            Assert.AreEqual (expected: 3, actual: model.SmallPosition);
            Assert.AreEqual (expected: 1, actual: _s.MidOffset); // 3/3
            model.SmallPosition = 5; _s.ScrollMinSmall ();
            Assert.AreEqual (expected: 4, actual: model.SmallPosition);
            Assert.AreEqual (expected: 1, actual: _s.MidOffset); // 4/3
            model.SmallPosition = 6; _s.ScrollMinSmall ();
            Assert.AreEqual (expected: 5, actual: model.SmallPosition);
            Assert.AreEqual (expected: 2, actual: _s.MidOffset); // 5/3
            model.SmallPosition = 7; _s.ScrollMinSmall ();
            Assert.AreEqual (expected: 6, actual: model.SmallPosition);
            Assert.AreEqual (expected: 2, actual: _s.MidOffset); // 6/3

            model.SmallPosition = model.LastVisiblePage + 2; _s.ScrollMinSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage + 1, actual: model.SmallPosition);
            Assert.AreEqual (expected: _s.DisctinctPixels - 1, actual: _s.MidOffset); // always fits, even when SmallPosition is not valid

            model.SmallPosition = -1;
            Assert.That (() => { _s.ScrollMinSmall (); }, Throws.Exception); // ComputeMidOffset() < 0
        }//public void ScrollMinSmall_Computing()
        [Test, Category ("Scroll*")]
        public void ScrollMaxSmall_Computing()
        {
            var model = SetUp_Scroll_Test ();

            model.SmallPosition = model.LastVisiblePage;
            _s.ScrollMaxSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage, actual: model.SmallPosition);
            Assert.AreEqual (expected: _s.DisctinctPixels - 1, actual: _s.MidOffset);

            model.SmallPosition = model.LastVisiblePage - 1; _s.ScrollMaxSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage, actual: model.SmallPosition);
            Assert.AreEqual (expected: 32, actual: _s.MidOffset);  // (LastVisiblePage/3)-(0/3) snapped to _s.DistinctPixels-1 | 33 1/3
            model.SmallPosition = model.LastVisiblePage - 2; _s.ScrollMaxSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage - 1, actual: model.SmallPosition);
            Assert.AreEqual (expected: 32, actual: _s.MidOffset); // (LastVisiblePage/3)-(1/3) snapped to _s.DistinctPixels-1 | 33
            model.SmallPosition = model.LastVisiblePage - 3; _s.ScrollMaxSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage - 2, actual: model.SmallPosition);
            Assert.AreEqual (expected: 32, actual: _s.MidOffset); // (LastVisiblePage/3)-(2/3) snapped to _s.DistinctPixels-1 | 32 2/3 
            model.SmallPosition = model.LastVisiblePage - 4; _s.ScrollMaxSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage - 3, actual: model.SmallPosition);
            Assert.AreEqual (expected: 32, actual: _s.MidOffset); // (LastVisiblePage/3)-(3/3)                                | 32 1/3 
            model.SmallPosition = model.LastVisiblePage - 5; _s.ScrollMaxSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage - 4, actual: model.SmallPosition);
            Assert.AreEqual (expected: 32, actual: _s.MidOffset); // (LastVisiblePage/3)-(4/3)                                | 32
            model.SmallPosition = model.LastVisiblePage - 6; _s.ScrollMaxSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage - 5, actual: model.SmallPosition);
            Assert.AreEqual (expected: 32, actual: _s.MidOffset); // (LastVisiblePage/3)-(5/3)                                | 31 2/3
            model.SmallPosition = model.LastVisiblePage - 7; _s.ScrollMaxSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage - 6, actual: model.SmallPosition);
            Assert.AreEqual (expected: 31, actual: _s.MidOffset); // (LastVisiblePage/3)-(5/3)                                | 31 1/3

            model.SmallPosition = model.LastVisiblePage + 1; _s.ScrollMaxSmall ();
            Assert.AreEqual (expected: model.LastVisiblePage + 1, actual: model.SmallPosition);
            Assert.AreEqual (expected: _s.DisctinctPixels - 1, actual: _s.MidOffset); // always fits, even when SmallPosition is not valid

            model.SmallPosition = -1;
            Assert.That (() => { _s.ScrollMaxSmall (); }, Throws.Exception); // ComputeMidOffset() < 0
        }//public void ScrollMaxSmall_Computing()
        [Test, Category ("Scroll*")]
        public void ScrollMinLarge_Computing()
        {
            var model = SetUp_Scroll_Test ();

            _s.ScrollMinLarge ();
            Assert.Zero (model.SmallPosition);

            model.SmallPosition += model.SmallSize; _s.ScrollMinLarge ();
            Assert.Zero (model.SmallPosition); Assert.Zero (_s.MidOffset);
            model.SmallPosition += model.SmallSize; model.SmallPosition--; _s.ScrollMinLarge ();
            Assert.Zero (model.SmallPosition); Assert.Zero (_s.MidOffset);
            model.SmallPosition += model.SmallSize; model.SmallPosition++; _s.ScrollMinLarge ();
            Assert.AreEqual (expected: 1, actual: model.SmallPosition); Assert.Zero (_s.MidOffset); // 1/3
            // scroll_max  - the thing that happens when one presses the "End" key (scroll_min: model.SmallPosition = 0);
            // it needs no special function as its as simple as:
            model.SmallPosition = model.LastVisiblePage;
            _s.ScrollMinLarge (); // 2/3 -> 1/3 | 100 -> 50
            Assert.AreEqual (expected: model.LastVisiblePage - model.SmallSize, actual: model.SmallPosition);
            // ComputeMidSize() == ComputeMidOffset(), because local == SmallSize, and MidPosition and SmallSize are both at 1/3 at this moment
            Assert.AreEqual (expected: _s.MidSize, actual: _s.MidOffset);
            _s.ScrollMinLarge ();
            Assert.Zero (model.SmallPosition); Assert.Zero (_s.MidOffset);

            model.SmallPosition = -1;
            Assert.That (() => { _s.ScrollMinLarge (); }, Throws.Nothing);
            Assert.Zero (model.SmallPosition);

            model.SmallPosition = 4 * model.SmallSize; _s.ScrollMinLarge ();
            Assert.AreEqual (expected: model.LastVisiblePage + model.SmallSize, actual: model.SmallPosition);
            Assert.AreEqual (expected: _s.DisctinctPixels - 1, actual: _s.MidOffset); // always fits, even when SmallPosition is not valid
        }//public void ScrollMinLarge_Computing()
        [Test, Category ("Scroll*")]
        public void ScrollMaxLarge_Computing()
        {
            var model = SetUp_Scroll_Test ();

            _s.ScrollMaxLarge ();
            Assert.AreEqual (expected: 50, actual: model.SmallPosition);
            Assert.AreEqual (expected: _s.MidSize, actual: _s.MidOffset);
            _s.ScrollMaxLarge ();
            Assert.AreEqual (expected: 100, actual: model.SmallPosition);
            Assert.AreEqual (expected: _s.DisctinctPixels - 1, actual: _s.MidOffset);
            _s.ScrollMaxLarge ();
            Assert.AreEqual (expected: 100, actual: model.SmallPosition);
            Assert.AreEqual (expected: _s.DisctinctPixels - 1, actual: _s.MidOffset);

            model.SmallPosition = 4 * model.SmallSize; _s.ScrollMaxLarge ();
            Assert.AreEqual (expected: 100, actual: model.SmallPosition);
            Assert.AreEqual (expected: _s.DisctinctPixels - 1, actual: _s.MidOffset);

            model.SmallPosition = -1;
            Assert.That (() => { _s.ScrollMaxLarge (); }, Throws.Nothing);
            Assert.AreEqual (expected: 49, actual: model.SmallPosition);
        }//public void ScrollMaxLarge_Computing()

        // --[WScrollerCommonModel, WTableViewScrollModel]---------------------------------------------------------------------
        [Test, Sequential, Category ("Bugs - _model.ScrollDistinctPixels bug")]
        public void Bug4([Values (25, 2)] int mo)
        {
            _s.Size = 17;
            _s.Length = 455;
            var model = new WScrollerCommonModel (15, 16);
            _s.Model = model;
            model.SmallPosition = 1;
            var dp = -1;
            _s.MidOffset = mo;
            _s.MidOffset += dp; Assert.AreEqual (expected: _s.MidOffset - dp, actual: mo);
            var q = model.ScrollDistinctPixels (_s, _s.Length - 2 * _s.Size, -1);
            Assert.AreEqual (q, _s.MidOffset);
            _s.Model = new WTableViewScrollModel (15, 16);
            q = model.ScrollDistinctPixels (_s, _s.Length - 2 * _s.Size, -1);
            Assert.AreEqual (q, _s.MidOffset);
        }// bug4()
        [Test, Category ("Bugs - division by your favorite number")]
        public void Bug4_1()
        {
            _s.Size = 17;
            _s.Length = 455;
            var model = new WScrollerCommonModel (15, 16);
            _s.Model = model;
            model.SmallPosition = 1;
            var dp = -12;
            _s.MidOffset += dp;
            var q = _s.MidOffset;
            Assert.That (() => q = model.ScrollDistinctPixels (_s, _s.Length - 2 * _s.Size, -1), Throws.Nothing);
            Assert.AreEqual (q, _s.MidOffset);
            _s.Model = new WTableViewScrollModel (15, 16);
            Assert.That (() => q = model.ScrollDistinctPixels (_s, _s.Length - 2 * _s.Size, -1), Throws.Nothing);
            Assert.AreEqual (q, _s.MidOffset);
        }// Bug4_1()
    }// public class WScrollerTest
}
