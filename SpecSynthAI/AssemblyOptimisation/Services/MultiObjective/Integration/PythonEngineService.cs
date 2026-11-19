using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SpecSynthAI.AssemblyOptimisation.Services.MultiObjective.Integration
{
    public sealed class PythonEngineService
    {
        public async Task<PythonExecutionResult> ExecuteAsync(
            string pythonExecutable, string entryScriptPath, string requestJsonPath,
            string resultJsonPath, string workingDirectory, int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pythonExecutable)) throw new ArgumentException("Python executable is required.");
            if (!File.Exists(entryScriptPath)) throw new FileNotFoundException("Python entry script not found.", entryScriptPath);
            if (!File.Exists(requestJsonPath)) throw new FileNotFoundException("Request JSON not found.", requestJsonPath);
            if (!Directory.Exists(workingDirectory)) throw new DirectoryNotFoundException(workingDirectory);
            if (timeoutSeconds < 1) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

            var resultDir = Path.GetDirectoryName(resultJsonPath);
            if (!string.IsNullOrWhiteSpace(resultDir)) Directory.CreateDirectory(resultDir);
            if (File.Exists(resultJsonPath)) File.Delete(resultJsonPath);

            var info = new ProcessStartInfo
            {
                FileName = pythonExecutable, WorkingDirectory = workingDirectory,
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            info.ArgumentList.Add(entryScriptPath);
            info.ArgumentList.Add("--request");
            info.ArgumentList.Add(requestJsonPath);
            info.ArgumentList.Add("--result");
            info.ArgumentList.Add(resultJsonPath);

            using var process = new Process { StartInfo = info };
            if (!process.Start()) throw new InvalidOperationException("Could not start Python process.");

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            try { await process.WaitForExitAsync(linked.Token).ConfigureAwait(false); }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                if (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    throw new TimeoutException($"Python process exceeded {timeoutSeconds} seconds.");
                throw;
            }

            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Python failed with exit code {process.ExitCode}:{Environment.NewLine}{stderr}");
            if (!File.Exists(resultJsonPath))
                throw new FileNotFoundException("Python did not create the result JSON.", resultJsonPath);

            return new PythonExecutionResult(process.ExitCode, stdout, stderr, resultJsonPath);
        }
    }

    public sealed class PythonExecutionResult
    {
        public PythonExecutionResult(int exitCode, string standardOutput, string standardError, string resultPath)
        { ExitCode = exitCode; StandardOutput = standardOutput; StandardError = standardError; ResultPath = resultPath; }
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
        public string ResultPath { get; }
    }
}
