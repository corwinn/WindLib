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

namespace Wind.Controls
{
    /// <summary>The WTableView default scroll model: IWScrollerModel&lt;long&gt;.</summary>
    public class WTableViewScrollModel : IWScrollerModel
    {
        private long _small_size; // visible area size
        private long _large_size; // virtual area size
        private long _small_position; // visible area position in the virtual area
        bool _the_way_is_shut = false; // event driven sanitizer
        public WTableViewScrollModel(int small = 0, long large = 0) { this.SmallSize = small; this.LargeSize = large; }
        delegate void Validator(long value); // throws ArgumentException if validation fails
        private void ValidateSetAndNotify(ref long field, long value, Validator validate, ref WScrollerEventHandler notify)
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
            get { return (int)_small_size; }
            set { ValidateSetAndNotify (ref _small_size, value, (x) => { x.PreventStrangeRendering (); }, ref SizeChanged); }
        }
        public long LargeSize
        {
            get { return _large_size; }
            set { ValidateSetAndNotify (ref _large_size, value, (x) => { x.PositiveOrZero (); }, ref SizeChanged); }
        }
        // Small.Position (in Large space)
        public long SmallPosition //TODO this was not intuitive after a month; VirtualPosition or LargeOffset would be better
        {
            get { return _small_position; }
            set { ValidateSetAndNotify (ref _small_position, value, (x) => { x.InExclusiveRange (-1, _large_size); }, ref Scroll); }
        }
        #region INScrollerModel implementation
        public virtual int ComputeMidOffset(WScroller scroller, int local)
        {
            return _small_position < 0 ? -1 : WMath.LinearProjection (value: _small_position, from: _large_size, to: local);
            //(_large_size > 0 ? (int)Math.Round (local * _small_position / (double)_large_size) : 0);
        }
        public virtual int ComputeMidSize(WScroller scroller, int local)
        {
            // nothing to scroll - nothing to render; this handles both 0 0 and 100 100 cases; AutoHide is no longer an option
            if (SmallSize >= LargeSize) return 0;
            var tmp = WMath.LinearProjection (value: _small_size, from: _large_size, to: local);
            //var tmp = (int)Math.Round (local * _small_size / (double)_large_size);
            return tmp < 1 ? 1 : tmp;
        }
        public virtual void ScrollMinSmall(WScroller scroller)
        {
            if (_small_position > 0) { _small_position--; Scroll (this, null); }
            else if (0 == LargeSize) Scroll (this, new WScrollerEventArgs (WScrollerEvent.Min | WScrollerEvent.Small));
        }
        protected virtual int LastVisiblePage { get { return (int)(_large_size > _small_size ? _large_size - _small_size : 0); } }
        public virtual void ScrollMaxSmall(WScroller scroller)
        {
            if (_small_position >= 0 && _small_position < LastVisiblePage) { _small_position++; Scroll (this, null); }
            else if (0 == LargeSize) Scroll (this, new WScrollerEventArgs (WScrollerEvent.Max | WScrollerEvent.Small));
        }
        public virtual void ScrollMinLarge(WScroller scroller)
        {
            if (0 == LargeSize) Scroll (this, new WScrollerEventArgs (WScrollerEvent.Min | WScrollerEvent.Large));
            else
            {
                var sentinel = _small_position;
                if ((_small_position -= _small_size) < 0) _small_position = 0; // snap to 1st visible page start offset
                if (sentinel != _small_position) Scroll (this, null);
            }
        }
        public virtual void ScrollMaxLarge(WScroller scroller)
        {
            if (0 == LargeSize) Scroll (this, new WScrollerEventArgs (WScrollerEvent.Max | WScrollerEvent.Large));
            else
            {
                var sentinel = _small_position;
                if ((_small_position += _small_size) > LastVisiblePage) _small_position = LastVisiblePage; // snap to last visible page start offset
                if (sentinel != _small_position) Scroll (this, null);
            }
        }
        public virtual int ScrollDistinctPixels(WScroller scroller, int local, int d)
        {
            if (LargeSize <= 0)
            {
                Scroll (this, new WScrollerEventArgs (WScrollerEvent.Distinct, d));
                return scroller.MidOffset; // nothing to scroll
            }

            var sentinel = _small_position;
            _small_position = WMath.LinearProjection (value: scroller.MidOffset, from: local, to: _large_size);
            if (sentinel != _small_position) Scroll (this, null);
            //_small_position = local > 0 ? (int)Math.Round (_large_size * scroller.MidOffset / (double)local) : 0;
            return ComputeMidOffset (scroller, local);
        }
        public event WScrollerEventHandler SizeChanged = new WScrollerEventHandler ((a, b) => { });
        public event WScrollerEventHandler Scroll = new WScrollerEventHandler ((a, b) => { });
        #endregion
    }// class WTableViewScrollModel
}
