﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using JetBrains.Annotations;

namespace PSFormatDeepString
{
    public class PrettyPrinter
    {
        private static readonly string[] _excludedPropertyNames = { "Type", "Message", "InnerException", "InnerExceptions", "StackTrace", "Data" };

        // @formatter:off — disable formatter after this line
        private static readonly Type[] _simpleTypes = {
            typeof(bool),
            typeof(char), typeof(string),
            typeof(byte), typeof(sbyte), typeof(ushort), typeof(short), typeof(int), typeof(uint), typeof(ulong), typeof(long),
            typeof(float), typeof(double), typeof(decimal),
            typeof(Type), typeof(RuntimeTypeHandle), typeof(Uri), typeof(Version)
        };
        // @formatter:on — enable formatter after this line

        private readonly Dictionary<object, string> _seenObjects = new Dictionary<object, string>();
        private readonly TextWriter _textWriter;

        private int _depth;
        private int _nextId;

        private string Indentation => "".PadLeft(3 * _depth, ' ');
        private string Level => _depth > 0 ? $"- level {_depth} " : "";

        private PrettyPrinter([NotNull] TextWriter textWriter)
        {
            _textWriter = textWriter ?? throw new ArgumentNullException(nameof(textWriter));
        }

        private void Nest([InstantHandle] [NotNull] Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _depth++;
            try
            {
                action();
            }
            finally
            {
                _depth--;
            }
        }

        private void NestIfHasHeader(string header, [InstantHandle] Action action)
        {
            if (string.IsNullOrWhiteSpace(header))
                action();
            else
            {
                WriteLine(header);
                Nest(action);
            }
        }

        private string NextObjectId()
        {
            return $"OID[{_nextId++}]";
        }

        private bool SawNewObject(object obj, out string oid)
        {
            if (_seenObjects.TryGetValue(obj, out oid))
                return false;

            oid = NextObjectId();
            _seenObjects.Add(obj, oid);
            return true;
        }

        private void WriteException(Exception exception, string oid, bool isInnerException)
        {
            var inner = isInnerException ? "INNER " : "";

            _textWriter.WriteLine($"{Indentation}=== BEGIN {inner}EXCEPTION {oid} {Level}===");

            WriteLine("Type", exception.GetType());
            WriteLine("Message", exception.Message);
            WriteLine("StackTrace", exception.StackTrace);

            if (exception.Data.Count > 0)
                WriteLine("Data", exception.Data);

            foreach (var (key, value) in GetProperties(exception))
                WriteLine(key, value);

            var innerExceptions = exception.GetInnerExceptions();
            foreach (var innerException in innerExceptions)
                Nest(() => WriteObject("InnerException", innerException, true));

            _textWriter.WriteLine($"{Indentation}=== END {inner}EXCEPTION {Level}===");
        }

        private void WriteLine(string header, object value)
        {
            bool IsSimple()
            {
                var type = value.GetType();
                return _simpleTypes.Contains(type)
                       || type.FullName == "System.RuntimeType"
                       || type.FullName?.StartsWith("System.Reflection") is true
                       || type.IsEnum;
            }

            switch (value)
            {
                case null:
                case object _ when IsSimple():
                    WriteLine(header, value?.ToString());
                    break;

                case FileSystemInfo fsi:
                    WriteLine(header, $"{fsi.GetType() .Name}({fsi.FullName})");
                    break;

                case IDictionary dictionary:
                    WriteLine(header);
                    Nest(() =>
                         {
                             foreach (var key in dictionary.Keys)
                                 WriteLine($"[{key}]", dictionary[key]);
                         });
                    break;

                case IEnumerable sequence:
                    WriteLine(header);
                    Nest(() =>
                         {
                             foreach (var (obj, idx) in sequence.Cast<object>()
                                                                .Select((o, i) => (o, i)))
                                 WriteLine($"[{idx}]", obj);
                         });
                    break;

                default:
                    WriteObject(header, value);
                    break;
            }
        }

        private void WriteLine(string header, Type value)
        {
            WriteLine(header,
                      value != null
                          ? value.FullName
                          : string.Empty);
        }

        private void WriteLine(string header, string value)
        {
            var lines = (value ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            WriteLine(header, lines);
        }

        private void WriteLine(string header, IEnumerable<string> lines = null)
        {
            var prefix = string.IsNullOrWhiteSpace(header) ? Indentation : Indentation + header + ": ";
            if (lines == null)
            {
                _textWriter.WriteLine(prefix);
                return;
            }

            using (var e = lines.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    _textWriter.WriteLine(prefix);
                    return;
                }

                var firstLine = e.Current;

                if (!e.MoveNext() || string.IsNullOrWhiteSpace(firstLine))
                {
                    _textWriter.WriteLine(prefix + firstLine?.Trim());
                    return;
                }

                _textWriter.WriteLine(prefix);
                Nest(() =>
                     {
                         _textWriter.WriteLine(Indentation + firstLine.TrimEnd());

                         while (e.MoveNext())
                             _textWriter.WriteLine(Indentation + e.Current?.TrimEnd());
                     });
            }
        }

        private void WriteObject(string header, object obj, bool isInnerException = false)
        {
            if (obj == null)
                return;

            if (!SawNewObject(obj, out var oid))
            {
                WriteLine(header, $"already seen object {oid}");
                return;
            }

            if (obj is Exception exception)
            {
                NestIfHasHeader(header,
                                () => { WriteException(exception, oid, isInnerException); });
            }
            else
            {
                using (var e = EnumeratePropertiesWithOid(obj))
                {
                    if (!e.MoveNext())
                    {
                        WriteLine(header, $"{obj}");
                        return;
                    }

                    NestIfHasHeader(header,
                                    () =>
                                    {
                                        _textWriter.WriteLine($"{Indentation}=== BEGIN OBJECT {oid} {Level}===");
                                        do
                                        {
                                            var (key, value) = e.Current;
                                            WriteLine(key, value);
                                        } while (e.MoveNext());

                                        _textWriter.WriteLine($"{Indentation}=== END OBJECT {Level}===");
                                    });
                }
            }
        }

        public static void Print(object obj,
                                 TextWriter textWriter)
        {
            var prettyPrinter = new PrettyPrinter(textWriter);
            prettyPrinter.WriteLine(null, obj);
        }

        public static string Print(object obj)
        {
            if (obj == null)
                return null;

            var textWriter = new StringWriter();

            Print(obj, textWriter);

            return textWriter.ToString();
        }

        private static IEnumerator<(string key, object value)> EnumeratePropertiesWithOid(object obj)
        {
            using (var e = GetProperties(obj)
                .GetEnumerator())
            {
                if (!e.MoveNext())
                    yield break;

                yield return ("Type", obj.GetType()
                                         .FullName);

                do
                    yield return e.Current;
                while (e.MoveNext());
            }
        }

        private static IEnumerable<(string key, object value)> GetProperties(object obj)
        {
            if (obj is Exception exception)
            {
                return exception.GetType()
                                .GetProperties()
                                .Where(p => p.CanRead)
                                .Where(p => !_excludedPropertyNames.Contains(p.Name))
                                .Select(p => (p.Name, p.GetValue(exception, null)));
            }

            if (obj is PSObject psObject)
            {
                return new[]
                       {
                           (nameof(psObject.BaseObject), psObject.BaseObject),
                           (nameof(psObject.ImmediateBaseObject), psObject.ImmediateBaseObject),
                           (nameof(psObject.TypeNames), psObject.TypeNames)
                       }
                    .Concat(psObject.Properties.Where(p => p.IsGettable)
                                    .Select(p => (p.Name, p.Value)));
            }

            return obj.GetType()
                      .GetProperties()
                      .Where(p => p.CanRead && p.GetIndexParameters()
                                                .Length == 0)
                      .Select(p => (p.Name, p.GetValue(obj, null)));
        }
    }
}
