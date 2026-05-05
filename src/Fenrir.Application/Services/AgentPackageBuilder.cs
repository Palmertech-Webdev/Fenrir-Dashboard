using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.Extensions.Hosting;

namespace Fenrir.Application.Services;

public sealed class AgentPackageBuilder : IAgentPackageBuilder
{
    private readonly string _repoRoot;

    public AgentPackageBuilder(IHostEnvironment environment)
    {
        var contentRoot = environment.ContentRootPath;
        var candidateRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", ".."));
        _repoRoot = Directory.Exists(Path.Combine(candidateRoot, "agent")) ? candidateRoot : contentRoot;
    }

    public async Task<AgentPackageBuildResult> BuildPackageAsync(AgentBuildRequest request, CancellationToken cancellationToken)
    {
        var apiBaseUrl = NormalizeApiBaseUrl(request.ServerUrl);
        var sourceName = string.IsNullOrWhiteSpace(request.SourceName)
            ? NormalizeSourceName(request.CompanyName)
            : request.SourceName.Trim();

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            sourceName = "FenrirAgent";
        }

        var publishPath = await EnsurePublishDirectoryAsync(cancellationToken);
        var packageName = $"FenrirAgent-{SanitizeFileName(sourceName)}.zip";

        await using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddDirectoryToArchive(archive, publishPath, publishPath);
            AddTextEntry(archive, "appsettings.json", JsonSerializer.Serialize(new
            {
                ApiBaseUrl = apiBaseUrl,
                SourceName = sourceName,
                RegisterSource = true,
                CaptureProcesses = true,
                CaptureNetworkConnections = true
            }, new JsonSerializerOptions { WriteIndented = true }));
            AddTextEntry(archive, "run-agent.cmd", BuildWindowsRunScript(apiBaseUrl, sourceName));
            AddTextEntry(archive, "run-agent.sh", BuildUnixRunScript(apiBaseUrl, sourceName));
            AddTextEntry(archive, "README.txt", BuildReadme(apiBaseUrl, sourceName));
        }

        memoryStream.Position = 0;
        return new AgentPackageBuildResult(packageName, memoryStream.ToArray());
    }

    private static string NormalizeApiBaseUrl(string serverUrl)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            throw new InvalidOperationException("Server API URL or IP address is required.");
        }

        var trimmed = serverUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (!Uri.TryCreate("http://" + trimmed, UriKind.Absolute, out uri))
            {
                throw new InvalidOperationException("Server API URL must be an absolute URL or IP address.");
            }
        }

        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            throw new InvalidOperationException("Server API URL must use http or https.");
        }

        return uri.ToString().TrimEnd('/');
    }

    private static string NormalizeSourceName(string companyName)
    {
        var trimmed = string.IsNullOrWhiteSpace(companyName) ? string.Empty : companyName.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "FenrirAgent" : trimmed + "-FenrirAgent";
    }

    private static string SanitizeFileName(string value)
    {
        var builder = new StringBuilder();
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_')
            {
                builder.Append(ch);
            }
            else if (char.IsWhiteSpace(ch))
            {
                builder.Append('_');
            }
        }

        return string.IsNullOrWhiteSpace(builder.ToString()) ? "FenrirAgent" : builder.ToString();
    }

    private async Task<string> EnsurePublishDirectoryAsync(CancellationToken cancellationToken)
    {
        var existing = FindPublishDirectory();
        if (existing is not null)
        {
            return existing;
        }

        await PublishAgentAsync(cancellationToken);

        existing = FindPublishDirectory();
        return existing ?? throw new InvalidOperationException("Unable to find agent publish output after build.");
    }

    private string? FindPublishDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(_repoRoot, "agent", "bin", "Release", "net10.0", "publish"),
            Path.Combine(_repoRoot, "agent", "bin", "Debug", "net10.0", "publish"),
            Path.Combine(_repoRoot, "agent", "publish")
        };

        return candidates.FirstOrDefault(Directory.Exists);
    }

    private async Task PublishAgentAsync(CancellationToken cancellationToken)
    {
        var publishDirectory = Path.Combine(_repoRoot, "agent", "publish");
        var projectPath = Path.Combine(_repoRoot, "agent", "Fenrir.Agent.csproj");
        Directory.CreateDirectory(publishDirectory);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("dotnet", $"publish \"{projectPath}\" -c Release -o \"{publishDirectory}\"")
            {
                WorkingDirectory = _repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet publish failed with exit code {process.ExitCode}: {stderr.Trim()}");
        }
    }

    private static void AddDirectoryToArchive(ZipArchive archive, string rootPath, string directory)
    {
        foreach (var filePath in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
        {
            var entryName = Path.GetRelativePath(rootPath, filePath).Replace(Path.DirectorySeparatorChar, '/');
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(filePath);
            fileStream.CopyTo(entryStream);
        }
    }

    private static void AddTextEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, Encoding.UTF8);
        writer.Write(content);
    }

    private static string BuildWindowsRunScript(string apiBaseUrl, string sourceName) =>
$"@echo off\r\nset DOTNET_ROOT=%~dp0\r\ndotnet Fenrir.Agent.dll --api \"{apiBaseUrl}\" --source \"{sourceName}\"\r\npause\r\n";

    private static string BuildUnixRunScript(string apiBaseUrl, string sourceName) =>
$"#!/usr/bin/env bash\ncd \"$(dirname \"$0\")\"\ndotnet Fenrir.Agent.dll --api \"{apiBaseUrl}\" --source \"{sourceName}\"\n";

    private static string BuildReadme(string apiBaseUrl, string sourceName)
    {
        return $"Fenrir Agent Package\n===================\n\nThis archive contains a configured Fenrir agent that points to the server at {apiBaseUrl}.\n\nRun the agent with:\n\nWindows:\n  run-agent.cmd\n\nLinux/macOS:\n  chmod +x run-agent.sh && ./run-agent.sh\n\nThe agent will use source name: {sourceName}\n\nIf you need to override the server or source, run the executable directly:\n  dotnet Fenrir.Agent.dll --api \"{apiBaseUrl}\" --source \"{sourceName}\"\n";
    }
}
