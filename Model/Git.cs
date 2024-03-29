﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;

namespace ItalicPig.Bootstrap.Model
{
    public static class Git
    {
        public static string FindGitExecutable()
        {
            // Look for system Git
            var SystemPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe");
            if (File.Exists(SystemPath))
            {
                return SystemPath;
            }

            // Look for SourceTree's embedded Git
            var SourceTreePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Atlassian", "SourceTree", "git_local", "bin", "git.exe");
            if (File.Exists(SourceTreePath))
            {
                return SourceTreePath;
            }

            // Just hope it's on the path
            return "git";
        }

        public static bool ReadSparseCheckoutConfig(string repositoryPath)
        {
            var ConfigPath = Path.Combine(repositoryPath, ".git", "config.worktree");
            try
            {
                // This is fragile and lazy, I should parse the file properly, but I don't have time
                var ConfigLines = File.ReadAllLines(ConfigPath);
                return ConfigLines.Any(l => l.Trim().Replace(" ", "") == "sparseCheckout=true");
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static string[] ReadSparseCheckoutPaths(string repositoryPath)
        {
            var ConfigPath = Path.Combine(repositoryPath, ".git", "info", "sparse-checkout");
            try
            {
                return File.ReadAllLines(ConfigPath);
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
        }

        public static Task<CommandResult> ShowVersionAsync(Action<string>? outputCallback = null)
        {
            return RunCommandAsync(Environment.CurrentDirectory, "version", outputCallback);
        }

        public static Task<CommandResult> CloneAsync(string repositoryPath, string url, Action<string>? outputCallback = null)
        {
            var ParentPath = Path.GetDirectoryName(repositoryPath) ?? "";
            var RepositoryName = Path.GetFileName(repositoryPath);
            return RunCommandAsync(ParentPath, $"clone --filter=blob:none --sparse {url} {RepositoryName}", outputCallback);
        }

        public static Task<CommandResult> InstallLfsAsync(string repositoryPath, Action<string>? outputCallback = null)
        {
            return RunCommandAsync(repositoryPath, "lfs install", outputCallback);
        }

        public static Task<CommandResult> SetConfigAsync(string repositoryPath, string setting, string value, GitConfigScope scope = GitConfigScope.Local, Action<string>? outputCallback = null)
        {
            return RunCommandAsync(repositoryPath, $"config --{scope.ToString().ToLowerInvariant()} {setting} {value}", outputCallback);
        }

        public static Task<CommandResult> EnableSparseCheckoutAsync(string repositoryPath, IEnumerable<string> sparseCheckoutPaths, Action<string>? outputCallback = null)
        {
            return RunCommandAsync(repositoryPath, $"sparse-checkout set {string.Join(" ", sparseCheckoutPaths)} --cone", outputCallback);
        }

        public static Task<CommandResult> DisableSparseCheckoutAsync(string repositoryPath, Action<string>? outputCallback = null)
        {
            return RunCommandAsync(repositoryPath, "sparse-checkout disable", outputCallback);
        }

        private static Task<CommandResult> RunCommandAsync(string workingDirectory, string arguments, Action<string>? outputCallback)
        {
            return Cli.Wrap(FindGitExecutable())
                .WithArguments(arguments)
                .WithWorkingDirectory(workingDirectory)
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe((outputCallback == null) ? PipeTarget.Null : PipeTarget.ToDelegate(outputCallback))
                .WithStandardErrorPipe((outputCallback == null) ? PipeTarget.Null : PipeTarget.ToDelegate(outputCallback))
                .ExecuteAsync();
        }
    }
}
