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

using System.Collections.Generic;

namespace Wind.Controls
{
    public interface IWTableViewModel
    {
        // The requests below shall follow this state:
        //  - stream = column_headers_stream
        //  - stream = data_stream
        //  ...
        // Since the vertical ranges (columns) are shared across column data, header and or footer:
        // set their horizontal range: .Header = true and .Header*Alignment to whatever you need.
        // Same for row headers: just use a distinct vertical range.
        IWTableViewModel ColumnHeaderStream(); // Please "return this;".
        IWTableViewModel DataStream();         // Please "return this;".
        //TODO Idea: ITableViewModel ColumnHeaderModel;
        //           ITableViewModel DataModel;

        IWTableViewCell Current { get; } // Get the cell at the current rc (row,column) pointer: cells[rc pointer]
        //TODO IEnumerable<ITableViewCell> Current { get; } - is it a good idea?

        // Relative move of the rc (row,column) pointer; return true when the move actually happened; false - when it didn't.
        bool Move(int column_offset, int row_offset); // a.k.a. "Stream.Seek(offset, SEEK_SET)"

        //void Push(); // LIFO store/restore the rc pointer
        //void Pop();  //TODONT will slightly simplify the tableview code, and reduce its robustness

        void Set(IEnumerable<IWTableViewCell> cells);  //TODO stream.write ; when the user adds a new row or column or updates cell data; or a model-side-only-thing?

        // Will be called if the user is using goto 1st/last row
        // and your model allows it; also, used by the vertical scroll-bar.
        // Unknown size: the scrollbars will lack their middle button; also, home/end can't function.
        /// <summary>Vertical size in [cells]. 0 = unknown.</summary>
        long SizeVertical { get; } // WTableViewScrollModel contract; not called when you're using your own scroll models
        // Will be called if the user is using goto 1st/last column
        // and your model allows it; also, used by the horizontal scroll-bar.
        // Unknown size: the scrollbars will lack their middle button; also, home/end can't function.
        /// <summary>Horizontal size in [cells]. 0 = unknown.</summary>
        long SizeHorizontal { get; } // WTableViewScrollModel contract; not called when you're using your own scroll models
    }// public interface IWTableViewModel
}
