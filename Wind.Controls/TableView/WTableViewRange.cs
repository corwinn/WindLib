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

namespace Wind.Controls
{
    // All cells in a single row share one horizontal TableViewRange: row size (its height).
    // All cells in a single column share one vertical TableViewRange: column size (its width).
    // Each range defines (for all cells that are sharing it):
    //   - auto-size policy (if any)
    //   - in-cell alignment (columns: left, center, right; rows: top, center, bottom)
    // There are a few fixed (anchored at specific portions of the table) optional ranges:
    //   - header row (column headers)
    //   - footer row (could mirror column headers)
    //   - leftmost column (row headers)
    //   - rightmost column (could mirror row headers)
    /// <summary>A table row or column.</summary>
    public class WTableViewRange
    {
        private int _size = 0;
        public const int DEFULT_SIZE = 20;
        public const bool DEFULT_AUTO_SIZE = false;
        public const WTableViewSizePolicy DEFAULT_SIZE_POLICY = WTableViewSizePolicy.Both;
        public const WTableViewAlignment DEFAULT_ALIGNMENT = WTableViewAlignment.Center;
        public WTableViewRange()
        {
            SizeChanged += (d) => { };

            Size = WTableViewRange.DEFULT_SIZE;
            AutoSize = WTableViewRange.DEFULT_AUTO_SIZE;
            AutoSizePolicy = WTableViewRange.DEFAULT_SIZE_POLICY;
            Alignment = WTableViewRange.DEFAULT_ALIGNMENT;
        }
        private int _last_grew_size = 0;
        public int UpdateSize(int new_size)
        {
            if (AutoSize)
            {
                if (new_size < Size && (AutoSizePolicy & WTableViewSizePolicy.Shrink) == WTableViewSizePolicy.Shrink)
                    return Size = (new_size >= _last_grew_size ? new_size : _last_grew_size);
                else if (new_size > Size && (AutoSizePolicy & WTableViewSizePolicy.Grow) == WTableViewSizePolicy.Grow)
                    return Size = _last_grew_size = new_size;
            }
            return new_size;
        }
        public delegate void TableViewRangeEvent(int delta);
        public event TableViewRangeEvent SizeChanged;
        public int Size
        {
            get { return _size; }
            set { if (value != _size) { var delta = value - _size; _size = value; SizeChanged (delta); } }
        }
        public bool AutoSize { get; set; }
        public WTableViewSizePolicy AutoSizePolicy { get; set; }
        public WTableViewAlignment Alignment { get; set; }

        /// <summary>When set - signal you to sort your data. When get - render something to show the range is sorted.</summary>
        public bool Sorted { get; set; }

        /// <summary>Used when the built-in selection renderer is used and selection mode is row, column, or both.</summary>
        public bool Selected { get; set; }

        /// <summary>Indicates whether or not this is a header range.</summary>
        public bool Header { get; private set; }
        /// <summary>Header alignment might differ.</summary>
        WTableViewAlignment HeaderVerticalAlignment { get; set; }
        WTableViewAlignment HeaderHorizontalAlignment { get; set; }
    }// class WTableViewRange

    [Flags]
    public enum WTableViewSizePolicy { Both = Shrink | Grow, Shrink = 1, Grow = 2 };
    public enum WTableViewAlignment { Top, Left, Bottom, Right, Center };
}
