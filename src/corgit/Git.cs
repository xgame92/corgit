﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace corgit
{
    public partial class Git
    {
        internal const string CommitFormat = "%H\n%ae\n%P\n%B";
        internal const string CommitSeparator = "\x00\x00";

        private static readonly Regex r_parseVersion = new Regex(@"^git version ", RegexOptions.Compiled);
        public string ParseVersion(string versionString)
        {
            return r_parseVersion.Replace(versionString, "");
        }

        private static readonly Dictionary<Regex, GitErrorCode> _gitErrorRegexes = new Dictionary<Regex, GitErrorCode>
        {
            {new Regex(@"Another git process seems to be running in this repository|If no other git process is currently running", RegexOptions.Compiled), GitErrorCode.RepositoryIsLocked },
            {new Regex(@"Authentication failed", RegexOptions.Compiled), GitErrorCode.AuthenticationFailed },
            {new Regex(@"Not a git repository", RegexOptions.IgnoreCase | RegexOptions.Compiled), GitErrorCode.NotAGitRepository },
            {new Regex(@"bad config file", RegexOptions.Compiled), GitErrorCode.BadConfigFile },
            {new Regex(@"cannot make pipe for command substitution|cannot create standard input pipe", RegexOptions.Compiled), GitErrorCode.CantCreatePipe },
            {new Regex(@"Repository not found", RegexOptions.Compiled), GitErrorCode.RepositoryNotFound },
            {new Regex(@"unable to access", RegexOptions.Compiled), GitErrorCode.CantAccessRemote },
            {new Regex(@"branch '.+' is not fully merged", RegexOptions.Compiled), GitErrorCode.BranchNotFullyMerged },
            {new Regex(@"Couldn't find remote ref", RegexOptions.Compiled), GitErrorCode.NoRemoteReference },
            {new Regex(@"A branch named '.+' already exists", RegexOptions.Compiled), GitErrorCode.BranchAlreadyExists },
            {new Regex(@"'.+' is not a valid branch name", RegexOptions.Compiled), GitErrorCode.InvalidBranchName },
            //mine:
            //{ new Regex(@"pathspec '.+' did not match any files", RegexOptions.Compiled), GitErrorCode.NoPathFound },
            //{ new Regex(@"current branch '.+' does not have any commits yet", RegexOptions.Compiled), GitErrorCode. },
            //template:
            //{new Regex(@"", RegexOptions.Compiled), GitErrorCode. },
        };
        public GitErrorCode? ParseErrorCode(string gitError)
        {
            foreach (var kvp in _gitErrorRegexes)
            {
                var regex = kvp.Key;
                if (regex.IsMatch(gitError))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        public IEnumerable<GitCommit> ParseLog(string log)
        {
            int index = 0;
            while (index < log.Length)
            {
                var nextIndex = log.IndexOf(CommitSeparator, index);
                if (nextIndex == -1)
                {
                    nextIndex = log.Length;
                }

                var entry = log.Substring(index, nextIndex - index);
                if (entry.StartsWith("\n"))
                {
                    entry = entry.Substring(1);
                }

                var commit = ParseCommit(entry);
                if (commit == null)
                {
                    break;
                }

                yield return commit;
                index = nextIndex + CommitSeparator.Length;
            }
        }

        private static readonly Regex r_parseCommit = new Regex(@"^([0-9a-f]{40})\n(.*)\n(.*)\n([\s\S]*)$", RegexOptions.Multiline | RegexOptions.Compiled);
        public GitCommit ParseCommit(string commit)
        {
            var match = r_parseCommit.Match(commit.Trim());
            if (!match.Success)
            {
                return null;
            }

            var parents = (match.Groups[3].Success && !string.IsNullOrEmpty(match.Groups[3].Value)) ? match.Groups[3].Value.Split(' ') : null;
            return new GitCommit(match.Groups[1].Value, match.Groups[4].Value, parents, match.Groups[2].Value);
        }

        public List<GitFileStatus> ParseStatus(ReadOnlySpan<char> status)
        {
            ReadOnlySpan<char> ParseEntry(ReadOnlySpan<char> entry, out GitFileStatus fileStatus)
            {
                fileStatus = default;
                if (entry.Length <= 4)
                {
                    return null;
                }

                string Rename = null;
                string Path;
                char X = entry[0];
                char Y = entry[1];
                //space = entry[2]
                entry = entry.Slice(3); //X + Y + space

                int lastIndex;
                switch (X)
                {
                    case 'R':
                    case 'C':
                        lastIndex = entry.IndexOf('\0');
                        if (lastIndex == -1)
                        {
                            return null;
                        }

                        Rename = entry.Slice(0, lastIndex).ToString();
                        entry = entry.Slice(lastIndex + 1);
                        break;
                }

                lastIndex = entry.IndexOf('\0');
                if (lastIndex == -1)
                {
                    return null;
                }

                Path = entry.Slice(0, lastIndex).ToString();

                //from: git.ts
                // If path ends with slash, it must be a nested git repo
                if (entry[lastIndex - 1] != '/')
                {
                    fileStatus = (X, Y, Rename, Path);
                }

                return entry.Slice(lastIndex + 1);
            }

            var parsed = new List<GitFileStatus>();
            while ((status = ParseEntry(status, out GitFileStatus fileStatus)) != null)
                parsed.Add(fileStatus);

            return parsed;
        }
    }
}
