﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;

namespace SpecFlow.Tools.MsBuild.Generation
{
    class OutOfProcessServer : IDisposable
    {
        private readonly TimeSpan _timeout;
        private readonly int _timeOutInMilliseconds;

        private TaskLoggingHelper Log { get; }

        private StringBuilder _output;
        private AutoResetEvent _outputWaitHandle;
        private AutoResetEvent _errorWaitHandle;
        private AutoResetEvent _usedPortReceived;
        private Process _externalProcess;
        private readonly string _workingDirectory;
        private short _usedPort;
        private const string GeneratorExe = "TechTalk.SpecFlow.CodeBehindGenerator.exe";
        private const string GeneratorDll = "TechTalk.SpecFlow.CodeBehindGenerator.dll";

        public OutOfProcessServer(TaskLoggingHelper log)
        {
            Log = log;
            _timeout = TimeSpan.FromMinutes(10);
            _timeOutInMilliseconds = Convert.ToInt32(_timeout.TotalMilliseconds);
            _workingDirectory = Path.GetDirectoryName(GetType().Assembly.Location);

            _output = new StringBuilder();
            _outputWaitHandle = new AutoResetEvent(false);
            _errorWaitHandle = new AutoResetEvent(false);
            _usedPortReceived = new AutoResetEvent(false);
        }

        public void Start(int port)
        {
            var processStartInfo = GetProcessStartInfo(port);

            _externalProcess = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true,

            };
            _externalProcess.Exited += Process_Exited;
            _externalProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    _outputWaitHandle.Set();
                }
                else
                {
                    if (e.Data.Contains("Used port:"))
                    {
                        _usedPort = short.Parse(e.Data.Substring(e.Data.Length - 5).Trim());
                        _usedPortReceived.Set();
                    }


                    _output.AppendLine(e.Data);
                }
            };
            _externalProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    _errorWaitHandle.Set();
                }
                else
                {
                    _output.AppendLine(e.Data);
                }
            };


            try
            {
                _externalProcess.Start();
            }
            catch (Exception)
            {
                Log.LogWithNameTag(Log.LogError, $"Error starting {_externalProcess.StartInfo.FileName} {_externalProcess.StartInfo.Arguments} in {_externalProcess.StartInfo.WorkingDirectory}");
                throw;
            }
            _externalProcess.BeginOutputReadLine();
            _externalProcess.BeginErrorReadLine();

            Log.LogWithNameTag(Log.LogMessage, "OutOfProcess generation process started");

        }

#if NET471
        private ProcessStartInfo GetProcessStartInfo(int port)
        {
            var exe = Path.Combine(_workingDirectory, GeneratorExe);

            Log.LogWithNameTag(Log.LogMessage, $"Starting OutOfProcess generation process {exe} in {_workingDirectory}");
            var processStartInfo = new ProcessStartInfo(exe, $"--port {port}")
            {
                WorkingDirectory = _workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            return processStartInfo;
        }
#endif


#if NETCOREAPP2_0
        private ProcessStartInfo GetProcessStartInfo(int port)
        {
            var dll = Path.Combine(_workingDirectory, GeneratorDll);

            Log.LogWithNameTag(Log.LogMessage, $"Starting OutOfProcess generation process {dll} in {_workingDirectory}");
            var processStartInfo = new ProcessStartInfo("dotnet", $"{dll} --port {port}")
            {
                WorkingDirectory = _workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            return processStartInfo;
        }
#endif
        private void Process_Exited(object sender, EventArgs e)
        {
            Log.LogWithNameTag(Log.LogMessage, "Output:" + _output);
            Log.LogWithNameTag(Log.LogMessage, $"OutOfProcess process has exited with exit code {_externalProcess.ExitCode}");

            _externalProcess.Exited -= Process_Exited;
        }

        public short WaitForPort()
        {
            if (_usedPortReceived.WaitOne(_timeOutInMilliseconds))
            {
                return _usedPort;
            }

            throw new Exception("Could not find the used port of code generation process");
        }

        public void Dispose()
        {
            if (_externalProcess.WaitForExit(_timeOutInMilliseconds) &&
                _outputWaitHandle.WaitOne(_timeOutInMilliseconds) &&
                _errorWaitHandle.WaitOne(_timeOutInMilliseconds))
            {
                Log.LogWithNameTag(Log.LogMessage, "[SpecFlow]" + Environment.NewLine + _output);
            }
            else
            {
                Log.LogWithNameTag(Log.LogError, $"[SpecFlow]Process took longer than {_timeout.TotalMinutes} min to complete");
            }



            _outputWaitHandle.Dispose();
            _outputWaitHandle = null;
            _errorWaitHandle.Dispose();
            _errorWaitHandle = null;

            _output = null;

            if (_externalProcess == null)
            {
                return;
            }

            if (_externalProcess.HasExited)
            {
                return;
            }

            _externalProcess.Kill();

            _externalProcess.Dispose();
            _externalProcess = null;

        }

        public string CurrentOutput => _output.ToString();
    }
}
