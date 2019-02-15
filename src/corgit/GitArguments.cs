﻿using System;
using System.Collections.Generic;
using System.Linq;
using static corgit.GitParsing;

namespace corgit
{
    public static class GitArguments
    {
        public struct CommitOptions
        {
            public readonly bool? All;
            public readonly bool? Amend;
            public readonly bool? Signoff;
            public readonly bool? SignCommit;
            public readonly bool? Empty;

            public CommitOptions(bool? all = default,
                                 bool? amend = default,
                                 bool? signoff = default,
                                 bool? signCommit = default,
                                 bool? empty = default)
            {
                All = all;
                Amend = amend;
                Signoff = signoff;
                SignCommit = signCommit;
                Empty = empty;
            }
        }
        public static IEnumerable<string> Commit(CommitOptions options = default)
        {
            yield return "commit";
            yield return "--quiet";
            yield return "--allow-empty-message";
            yield return "--file";
            yield return "-";

            if (options.All == true)
            {
                yield return "--all";
            }

            if (options.Amend == true)
            {
                yield return "--amend";
            }

            if (options.Signoff == true)
            {
                yield return "--signoff";
            }

            if (options.SignCommit == true)
            {
                yield return "-S";
            }

            if (options.Empty == true)
            {
                yield return "--allow-empty";
            }
        }

        public struct LogOptions
        {
            public readonly int? MaxEntries;

            public LogOptions(int? maxEntries = 32)
            {
                MaxEntries = maxEntries;
            }
        }
        public static IEnumerable<string> Log(LogOptions options = default)
        {
            const string CommitFormat = "%H\n%ae\n%P\n%B";
            const string Separator = "%x00%x00";

            yield return "log";

            if (options.MaxEntries.HasValue)
            {
                yield return $"-{options.MaxEntries}";
            }

            yield return $"--pretty=format:{CommitFormat}{Separator}";
        }

        public static IEnumerable<string> Add(IEnumerable<string> paths = null)
        {
            yield return "add";
            yield return "-A";
            yield return "--";
            foreach (var path in (paths ?? Enumerable.Empty<string>()).DefaultIfEmpty("."))
            {
                yield return path;
            }
        }

        public static IEnumerable<string> Remove(IEnumerable<string> paths)
        {
            yield return "rm";
            yield return "--";
            foreach (var path in (paths ?? Enumerable.Empty<string>()))
            {
                yield return path;
            }
        }

        public static IEnumerable<string> Config(string key, string value = null, string scope = null)
        {
            yield return "config";
            if (!string.IsNullOrEmpty(scope))
            {
                yield return $"--{scope}";
            }
            yield return key;
            if (!string.IsNullOrEmpty(value))
            {
                yield return value;
            }
        }

        public static IEnumerable<string> Status()
        {
            yield return "--no-optional-locks";
            yield return "status";
            yield return "-z";
            yield return "-u";
        }

        public static IEnumerable<string> Init()
        {
            yield return "init";
        }
    }
}
