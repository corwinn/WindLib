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

using System.Drawing;

namespace Wind.Controls
{
    // Instead of adding 10 methods to the model.
    /// <summary>A single cell - an intersection of a row and a column.</summary>
    public interface IWTableViewCell
    {
        /// <summary>The Horizontal Range this cell is associated with.</summary>
        WTableViewRange HRange { get; }

        /// <summary>The Vertical Range this cell is associated with.</summary>
        WTableViewRange VRange { get; }

        bool Selected { get; set; } //TODO selecting each odd byte of a 193TB file could be complicated

        /// <summary>Indicate that your cell has to be Measured and Rendered.</summary>
        bool Changed { get; set; }

        /// <summary>Return the preferred size of your cell. Update its H/R range(s).</summary>
        Size Measure(WGraphicsContext gc);

        // Should you decide to compute Mersenne primes here, please do no complain about "frozen UI" :).
        // This function should measure 1000/FPS/visible_cells [milliseconds] at least; compute with MA(3600 seconds) at least.
        /// <summary>Render whatever you like.</summary>
        void Render(WGraphicsContext gc);

        // Used for edit mode: whether this is a newly created (by the view) cell, or not.
        // When true:
        //  - your HRange is known and VRange is not - a new column was added/inserted
        //  - your VRange is known and HRange is not - a new row was added/inserted
        // When false: TODO CellValueChanged - see the editor below - not important right now - complete the read-only 1st
        bool New { get; set; } //TODO perhaps this is wrong - perhaps "new" and "set" shall be model-only responsibility?
        // object Data { get; set; } //TODO editor - the editor shall know what to update where, etc.
    }
}
