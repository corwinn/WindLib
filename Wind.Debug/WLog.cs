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
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
//LATER just use "log4net"
namespace Wind.Log
{
    /// <summary>Double-FIFO multi-threaded logger; default timeout/overflow policy: ask the user.</summary>
    public sealed class WLog
    {
        public static Form ProgramMainForm { get; set; }
        // Remember: the thread here is not the UI one.
        public interface ILogListener { void Log(LogEntry entry); }

        public class LogIssuePolicyException : Exception { public bool SwitchLogOff { get; set; } }

        // Throw WLog.LogIssuePolicyException if the thread should cancel - when SwitchLogOff is true, the log will be permanently off;
        // don't throw anything if you want your thread to re-try logging the message.
        public interface ILogIssuePolicy
        {
            void Timeout();  // after Timeout() your thread will wait again;
            void Overflow(); // if you don't resolve the overflow the log will be switched off
        }
        // When logging to file, or to a network stream, or anywhere, there could be an issue like disk full, write - slow, etc.
        // While the double-FIFO handles most of these, it has limits - this thing lets you handle such exceptional situations
        // via a user interface dialog.
        class DefaultLogIssuePolicy : WLog.ILogIssuePolicy
        {
            delegate DialogResult AskTheUser();
            public void Timeout()
            {
                DialogResult d = DialogResult.Yes;
                if (null != ProgramMainForm)
                    d = (DialogResult)ProgramMainForm.Invoke ((AskTheUser)(() =>
                    {
                        return MessageBox.Show (text: "There is a log issue. Retry (Yes/No) or Cancel?", caption: "Log Issue",
                            buttons: MessageBoxButtons.YesNoCancel, icon: MessageBoxIcon.Error);
                    }));
                else
                    d = MessageBox.Show (text: "There is a log issue. Retry (Yes/No) or Cancel?", caption: "Log Issue",
                        buttons: MessageBoxButtons.YesNoCancel, icon: MessageBoxIcon.Error);
                if (DialogResult.Cancel == d)
                    throw new WLog.LogIssuePolicyException () { SwitchLogOff = true };
                if (DialogResult.No == d)
                    throw new WLog.LogIssuePolicyException () { SwitchLogOff = false };
            }
            public void Overflow() { Timeout (); }
        }
        public class LogEntry
        {
            string _m;
            public LogEntry(string message) { _m = message; }
            public static implicit operator LogEntry(string message) { return new LogEntry (message); }
            public override string ToString() { return _m; }
            public string ThreadName { get; set; }
        }
        sealed class Log
        {
            const int MAX_PUT_THREADS = 3;
            const int WAIT_TIMEOUT = 10; // [msec]
            Mutex _put_lock = new Mutex (initiallyOwned: false);
            Mutex _state_lock = new Mutex (initiallyOwned: false); // because state can change while the next thread waits
            Queue<LogEntry> _pq = new Queue<LogEntry> ();
            Queue<LogEntry> _gq = new Queue<LogEntry> ();
            const int MAX_ENTRIES = 1 << 15;
            WLog.ILogIssuePolicy _log_issue_policy;
            bool _off = false;

            public Log()
            {
                _log_issue_policy = new WLog.DefaultLogIssuePolicy ();
            }

            // What to do on an issue.
            public WLog.ILogIssuePolicy LogIssuePolicy { get { return _log_issue_policy; } set { _log_issue_policy = value ?? _log_issue_policy; } }

            internal void Put(LogEntry message)
            {
                _state_lock.WaitOne (); // one thread at a time, or it becomes pretty complicated <=> there is "log4net" already
                try
                {
                    if (_off) return;
                    message.ThreadName = Thread.CurrentThread.Name;
                    if (string.IsNullOrEmpty (message.ThreadName)) message.ThreadName = Thread.CurrentThread.ManagedThreadId.ToString ();
                    try
                    {
                        while (!_put_lock.WaitOne (millisecondsTimeout: WAIT_TIMEOUT))
                            _log_issue_policy.Timeout (); // could throw WLog.LogIssuePolicyException
                        try
                        {
                            if (_pq.Count >= MAX_ENTRIES) _log_issue_policy.Overflow (); // could throw WLog.LogIssuePolicyException
                            if (_pq.Count < MAX_ENTRIES) _pq.Enqueue (message);
                            else _off = true; // if there was an overflow and it wasn't resolved
                        }
                        finally { _put_lock.ReleaseMutex (); }
                    }
                    catch (WLog.LogIssuePolicyException e) { if (e.SwitchLogOff) _off = true; }
                }
                finally { _state_lock.ReleaseMutex (); }
            }// internal void Put(LogEntry message)
            internal IEnumerable<LogEntry> Get() //TODO check if IEnumerable + Clear() is faster than Dequeue()
            {
                _put_lock.WaitOne ();
                try
                {
                    if (_pq.Count > 0)
                    {
                        var tmp = _gq;
                        _gq = _pq;
                        _pq = tmp;
                    }
                }
                finally { _put_lock.ReleaseMutex (); }
                return _gq;
            }
            internal void Clear()
            {
                _gq.Clear ();
            }
        }//class Log

        static WLog.Log _info = new WLog.Log ();//TODO is there a point to creating distinct log objects
        public static void Info(string message) { _info.Put (message); }
        public static void Err(string message) { _info.Put ("Error: " + message); }
        public static void Warn(string message) { _info.Put ("Warn: " + message); }

        static bool _running;
        static Semaphore _stopped = new Semaphore (initialCount: 0, maximumCount: 1);
        static Mutex _listeners_lock = new Mutex (initiallyOwned: false);

        static void LogThread()
        {
            try
            {
                while (_running)
                {
                    foreach (var log in _logs)
                    {
                        if (!_running) break;
                        _listeners_lock.WaitOne (); // Unsubscribe() should flush all messages related to foo prior unsubscribing it
                        try
                        {
                            foreach (var log_entry in log.Get ())
                            {
                                if (!_running) break;

                                foreach (var listener in _listeners)
                                {
                                    if (!_running) break;
                                    listener.Log (log_entry);
                                }
                            }
                        }
                        finally { _listeners_lock.ReleaseMutex (); }
                        if (_running) log.Clear ();
                    }
                    Thread.Sleep (1);
                }
                FlushAll ();
            }
            finally
            {
                _stopped.Release ();
            }
        }

        static void FlushAll()
        {
            _listeners_lock.WaitOne ();
            try
            {
                foreach (var log in _logs)
                {
                    foreach (var log_entry in log.Get ())
                        foreach (var listener in _listeners)
                            listener.Log (log_entry);
                    log.Clear ();
                }
            }
            finally { _listeners_lock.ReleaseMutex (); }
        }

        static Thread _log_thread = new Thread (LogThread);
        static List<Log> _logs = new List<Log> ();
        static WLog()
        {
            _log_thread.Name = "WLog._log_thread";
            _logs.Add (_info);
            Start ();
        }

        public static void Start() { if (_running) return; _running = true; _log_thread.Start (); }
        public static void Stop() { if (!_running) return; _running = false; _stopped.WaitOne (); }

        static List<WLog.ILogListener> _listeners = new List<ILogListener> ();
        public static void Subscribe(WLog.ILogListener listener)
        {
            _listeners_lock.WaitOne ();
            try { _listeners.Add (listener); }
            finally { _listeners_lock.ReleaseMutex (); }
        }
        public static void Unsubscribe(WLog.ILogListener listener)
        {
            _listeners_lock.WaitOne ();
            try
            {
                foreach (var log in _logs)
                    foreach (var log_entry in log.Get ())
                        listener.Log (log_entry);
                _listeners.Remove (listener);
            }
            finally { _listeners_lock.ReleaseMutex (); }
        }
    }// public class WLog
}
