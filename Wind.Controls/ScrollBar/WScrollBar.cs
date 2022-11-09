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
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace Wind.Controls
{
    //TODO "unpack" :)

    //TODO what is it (besides custom drawing) that the built-in scrollbars can't do using percent(precision) scrolling?

    /* A scrollbar - allows you to view large area through a small one (clip rectangle).
    Use case:
      - you click on, or drag a, or press a, keyboard or graphics button, and the small area moves inside the large one -
        thus changing the rendering (the scrollbar needs to know small.position inside large.area)
      - you drag the large area around, or use the mouse scroll wheel, and the same movement happens, although this time
        the input delta is in pixels or mouse wheel delta

    The not-obvious things:
     - the scroller is not tied to a graphics control - it scrolls the thing that produces a rendering on that control
     - small.position and large.size are in the large area coordinate system
     - when moving the middle button you're switching between "pages", which number equals the distinct pixel positions:
       - 193 bytes file (1 byte per line being rendered, 10 pixels line height), and 100 distinct pixel positions of the bar:
         * at position 0: render 10 bytes from page 0: byte 0
         * at position 1: render 10 bytes from page 1: byte 1.93
         * at position 2: render 10 bytes from page 1: byte f(page_num)=page_num*(193/100)
         * ...
       The bytes, their number, the way and the where they're being rendered - all irrelevant to the scroller and its renderer.
       Because of these pages, sometimes when you move a scrollbar to its max position you see almost nothing on the screen -
       sometimes however, that shouldn't happen (like in file explorer views) and the computation changes: page_num*((193-x)/100),
       "x" being the things that can be rendered on the screen.
       Note the precision loss: can't fit that into an "int".
       Now imagine the above with a 193TB file - "int" is no longer enough for the page size computation.
     - the middle button size represents a ratio: small_area_size/large_area_size, but it has a minimum, so after a certain point
       it no longer reflects that ratio; e.g. large_area_size maps to (scrollbar_size - scrollbar_minmax_buttons_size)
     - small.size should modify small.position when (small.position + small.size) >= large.size
       (this became obvious after approximately 3 months when I actually started using the scrollbars)
       TODO fix this and add the appropriate tests
     Conclusion: use a state diagram when designing scrollbars - draw all events; not so simple after all.
    */
    public interface IWScrollerModel // provided by the thing being scrolled around
    {//NOTE consider the "sender" immutable, until I put a //TODO read-only proxy in place
        //NOTE the contract is valid for WScroller and its descendants here only

        // Return local * small_size / large_size:
        //   - if large_size is too big - return 1 - (when midsize < minmax buttons size, it is set to it: midsize = minmax buttons size);
        //   - if large_size <= small_size - return 0;
        // "small_size" is the size (to where the scrollbar is bound to: Horizontal - Width, Vertical - Height).
        int ComputeMidSize(WScroller scroller, int local);

        // Return local * small_position / large_size.
        // When the return value is >= DistinctPixels it will be set to DistinctPixels-1, for DistinctPixels > 0.
        // Exception: when you return a value < 0.
        int ComputeMidOffset(WScroller scroller, int local); // MidOffset = small_position * local / large_size

        // There are 5 distinct methods (no need for double switching (1 - controller; 2 - model)); Besides amount should be T, so:

        void ScrollMinSmall(WScroller scroller); // small step; small_position--
        void ScrollMaxSmall(WScroller scroller); // small step; small_position++
        void ScrollMinLarge(WScroller scroller); // large step; small_position -= small_size
        void ScrollMaxLarge(WScroller scroller); // large step; small_position += small_size

        // "d" is the amount of distinct pixels you need to scroll to - its value is |d| <= dp.
        // This will be called when the user drags the middle button.
        // Return the mid button position: local * small_position / large_size; ComputeMidOffset has to be high precision for this one.
        // Exception: when the result differs "sender.MidOffset".
        int ScrollDistinctPixels(WScroller scroller, int local, int d); // small_position = MidOffset * large_size / local

        // An exception will be thrown when sender != _model.
        // An exception will be thrown if you attempt stack overflow.
        event WScrollerEventHandler SizeChanged; // notify when your_size has been changed - no matter which one
        event WScrollerEventHandler Scroll;  // notify when your_offset has been changed, but not by an WScroller
    }// IWScrollerModel

    [Flags]
    public enum WScrollerEvent { Distinct = 1, Min = 2, Max = 4, Small = 8, Large = 16 };
    public class WScrollerEventArgs
    {
        public WScrollerEventArgs(WScrollerEvent e, int a = 0) { Event = e; Amount = a; }
        /// <summary>What happened.</summary>
        public WScrollerEvent Event { get; private set; }
        /// <summary>The mount of the thing that happened.</summary>
        public int Amount { get; private set; }
    }
    public delegate void WScrollerEventHandler(object sender, WScrollerEventArgs e);

    /// <summary>This is the scrollbar that has no relation to the thing being scrolled, let alone its rendering.</summary>
    public sealed class WScroller // : extends BlackBox ; no events here
    {
        /*               Min       NB1          Mid     NB2   Max    ___
            scrollbar: [<<<<<]..............[#########].....[>>>>>]  ___"a"

                       |     |<_mid_offset_>|         |     |     |
                       |<_a_>|              |<__mlen_>|     |<_a_>|
                       |     |<____________local___________>|     |
                       |<___________________len__________________>|

            _dp = _local - _mlen
        */ // See "ScrollBarPicture.dia" for a better view.
        private int _dp;  // distinct pixels: (Length - 2 * ButtonSize) - MidSize
        private int _len;  // the size of the scroller (Vertical - Height, Horizontal - Width)
        private int _mlen; // the size of the middle button (Vertical - Height, Horizontal - Width)
        private int _a;    // the size of the scrollbar and the min/max buttons (Vertical - Width, Horizontal - Height)
        private int _local; // this is the local space for the middle button: Length - 2 * ButtonSize
        private int _mid_offset; // the current middle button position
        private bool _mm_visible = true; // min max visible
        private IWScrollerModel _model = new DefaultModel ();

        /// <summary>Prevents "if (null == _model)". Does nothing. Throws nothing.</summary>
        sealed class DefaultModel : IWScrollerModel
        {
            public int ComputeMidSize(WScroller scroller, int local) { return local; }
            public int ComputeMidOffset(WScroller scroller, int local) { return 0; }
            public void ScrollMinSmall(WScroller scroller) { }
            public void ScrollMaxSmall(WScroller scroller) { }
            public void ScrollMinLarge(WScroller scroller) { }
            public void ScrollMaxLarge(WScroller scroller) { }
            public int ScrollDistinctPixels(WScroller scroller, int local, int d) { return scroller.MidOffset; }
            public event WScrollerEventHandler SizeChanged;
            public event WScrollerEventHandler Scroll;
            void Notify() { SizeChanged (null, null); Scroll (null, null); }
        }

        private void HandleModelChanged(object sender, WScrollerEventArgs e) { UpdateLocal (model_changed: true); }

        private void SetModel(IWScrollerModel value)
        {
            _model.SizeChanged -= HandleModelChanged;
            _model.Scroll -= HandleModelChanged;
            _model = value ?? _model;
            _model.SizeChanged += HandleModelChanged;
            _model.Scroll += HandleModelChanged;
            HandleModelChanged (null, null);
        }

        public WScroller() { }
        public WScroller(IWScrollerModel value) { SetModel (value); }

        private void UpdateDisctinctPixels()
        {
            _dp = _local - _mlen;
            WDebug.Assert (_dp >= 0, "You have a bug: fix it.");
            // the above is written like this (instead of checking _local < _mlen) to ensure that the author of this class
            // knows what he is doing; its mandatory that this class is 101% robust - and can withstand anything thrown at it;
        }

        // _mlen is set to [_a;_local] when _a < _local, 0 for t = 0, [_a] - otherwise.
        //TODONT private int _validation_dp = 0;
        private void UpdateMidSize()
        {
            var t = _model.ComputeMidSize (this, _local); // _local * small.size / large.size
            //TODONT _validation_dp = _local - t; // used for validation
            if (t != _mlen)
            {
                if (0 == (_mlen = t)) return;
                if (t < (_mlen = _a)) return; // _dp upper bound can no longer be used for _mid_offset validation
                if (t > (_mlen = _local)) return;
                _mlen = t;
            }
        }
        private void UpdateMidOffset()
        {
            if (_dp > 0) // ComputeMidOffset() will return 0 if _dp is 0
            {
                var t = _model.ComputeMidOffset (this, _local);
                WDebug.Assert (t >= 0, "_model.ComputeMidOffset bug");

                // the "=" is required due to Math.Round error, but is it the right thing to do?
                //TODO the right thing is _validation_dp to be high precision as well - and it means this check shall be perfomed by the model
                //TODONT if (_validation_dp > 0) WDebug.Assert (t <= _validation_dp, "_model.ComputeMidOffset bug (small_position out of order)");

                // after a certain point _dp becomes invalid, because _mlen stops reflecting "small_size" accurately
                _mid_offset = t >= _dp ? _dp - 1 : t;
            }
            else
                _mid_offset = 0;
        }
        private void UpdateLocal(bool model_changed = false)
        {
            var t = _mm_visible ? 2 * _a : 0;
            var l = _len >= t ? _len - t : 0; // _local shall be >= 0
            if (l != _local || model_changed)
            {
                _local = l;
                UpdateMidSize ();
                // the user can shrink the control in a way that _local < _mlen, but _dp can't be < 0
                if (_local >= _mlen)
                    UpdateDisctinctPixels (); // _local or _mlen changed
                UpdateMidOffset ();
            }
        }
        private void SetSize(ref int size, int value)
        {
            value.PreventStrangeRendering ();
            if (value != size) { size = value; UpdateLocal (); }
        }

        // The following 5 are used by the scrollbar renderer; ArgumentException when its value is outside [0;WaExtensions.MAX].
        public int Size // width (VerticalScrollbar) or height (HorizontalScrollbar)
        {
            get { return _a; }
            set { SetSize (ref _a, value); } // changes _len when _mm_visible only; if set to 0 - there will be no rendering
        }
        public int Length // min: "3 *_a" when the middle button is visible, and "2 * _a" otherwise
        {
            get { return _len; }
            set { SetSize (ref _len, value); } // if _mlen changes, _dp changes - thats a law; changing _len changes _mlen - another law;
        }
        public int MidOffset // [0;DisctinctPixels) for DisctinctPixels > 0; 0 therwise
        {
            get { return _mid_offset; }
            set
            {
                if (value != _mid_offset)
                {
                    if (value >= _dp) value = _dp - 1; // can become < 0
                    if (value < 0) value = 0;          // that's why this check is the 2nd one
                    _mid_offset = value;
                }
            }
        }
        public int MidSize { get { return _mlen; } } // you set this via Model.ComputeMidSize()
        public bool MinMaxVisible
        {
            get { return _mm_visible; }
            set { if (value != _mm_visible) { _mm_visible = value; UpdateLocal (); } }
        }

        public int DisctinctPixels { get { return _dp; } }

        public IWScrollerModel Model { get { return _model; } set { SetModel (value); } }

        // proxy

        public void ScrollMinSmall() { _model.ScrollMinSmall (this); UpdateMidOffset (); }
        public void ScrollMaxSmall() { _model.ScrollMaxSmall (this); UpdateMidOffset (); }
        public void ScrollMinLarge() { _model.ScrollMinLarge (this); UpdateMidOffset (); }
        public void ScrollMaxLarge() { _model.ScrollMaxLarge (this); UpdateMidOffset (); }
        public void ScrollDistinctPixels(int value) // _model.ScrollDistinctPixels contract
        {
            value.PreventStrangeRenderingOffset ();

            var p = _mid_offset + value;
            var upper_bound_mid_offset = p >= _dp ? _dp - 1 : p;
            _mid_offset = upper_bound_mid_offset < 0 ? 0 : upper_bound_mid_offset;
            //TODONT _mid_offset = (_mid_offset = p >= _dp ? _dp - 1 : p) < 0 ? 0 : _mid_offset; shorter but less readable
            var t = _model.ScrollDistinctPixels (this, _local, value);
            if (t != _mid_offset) throw new WException ("_model.ScrollDistinctPixels bug");
        }
    }// class WScroller

    // T=int
    // Use this if you want a classic scrollbar. Also, you can consider this an IWScrollerModel example.
    public class WScrollerCommonModel : IWScrollerModel
    {
        private int _small_size; // visible area size
        private int _large_size; // virtual area size
        private int _small_position; // visible area position in the virtual area
        bool _the_way_is_shut = false; // event driven - sanitizer
        public WScrollerCommonModel(int small = 0, int large = 0) { this.SmallSize = small; this.LargeSize = large; }
        delegate void Validator(int value); // throws ArgumentException if validation fails
        private void ValidateSetAndNotify(ref int field, int value, Validator validate, ref WScrollerEventHandler notify)
        {
            validate (value);
            WDebug.Assert (!_the_way_is_shut, "Congrats: You have an event loop or wrong threading");
            _the_way_is_shut = true;
            try
            {
                if (value != field)
                {
                    field = value;
                    notify (this, null);
                }
            }
            finally
            {
                _the_way_is_shut = false;
            }
        }
        public int SmallSize
        {
            get { return _small_size; }
            set { ValidateSetAndNotify (ref _small_size, value, (x) => { x.PreventStrangeRendering (); }, ref SizeChanged); }
        }
        public int LargeSize
        {
            get { return _large_size; }
            set { ValidateSetAndNotify (ref _large_size, value, (x) => { x.PositiveOrZero (); }, ref SizeChanged); }
        }
        // Small.Position (in Large space)
        public int SmallPosition //TODO this was not intuitive after a month; VirtualPosition or LargeOffset would be better
        {
            get { return _small_position; }
            set { ValidateSetAndNotify (ref _small_position, value, (x) => { x.InExclusiveRange (-1, _large_size); }, ref Scroll); }
        }
        #region IScrollerModel implementation
        public virtual int ComputeMidOffset(WScroller scroller, int local)
        {
            return _small_position < 0 ? -1 : WMath.LinearProjection (value: _small_position, from: _large_size, to: local);
            //(_large_size > 0 ? (int)Math.Round (local * _small_position / (double)_large_size) : 0);
        }
        public virtual int ComputeMidSize(WScroller scroller, int local)
        {
            // nothing to scroll - nothing to render; this handles both 0 0 and 100 100 cases; AutoHide is no longer an option
            if (SmallSize >= LargeSize) return 0;
            // if (_large_size <= 0 || _small_size <= 0) // not initialized?
            //     return local; // block scrolling, but keep rendering the scrollbar

            if (_small_size > _large_size)
                return 0; // block scrolling, no rendering
            var tmp = WMath.LinearProjection (value: _small_size, from: _large_size, to: local);
            // var tmp = (int)Math.Round (local * _small_size / (double)_large_size);
            return tmp < 1 ? 1 : tmp;
        }
        //TODO these are all wrong: they won't allow scroll notification when LargeSize isn't available.
        //     See WTableViewScrollModel for reference and unit tests.
        public virtual void ScrollMinSmall(WScroller scroller)
        {
            if (_small_position > 0) { _small_position--; Scroll (this, null); }
        }
        protected virtual int LastVisiblePage { get { return _large_size > _small_size ? _large_size - _small_size : 0; } }
        public virtual void ScrollMaxSmall(WScroller scroller)
        {
            if (_small_position >= 0 && _small_position < LastVisiblePage) { _small_position++; Scroll (this, null); }
        }
        public virtual void ScrollMinLarge(WScroller scroller)
        {
            var sentinel = _small_position;
            if ((_small_position -= _small_size) < 0) _small_position = 0; // snap to 1st visible page start offset
            if (sentinel != _small_position) Scroll (this, null);
        }
        public virtual void ScrollMaxLarge(WScroller scroller)
        {
            var sentinel = _small_position;
            if ((_small_position += _small_size) > LastVisiblePage) _small_position = LastVisiblePage; // snap to last visible page start offset
            if (sentinel != _small_position) Scroll (this, null);
        }
        public virtual int ScrollDistinctPixels(WScroller scroller, int local, int d)
        {
            if (LargeSize <= 0) return scroller.MidOffset; // nothing to scroll

            var sentinel = _small_position;
            _small_position = WMath.LinearProjection (value: scroller.MidOffset, from: local, to: _large_size);
            if (sentinel != _small_position) Scroll (this, null);
            //_small_position = local > 0 ? (int)Math.Round (_large_size * scroller.MidOffset / (double)local) : 0;
            return ComputeMidOffset (scroller, local);
        }//TODO all wrong - see the above start
        public event WScrollerEventHandler SizeChanged = new WScrollerEventHandler ((a, b) => { });
        //DONE this was not intuitive after a month; OnScroll would be better
        public event WScrollerEventHandler Scroll = new WScrollerEventHandler ((a, b) => { });
        #endregion
    }// class WScrollerCommonModel

    // Responsible for everything common to scrollbar renderers (VerticalScrollBar, PolynomialScrollbar, YourScrollbar, ..., Cake)
    //TODO shall become "sealed" one day using ScrollbarRenderStyle or something
    public abstract class WScrollBar : WControl
    {//TODO visual fx
        protected WScroller _s = new WScroller ();

        public WScrollBar()
        {
            MiddleVisible = true;
            var s = DefaultSize;
            _s.Length = s.Height;
            _s.Size = s.Width;

            _timer_c.Interval = COLOR_TIMER_INTERVAL;
            _timer_c.Tick += TimerHandler_Color;

            _timer_r.Interval = REPEAT_TIMER_INTERVAL;
            _timer_r.Tick += (a, b) =>
            {
                HandleMouseEvent (); _timer_r.Interval = COLOR_TIMER_INTERVAL;
            };
        }

        protected override bool NoRender
        {
            get
            {
                return (_s.Length <= 0 || _s.Size <= 0 ||
                    (_s.Size * (_s.MinMaxVisible ? 2 : 0) + _s.MidSize) > _s.Length);
            }
        }

        // If you resize it to a valid size it should start rendering again.
        private bool _auto_hide = false;
        private void VisibilityUpdate()
        {
            if (!Visible && _auto_hide) { Visible = !NoRender; _auto_hide = !Visible; }
            if (Visible && !_auto_hide) { Visible = !NoRender; _auto_hide = !Visible; }
        }
        protected override void OnResize(EventArgs e) { base.OnResize (e); VisibilityUpdate (); }

        [Browsable (false)]// for now
        public IWScrollerModel Model
        {
            get { return _s.Model; }
            set
            {
                _s.Model.Scroll -= ModelChanged;
                _s.Model.SizeChanged -= ModelChanged;
                _s.Model = value;
                _s.Model.Scroll += ModelChanged;
                _s.Model.SizeChanged += ModelChanged;
            }
        }

        private void ModelChanged(object sender, WScrollerEventArgs e)
        {
            MiddleVisible = _s.MidSize > 0;
            VisibilityUpdate ();
            this.Invalidate ();
        }

        [Browsable (true)]
        // Should be controllable from here only, because nothing in WScroller depends on middle button visibility AFAIK -
        // it could surprise me one day though.
        public bool MiddleVisible { get; private set; }

        // -simplicity ends here- - welcome to the event-driven web, where thigs look simultaneous - but aren't :).
        // Mouse; no keyboard events, except for TODO precise positioning - but that's later, when WEdit is ready.

        // 5 distinct areas that react to mouse events.
        protected enum HitTestArea { CleanFreshAir, Min, NB1, Mid, NB2, Max }; // NB (not a button)
        [Flags]
        protected enum MouseState { Unknown, Down = 1, Up = 2, Move = 4 };
        const int COLOR_TIMER_INTERVAL = 1000 / 16; // 16 FPS is good enough; | TODO to styles
        const int REPEAT_TIMER_INTERVAL = 1000 / 2; //                        |
        System.Windows.Forms.Timer _timer_c = new System.Windows.Forms.Timer (); // this stays this way (there are other "Timer"s)
        System.Windows.Forms.Timer _timer_r = new System.Windows.Forms.Timer (); // the repeat timer

        // Return the area that was hit by the mouse, or CleanFreshAir.
        protected abstract HitTestArea HitTest(MouseEventArgs e);
        HitTestArea _active_area = HitTestArea.CleanFreshAir;
        MouseState _mouse_state = MouseState.Unknown;
        int _mx = 0, _my = 0;

        protected abstract void BeginDrag(int mx, int my);
        protected abstract int Drag(int x, int y);// return delta distinct pixels - middle button delta [pixels]
        protected abstract void EndDrag(int x, int y);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _mx = e.X;
            _my = e.Y;
            _mouse_state = (_mouse_state & MouseState.Move) | MouseState.Down;
            _active_area = HitTest (e);
            if (HitTestArea.Mid == _active_area)
                BeginDrag (e.X, e.Y);
            else
            {
                HandleMouseEvent ();
                _timer_r.Start ();
            }
        }

        // Returns true when the middle button is in mouse range - faster than HitTest for now.
        protected abstract bool MiddleButtonInMouseRange(int x, int y);

        private void HandleMouseEvent() // bridge
        {
            // The handling stops when the mouse leaves ClientRectangle or _active_area changes -
            // at least this is how the "Visual C# 2008 Express Edition" scrollbars act.
            if ((MouseState.Down & _mouse_state) == MouseState.Down)
            {
                if (!this.ClientRectangle.Contains (_mx, _my) ||
                    _active_area != HitTest (new MouseEventArgs (MouseButtons.None, 0, _mx, _my, 0))) //TODO simplify
                    return;
            }

            switch (_active_area)
            {
                case HitTestArea.Min: _s.ScrollMinSmall (); this.Refresh (); break;
                case HitTestArea.NB1:
                    { // the middle button is moving and if it becomes under the mouse pointer, the timer stops
                        if (MiddleButtonInMouseRange (_mx, _my)) _timer_r.Stop ();
                        else { _s.ScrollMinLarge (); this.Refresh (); }
                    } break;
                case HitTestArea.NB2:// _s.ScrollMaxLarge (); this.Refresh (); break;
                    {
                        if (MiddleButtonInMouseRange (_mx, _my)) _timer_r.Stop ();
                        else { _s.ScrollMaxLarge (); this.Refresh (); }
                    } break;
                case HitTestArea.Max: _s.ScrollMaxSmall (); this.Refresh (); break;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (HitTestArea.Mid == _active_area)
                EndDrag (e.X, e.Y);
            _mouse_state = (_mouse_state & MouseState.Move) | MouseState.Up;
            _active_area = HitTestArea.CleanFreshAir;
            _timer_r.Stop ();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _mx = e.X;
            _my = e.Y;
            if (HitTestArea.Mid == _active_area && (MouseState.Down & _mouse_state) == MouseState.Down)
            {
                var d = Drag (e.X, e.Y);
                if (0 != d)
                {
                    _s.ScrollDistinctPixels (d);
                    this.Refresh ();
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            // this will work in all cases because the delta ain't coming from X or Y
            if (0 == e.Delta) return;
            int d = 1; //TODO setting/style, etc. scroll speed or something
            // d = this.Height / 10;// faster
            if (e.Delta > 0) d = -d; //TODO setting/style, etc. - invert mouse wheel direction
            _s.ScrollDistinctPixels (d);
            this.Refresh ();
        }

        static bool _timer_c_the_way_is_shut = false; // block recursion
        protected void TimerHandler_Color(object sender, EventArgs e)
        {
            if (_timer_c_the_way_is_shut) return;
            _timer_c_the_way_is_shut = true;
            try
            {
                _timer_c.Stop ();
            }
            finally { _timer_c_the_way_is_shut = false; }
        }
    }// public abstract class WScrollBar

    public class WVerticalScrollBar : WScrollBar
    {
        // The ".net" knows better.
        protected override Size DefaultSize { get { return new System.Windows.Forms.VScrollBar ().Size; } }

        // Imagine your surprise when you see how difficult is to render a scrollbar:
        protected override void Render(Graphics gc)
        {
            gc.FillRectangle (WStyle.FillBack, 0, 0, _s.Size, _s.Length);// background
            int middle_button_offset = 0; // offset by the min button
            if (_s.MinMaxVisible)
            {//TODONT optimize /\ this will go to WScrollBar, and it will use pre-computed Paths, Styles, etc. - not important now
                //             \/
                int a0 = _s.Size / 4, a1 = 3 * a0, a2 = 2 * a0; // h/v lines inside _s.Size : f(t) = 1/4, f(t) = 3/4, f(t) = 1/2
                Point[] up = new Point[] { new Point (a0, a1), new Point (a2, a0), new Point (a1, a1) }; // arrow up - ^
                {// top button
                    ControlPaint.DrawButton (gc, 0, 0, _s.Size, _s.Size, ButtonState.Flat);
                    gc.DrawLines (WStyle.FontPen, up); //TODO this throws, when "up = {{0,0},{0,0},{0,0}}"
                }
                {// bottom button
                    gc.TranslateTransform (0, _s.Length - _s.Size);
                    try
                    {
                        ControlPaint.DrawButton (gc, 0, 0, _s.Size, _s.Size, ButtonState.Flat);
                        gc.TranslateTransform (a2, a2);   // nothing scary here ; each "transform" pushes a matrix into a "stack";
                        gc.RotateTransform (180);         // each point gets multiplied to the matrices using the LIFO order;
                        gc.TranslateTransform (-a2, -a2); // see "Open GL" for more info;
                        gc.DrawLines (WStyle.FontPen, up);
                    }
                    finally
                    {
                        gc.ResetTransform ();
                    }
                }
                middle_button_offset = _s.Size;
            }
            if (MiddleVisible && _s.Size > 0 && _s.MidSize > 0)
                ControlPaint.DrawButton (gc, 0, middle_button_offset + _s.MidOffset, _s.Size, _s.MidSize, ButtonState.Flat); // mid button
        }// Render()

        protected override void OnResize(EventArgs e)
        {
            _s.Length = WControl.SanitizeSize (this.Height);
            _s.Size = WControl.SanitizeSize (this.Width);
            base.OnResize (e);
        }

        // When the middle button is not visible page scroll is based on top or bottom NB area.
        protected override sealed HitTestArea HitTest(MouseEventArgs e)
        {
            // no need to create 5 "Rectangle"s
            if (!this.ClientRectangle.Contains (e.X, e.Y)) return HitTestArea.CleanFreshAir;
            else if (e.Y < _s.Size) return HitTestArea.Min;
            else if (e.Y < _s.Size + (MiddleVisible ? _s.MidOffset : _s.DisctinctPixels / 2)) return HitTestArea.NB1;
            else if (e.Y < _s.Size + _s.MidOffset + _s.MidSize) return HitTestArea.Mid;
            else if (e.Y < this.Height - _s.Size) return HitTestArea.NB2;
            else return HitTestArea.Max;
        }

        int _mx = 0, _my = 0;
        protected override sealed void BeginDrag(int x, int y) { _mx = x; _my = y; }
        protected override sealed int Drag(int x, int y) { int r = y - _my; _mx = x; _my = y; return r; }
        protected override sealed void EndDrag(int x, int y) { _mx = _my = 0; }

        protected override sealed bool MiddleButtonInMouseRange(int x, int y)
        {
            var a = _s.Size + _s.MidOffset;
            var b = a + _s.MidSize;
            return y >= a && y <= b;
        }
    }// public class WVerticalScrollBar
    public class WHorizontalScrollBar : WScrollBar
    {
        protected override Size DefaultSize { get { return new System.Windows.Forms.HScrollBar ().Size; } }
        protected override void Render(Graphics gc)
        {// just swap w with h, and x with y ; TODO to WScrollBar
            gc.FillRectangle (WStyle.FillBack, 0, 0, _s.Length, _s.Size);// background
            int middle_button_offset = 0; // offset by the min button
            if (_s.MinMaxVisible)
            {//TODONT optimize /\ this will go to WScrollBar, and it will use pre-computed Paths, Styles, etc. - not important now
               //              \/
                int a0 = _s.Size / 4, a1 = 3 * a0, a2 = 2 * a0; // h/v lines inside _s.Size : f(t) = 1/4, f(t) = 3/4, f(t) = 1/2
                Point[] up = new Point[] { new Point (a0, a1), new Point (a2, a0), new Point (a1, a1) }; // arrow up - ^
                {// left button
                    gc.TranslateTransform (a2 + (_s.Size % 2), a2 + (_s.Size % 2));
                    gc.RotateTransform (-90);
                    gc.TranslateTransform (-a2, -a2);
                    ControlPaint.DrawButton (gc, 0, 0, _s.Size, _s.Size, ButtonState.Flat);
                    gc.DrawLines (WStyle.FontPen, up); //TODO this throws, when "up = {{0,0},{0,0},{0,0}}"
                }
                {// right button
                    gc.ResetTransform ();
                    gc.TranslateTransform (_s.Length - _s.Size, 0);
                    try
                    {
                        ControlPaint.DrawButton (gc, 0, 0, _s.Size, _s.Size, ButtonState.Flat);
                        gc.TranslateTransform (a2, a2);   // nothing scary here ; each "transform" pushes a matrix into a "stack"
                        gc.RotateTransform (90);          // each point gets multiplied to the matrices using the LIFO order
                        gc.TranslateTransform (-a2, -a2); // see "Open GL" for more info
                        gc.DrawLines (WStyle.FontPen, up);
                    }
                    finally
                    {
                        gc.ResetTransform ();
                    }
                }
                middle_button_offset = _s.Size;
            }
            if (MiddleVisible)
                ControlPaint.DrawButton (gc, middle_button_offset + _s.MidOffset, 0, _s.MidSize, _s.Size, ButtonState.Flat); // mid button
        }// Render()
        protected override void OnResize(EventArgs e)
        {
            _s.Length = WControl.SanitizeSize (this.Width); // hit some .NET bug here: this.Width < 0; (".NET bug #1" @ Process.txt)
            _s.Size = WControl.SanitizeSize (this.Height); // checking Height as well - just in case
            base.OnResize (e);
        }
        // When the middle button is not visible page scroll is based on top or bottom NB area.
        protected override sealed HitTestArea HitTest(MouseEventArgs e)
        {
            // no need to create 5 "Rectangle"s
            if (!this.ClientRectangle.Contains (e.X, e.Y)) return HitTestArea.CleanFreshAir;
            else if (e.X < _s.Size) return HitTestArea.Min;
            else if (e.X < _s.Size + (MiddleVisible ? _s.MidOffset : _s.DisctinctPixels / 2)) return HitTestArea.NB1;
            else if (e.X < _s.Size + _s.MidOffset + _s.MidSize) return HitTestArea.Mid;
            else if (e.X < this.Width - _s.Size) return HitTestArea.NB2;
            else return HitTestArea.Max;
        }
        int _mx = 0, _my = 0;
        protected override sealed void BeginDrag(int x, int y) { _mx = x; _my = y; }
        protected override sealed int Drag(int x, int y) { int r = x - _mx; _mx = x; _my = y; return r; }
        protected override sealed void EndDrag(int x, int y) { _mx = _my = 0; }
        protected override sealed bool MiddleButtonInMouseRange(int x, int y)
        {
            var a = _s.Size + _s.MidOffset;
            var b = a + _s.MidSize;
            return x >= a && x <= b;
        }
    }//public class WHorizontalScrollBar
}
