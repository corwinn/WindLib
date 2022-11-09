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
using System.Text;
using System.Diagnostics;

namespace Wind
{
    // Not for the faint of heart.
    public class WDebug
    {
        internal static string GetCaller(int stack_frames_to_skip)
        {
            string result = "";
            StackTrace s = new StackTrace ();
            var frame = stack_frames_to_skip > 0 && s.FrameCount > stack_frames_to_skip ? s.GetFrame (stack_frames_to_skip) : null;
            if (null != frame)
            {
                var caller = frame.GetMethod ();
                result += "at " + (null != caller ?
                    (null != caller.ReflectedType ? caller.ReflectedType.ToString () : "unknown_type")
                    + "." + caller.Name : "unknown_caller");
            }
            return result;
        }

        // replace and enhance "if (...) throw new ArgumentException(...)" with "NDebug.Throw_ArgumentException"
        /*public static void Throw_ArgumentException(int stack_frames_to_skip, string message)
        {
            throw new ArgumentException (message + " " + GetCaller (stack_frames_to_skip + 1));
        }

        public static void Throw_ArgumentException(string message) { Throw_ArgumentException (1, message); }

        public static void Throw(Exception e)
        {
            var new_msg = GetCaller (2) + ": " + e.Message;
            throw (Exception)Activator.CreateInstance (e.GetType (), new object[] { new_msg });
        }*/

        // System.Diagnostics.Debug ain't available @ release builds
        public static void Assert(bool I_shall_be_true, string message_if_false)
        {
            if (true != I_shall_be_true)
                throw new Exception ("Assertion failed: " + GetCaller (2) + ": " + message_if_false);
        }

        // The best debugger for file format parsing.
        public static string PrintBytes(byte[] bytes, int at_most = 1<<4)
        {
            StringBuilder result = new StringBuilder ();
            result.Append (bytes[0].ToString ("X2"));
            for (int i = 1; i < at_most; i++)
                result.Append (i < bytes.Length ? " " + bytes[i].ToString ("X2") : "   ");
            result.Append ("| ");
            for (int i = 0; i < at_most; i++)
                result.Append (i < bytes.Length ? ((bytes[i] > 32 && bytes[i] < 127) ? (char)bytes[i] : ' ') : ' ');
            result.Append ("|");
            return result.ToString ();
        }
    }// class WDebug

    public class WException : Exception
    {
        public WException(string message) : base (WDebug.GetCaller (2) + ": " + message) { }
        public WException(string message, Exception innerException) : base (WDebug.GetCaller (2) + ": " + message, innerException) { }
    }
    public class WArgumentException : WException
    {
        public WArgumentException(string message) : base (message) { }
        public WArgumentException(string message, Exception innerException) : base (message, innerException) { }
    }
}
