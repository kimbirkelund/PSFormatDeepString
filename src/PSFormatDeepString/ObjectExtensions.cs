using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PSFormatDeepString
{
    public static class ObjectExtensions
    {
        public static Exception FindException(this Exception exception, Func<Exception, bool> predicate)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (predicate(exception))
                return exception;

            return exception.GetInnerExceptions()
                            .Select(iex => iex.FindException(predicate))
                            .FirstOrDefault();
        }

        public static Exception FindException(this Exception exception, IEnumerable<Type> exceptionTypes, Func<Exception, bool> predicate = null)
        {
            return exception.FindException(ex => exceptionTypes.Any(t => t.IsInstanceOfType(ex))
                                                 && (predicate == null || predicate(ex)));
        }

        public static Exception FindException(this Exception exception, Type exceptionType, Func<Exception, bool> predicate = null)
        {
            return exception.FindException(new[] { exceptionType }, predicate);
        }

        public static TException FindException<TException>(this Exception exception, Func<Exception, bool> predicate = null)
            where TException : Exception
        {
            return exception.FindException(new[] { typeof(TException) }, predicate) as TException;
        }

        public static Exception FindException<TException1, TException2>(this Exception exception, Func<Exception, bool> predicate = null)
            where TException1 : Exception
            where TException2 : Exception
        {
            return exception.FindException(new[] { typeof(TException1), typeof(TException2) }, predicate);
        }

        /// <summary>
        ///     Gets inner exception(s) and calls <see cref="UnwrapException" /> on them.
        /// </summary>
        public static IEnumerable<Exception> GetInnerExceptions(this Exception exception)
        {
            var innerExceptions = GetInnerExceptionsFromAggregateException(exception)
                                  ?? GetInnerExceptionsFromTargetInvocationException(exception);
            if (innerExceptions != null)
                return innerExceptions;

            if (exception.InnerException != null)
                return exception.InnerException.UnwrapException();

            return Enumerable.Empty<Exception>();
        }


        /// <summary>
        ///     Unwraps exception until only non-wrapping exceptions are found.
        /// </summary>
        /// <remarks>
        ///     Wrapping exceptions are:
        ///     - <see cref="System.AggregateException" />
        /// - <see cref="TargetInvocationException" />
        /// </remarks>
        public static IEnumerable<Exception> UnwrapException(this Exception exception)
        {
            var innerExceptions = GetInnerExceptionsFromAggregateException(exception)
                                  ?? GetInnerExceptionsFromTargetInvocationException(exception);
            if (innerExceptions != null)
                return innerExceptions;

            return new[] { exception };
        }

        private static IEnumerable<Exception> GetInnerExceptionsFromAggregateException(Exception exception)
        {
            var aggregateException = exception as AggregateException;

            return aggregateException?.InnerExceptions
                                     .SelectMany(ex => ex.UnwrapException());
        }

        private static IEnumerable<Exception> GetInnerExceptionsFromTargetInvocationException(Exception exception)
        {
            var tiEx = exception as TargetInvocationException;

            return tiEx?.InnerException.UnwrapException();
        }
    }
}
