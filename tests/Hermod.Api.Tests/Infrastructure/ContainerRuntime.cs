using System.Diagnostics;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Hermod.Api.Tests.Infrastructure;

public class ContainerRuntime : IAsyncInitializer
{
    public Task InitializeAsync()
    {
        // If DOCKER_HOST is already set (e.g. by CI), trust it
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_HOST")))
            return Task.CompletedTask;

        // Try Podman first — set DOCKER_HOST so Testcontainers uses its API socket
        if (TryRun("podman", "info --format {{.Host.RemoteSocket.Path}}", out var podmanSocket)
            && !string.IsNullOrWhiteSpace(podmanSocket))
        {
            TryRun("podman", "system service --time=0", out _, waitForExit: false);
            Environment.SetEnvironmentVariable("DOCKER_HOST", $"unix://{podmanSocket.Trim()}");
            return Task.CompletedTask;
        }

        // Fall back to Docker — if this fails, let tests fail naturally
        if (TryRun("docker", "info", out _))
            return Task.CompletedTask;

        throw new InvalidOperationException(
            "No container runtime found. Install Podman or Docker to run integration tests.");
    }

    private static bool TryRun(string fileName, string arguments, out string output, bool waitForExit = true)
    {
        output = string.Empty;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            var process = Process.Start(psi);
            if (process is null) return false;

            if (waitForExit)
            {
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(10_000);
                return process.ExitCode == 0;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
