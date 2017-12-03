﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Interception
{
    using System.Collections;
    using System.Data.Common;
    using System.Data.Entity.Resources;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Moq.Protected;
    using Xunit;
    using Xunit.Extensions;

    public class DatabaseLogFormatterTests : TestBase
    {
        private static readonly StringResourceVerifier _resourceVerifier = new StringResourceVerifier(
            new AssemblyResourceLookup(EntityFrameworkAssembly, "System.Data.Entity.Properties.Resources"));

        public class Constructors : TestBase
        {
            [Fact]
            public void Constructors_validate_arguments()
            {
                Assert.Equal(
                    "writeAction",
                    Assert.Throws<ArgumentNullException>(() => new DatabaseLogFormatter(new Mock<DbContext>().Object, null)).ParamName);
                Assert.Equal(
                    "writeAction",
                    Assert.Throws<ArgumentNullException>(() => new DatabaseLogFormatter(null)).ParamName);
            }
        }

        public class Context : TestBase
        {
            [Fact]
            public void Context_returns_configured_context()
            {
                var context = new Mock<DbContext>().Object;
                Assert.Same(context, new DatabaseLogFormatter(context, new StringWriter().Write).Context);
            }
        }

        public class Writer : TestBase
        {
            [Fact]
            public void Writer_returns_configured_writer()
            {
                Action<string> writer = new StringWriter().Write;
                Assert.Same(writer, new DatabaseLogFormatter(new Mock<DbContext>().Object, writer).WriteAction);
            }
        }

        public class NonQueryExecuting : TestBase
        {
            [Fact]
            public void NonQueryExecuting_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(() => formatter.NonQueryExecuting(null, new DbCommandInterceptionContext<int>()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.NonQueryExecuting(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void NonQueryExecuting_logs()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).NonQueryExecuting(CreateCommand("I am Sam"), new DbCommandInterceptionContext<int>());

                Assert.Equal("I am Sam", GetSingleLine(writer));
            }
        }

        public class ReaderExecuting : TestBase
        {
            [Fact]
            public void ReaderExecuting_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.ReaderExecuting(null, new DbCommandInterceptionContext<DbDataReader>())).ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.ReaderExecuting(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void ReaderExecuting_logs()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).ReaderExecuting(
                    CreateCommand("I am Sam"), new DbCommandInterceptionContext<DbDataReader>());

                Assert.Equal("I am Sam", GetSingleLine(writer));
            }
        }

        public class ScalarExecuting : TestBase
        {
            [Fact]
            public void ScalarExecuting_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(() => formatter.ScalarExecuting(null, new DbCommandInterceptionContext<object>()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.ScalarExecuting(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void ScalarExecuting_logs()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).ScalarExecuting(
                    CreateCommand("Sam I am"), new DbCommandInterceptionContext<object>());

                Assert.Equal("Sam I am", GetSingleLine(writer));
            }
        }

        public class NonQueryExecuted : TestBase
        {
            [Fact]
            public void NonQueryExecuted_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(() => formatter.NonQueryExecuted(null, new DbCommandInterceptionContext<int>()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.NonQueryExecuted(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void NonQueryExecuted_logs()
            {
                var interceptionContext = new DbCommandInterceptionContext<int>();
                interceptionContext.Result = 88;
                var writer = new StringWriter();

                new DatabaseLogFormatter(writer.Write).NonQueryExecuted(CreateCommand(""), interceptionContext);

                Assert.Equal(Strings.CommandLogComplete(0, "88", ""), GetSingleLine(writer));
            }
        }

        public class ReaderExecuted : TestBase
        {
            [Fact]
            public void ReaderExecuted_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.ReaderExecuted(null, new DbCommandInterceptionContext<DbDataReader>()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.ReaderExecuted(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void ReaderExecuted_logs()
            {
                var dataReader = new Mock<DbDataReader>().Object;
                var interceptionContext = new DbCommandInterceptionContext<DbDataReader>();

                interceptionContext.Result = dataReader;
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).ReaderExecuted(CreateCommand(""), interceptionContext);

                Assert.Equal(Strings.CommandLogComplete(0, dataReader.GetType().Name, ""), GetSingleLine(writer));
            }
        }

        public class ScalarExecuted : TestBase
        {
            [Fact]
            public void ScalarExecuted_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(() => formatter.ScalarExecuted(null, new DbCommandInterceptionContext<object>()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.ScalarExecuted(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void ScalarExecuted_logs()
            {
                var interceptionContext = new DbCommandInterceptionContext<object>();
                interceptionContext.Result = "That Sam-I-am";

                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).ScalarExecuted(CreateCommand(""), interceptionContext);

                Assert.Equal(Strings.CommandLogComplete(0, "That Sam-I-am", ""), GetSingleLine(writer));
            }
        }

        public class Executing : TestBase
        {
            [Fact]
            public void Executing_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(() => formatter.Executing(null, new DbCommandInterceptionContext<int>())).ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.Executing<int>(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void Executing_filters_by_context_when_set()
            {
                var context1 = new Mock<DbContext>().Object;
                var context2 = new Mock<DbContext>().Object;

                var writer = new StringWriter();
                var formatter = new DatabaseLogFormatter(writer.Write);
                formatter.Executing(CreateCommand("That Sam-I-am!"), new DbCommandInterceptionContext<int>().WithDbContext(context1));
                formatter.Executing(CreateCommand("I do not like"), new DbCommandInterceptionContext<int>().WithDbContext(context2));
                formatter.Executing(CreateCommand("that Sam-I-am"), new DbCommandInterceptionContext<int>());

                Assert.Equal("That Sam-I-am!", GetSingleLine(writer));
            }

            [Fact]
            public void Executing_logs_every_command_when_context_not_set()
            {
                var context1 = new Mock<DbContext>().Object;
                var context2 = new Mock<DbContext>().Object;

                var writer = new StringWriter();
                var formatter = new DatabaseLogFormatter(writer.Write);
                formatter.Executing(CreateCommand("Do you like"), new DbCommandInterceptionContext<int>());
                formatter.Executing(CreateCommand("Green eggs and ham?"), new DbCommandInterceptionContext<int>().WithDbContext(context1));
                formatter.Executing(CreateCommand("I do not like them"), new DbCommandInterceptionContext<int>().WithDbContext(context2));

                var lines = GetLines(writer);
                Assert.Equal("Do you like", lines[0]);
                Assert.Equal("Green eggs and ham?", lines[2]);
                Assert.Equal("I do not like them", lines[4]);
            }
        }

        public class Executed : TestBase
        {
            [Fact]
            public void Executed_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(() => formatter.Executed(null, new DbCommandInterceptionContext<int>()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.Executed<int>(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void Executed_filters_by_context_when_set()
            {
                var context1 = new Mock<DbContext>().Object;
                var context2 = new Mock<DbContext>().Object;

                var writer = new StringWriter();
                var formatter = new DatabaseLogFormatter(writer.Write);

                var interceptionContext = new DbCommandInterceptionContext<string>().WithDbContext(context1);
                interceptionContext.Result = "Sam-I-am";
                formatter.Executed(CreateCommand(""), interceptionContext);

                interceptionContext = new DbCommandInterceptionContext<string>().WithDbContext(context2);
                interceptionContext.Result = "I do not like";
                formatter.Executed(CreateCommand(""), interceptionContext);

                formatter.Executed(CreateCommand(""), new DbCommandInterceptionContext<string> { Result = "Green eggs and ham" });

                Assert.Equal(Strings.CommandLogComplete(0, "Sam-I-am", ""), GetSingleLine(writer));
            }

            [Fact]
            public void Executed_logs_every_command_when_context_not_set()
            {
                var context1 = new Mock<DbContext>().Object;
                var context2 = new Mock<DbContext>().Object;

                var writer = new StringWriter();
                var formatter = new DatabaseLogFormatter(writer.Write);

                formatter.Executed(CreateCommand(""), new DbCommandInterceptionContext<string> { Result = "Would you like them" });

                var interceptionContext = new DbCommandInterceptionContext<string>().WithDbContext(context1);
                interceptionContext.Result = "Here or there?";
                formatter.Executed(CreateCommand(""), interceptionContext);

                interceptionContext = new DbCommandInterceptionContext<string>().WithDbContext(context2);
                interceptionContext.Result = "I would not like them";
                formatter.Executed(CreateCommand(""), interceptionContext);

                var lines = GetLines(writer);
                Assert.Equal(Strings.CommandLogComplete(0, "Would you like them", ""), lines[0]);
                Assert.Equal(Strings.CommandLogComplete(0, "Here or there?", ""), lines[1]);
                Assert.Equal(Strings.CommandLogComplete(0, "I would not like them", ""), lines[2]);
            }
        }

        public class LogCommand : TestBase
        {
            [Fact]
            public void LogCommand_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(() => formatter.LogCommand(null, new DbCommandInterceptionContext<int>()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.LogCommand<int>(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void LogCommand_can_handle_commands_with_null_text_and_parameters()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(new Mock<DbCommand>().Object, new DbCommandInterceptionContext<int>());

                Assert.Equal("<null>", GetLines(writer)[0]);
            }

            [Fact]
            public void LogCommand_logs_command_text_and_parameters()
            {
                var parameter1 = CreateParameter("Param1", ParameterDirection.Input, true, DbType.String, 4000, 0, 0, "value");
                var parameter2 = CreateParameter("Param2", ParameterDirection.InputOutput, false, DbType.Decimal, -1, 18, 2, 7.7m);

                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(
                    CreateCommand("here or there", parameter1, parameter2), new DbCommandInterceptionContext<int>());

                var lines = GetLines(writer);
                Assert.Equal("here or there", lines[0]);
                Assert.Equal("-- Param1: 'value' (Type = String, Size = 4000)", lines[1]);

                var expected = string.Format(
                    CultureInfo.CurrentCulture,
                    "-- Param2: '{0}' (Type = Decimal, Direction = InputOutput, IsNullable = false, Size = -1, Precision = 18, Scale = 2)",
                    7.7m);
                Assert.Equal(expected, lines[2]);
            }

            [Fact]
            public void LogCommand_adds_timestamp_log_line_for_async_commands()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(
                    CreateCommand("I would not like them"), new DbCommandInterceptionContext<int>().AsAsync());

                var lines = GetLines(writer);
                Assert.Equal("I would not like them", lines[0]);
                Assert.Contains(Strings.CommandLogAsync("", ""), lines[1]);

                var timestamp = DateTime.Parse(lines[1].Substring(Strings.CommandLogAsync("", "").Length));
                Assert.Equal(0d, Math.Abs((timestamp - DateTime.Now).TotalMinutes), 0);
            }

            [Fact]
            public void LogCommand_adds_timestamp_log_line_for_sync_commands()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(
                    CreateCommand("I would not like them"), new DbCommandInterceptionContext<int>());

                var lines = GetLines(writer);
                Assert.Equal("I would not like them", lines[0]);
                Assert.Contains(Strings.CommandLogNonAsync("", ""), lines[1]);

                var timestamp = DateTime.Parse(lines[1].Substring(Strings.CommandLogNonAsync("", "").Length));
                Assert.Equal(0d, Math.Abs((timestamp - DateTime.Now).TotalMinutes), 0);
            }
        }

        public class LogParameter : TestBase
        {
            [Fact]
            public void LogParameter_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.LogParameter(
                            null, new DbCommandInterceptionContext<int>(), new Mock<DbParameter>().Object)).ParamName);

                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.LogParameter<int>(
                            new Mock<DbCommand>().Object, null, new Mock<DbParameter>().Object)).ParamName);

                Assert.Equal(
                    "parameter",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.LogParameter(
                            new Mock<DbCommand>().Object, new DbCommandInterceptionContext<int>(), null)).ParamName);
            }

            [Fact]
            public void LogParameter_handles_all_normal_properties()
            {
                var parameter = CreateParameter("Param1", ParameterDirection.InputOutput, false, DbType.Decimal, 4, 18, 2, 2013m);

                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(CreateCommand("", parameter), new DbCommandInterceptionContext<int>());

                Assert.Equal(
                    "-- Param1: '2013' (Type = Decimal, Direction = InputOutput, IsNullable = false, Size = 4, Precision = 18, Scale = 2)",
                    GetLines(writer)[0]);
            }

            [Fact]
            public void LogParameter_handles_nullable_and_non_nullable_properties()
            {
                var parameter1 = CreateParameter("Param1", ParameterDirection.Input, true, DbType.String, 4000, 0, 0, "value");
                var parameter2 = CreateParameter("Param2", ParameterDirection.Input, false, DbType.String, 4000, 0, 0, "value");

                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(
                    CreateCommand("", parameter1, parameter2), new DbCommandInterceptionContext<int>());

                var lines = GetLines(writer);
                Assert.Equal("-- Param1: 'value' (Type = String, Size = 4000)", lines[0]);
                Assert.Equal("-- Param2: 'value' (Type = String, IsNullable = false, Size = 4000)", lines[1]);
            }

            [Fact]
            public void LogParameter_handles_different_kinds_of_null_and_non_null_values()
            {
                var parameter1 = CreateParameter("Param1", ParameterDirection.Input, true, DbType.String, 4000, 0, 0, null);
                var parameter2 = CreateParameter("Param2", ParameterDirection.Input, false, DbType.String, 4000, 0, 0, DBNull.Value);
                var parameter3 = CreateParameter("Param3", ParameterDirection.Input, false, DbType.String, 4000, 0, 0, "Not Null");

                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(
                    CreateCommand("", parameter1, parameter2, parameter3), new DbCommandInterceptionContext<int>());

                var lines = GetLines(writer);
                Assert.Equal("-- Param1: 'null' (Type = String, Size = 4000)", lines[0]);
                Assert.Equal("-- Param2: 'null' (Type = String, IsNullable = false, Size = 4000)", lines[1]);
                Assert.Equal("-- Param3: 'Not Null' (Type = String, IsNullable = false, Size = 4000)", lines[2]);
            }

            [Fact]
            public void LogParameter_includes_Scale_only_if_set()
            {
                var parameter1 = CreateParameter("Param1", ParameterDirection.Input, true, DbType.String, 0, 0, 2, "value");
                var parameter2 = CreateParameter("Param2", ParameterDirection.Input, true, DbType.String, 0, 0, 0, "value");

                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(
                    CreateCommand("", parameter1, parameter2), new DbCommandInterceptionContext<int>());

                var lines = GetLines(writer);
                Assert.Equal("-- Param1: 'value' (Type = String, Scale = 2)", lines[0]);
                Assert.Equal("-- Param2: 'value' (Type = String)", lines[1]);
            }

            [Fact]
            public void LogParameter_includes_Precision_only_if_set()
            {
                var parameter1 = CreateParameter("Param1", ParameterDirection.Input, true, DbType.String, 0, 18, 0, "value");
                var parameter2 = CreateParameter("Param2", ParameterDirection.Input, true, DbType.String, 0, 0, 0, "value");

                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(
                    CreateCommand("", parameter1, parameter2), new DbCommandInterceptionContext<int>());

                var lines = GetLines(writer);
                Assert.Equal("-- Param1: 'value' (Type = String, Precision = 18)", lines[0]);
                Assert.Equal("-- Param2: 'value' (Type = String)", lines[1]);
            }

            [Fact]
            public void LogParameter_includes_Size_only_if_set()
            {
                var parameter1 = CreateParameter("Param1", ParameterDirection.Input, true, DbType.String, -1, 0, 0, "value");
                var parameter2 = CreateParameter("Param2", ParameterDirection.Input, true, DbType.String, 0, 0, 0, "value");

                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(
                    CreateCommand("", parameter1, parameter2), new DbCommandInterceptionContext<int>());

                var lines = GetLines(writer);
                Assert.Equal("-- Param1: 'value' (Type = String, Size = -1)", lines[0]);
                Assert.Equal("-- Param2: 'value' (Type = String)", lines[1]);
            }

            [Fact]
            public void LogParameter_includes_Direction_only_if_set()
            {
                var parameter1 = CreateParameter("Param1", ParameterDirection.ReturnValue, true, DbType.String, 0, 0, 0, "value");
                var parameter2 = CreateParameter("Param2", ParameterDirection.Input, true, DbType.String, 0, 0, 0, "value");

                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogCommand(
                    CreateCommand("asfasfasg", parameter1, parameter2), new DbCommandInterceptionContext<int>());

                var lines = GetLines(writer);
                Assert.Equal("-- Param1: 'value' (Type = String, Direction = ReturnValue)", lines[1]);
                Assert.Equal("-- Param2: 'value' (Type = String)", lines[2]);
            }
        }

        public class LogResult : TestBase
        {
            [Fact]
            public void LogResult_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "command",
                    Assert.Throws<ArgumentNullException>(() => formatter.LogResult(null, new DbCommandInterceptionContext<int>()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.LogResult<int>(new Mock<DbCommand>().Object, null)).ParamName);
            }

            [Fact]
            public void LogResult_handles_completed_commands_with_DbDataReader_results()
            {
                var dataReader = new Mock<DbDataReader>().Object;
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogResult(
                    new Mock<DbCommand>().Object, new DbCommandInterceptionContext<DbDataReader> { Result = dataReader });

                Assert.Equal(Strings.CommandLogComplete(0, dataReader.GetType().Name, ""), GetSingleLine(writer));
            }

            [Fact]
            public void LogResult_handles_completed_commands_with_int_results()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogResult(
                    new Mock<DbCommand>().Object, new DbCommandInterceptionContext<int> { Result = 77 });

                Assert.Equal(Strings.CommandLogComplete(0, "77", ""), GetSingleLine(writer));
            }

            [Fact]
            public void LogResult_handles_completed_commands_with_some_object_results()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogResult(
                    new Mock<DbCommand>().Object, new DbCommandInterceptionContext<string> { Result = "Green Eggs and Ham" });

                Assert.Equal(Strings.CommandLogComplete(0, "Green Eggs and Ham", ""), GetSingleLine(writer));
            }

            [Fact]
            public void LogResult_handles_completed_commands_with_null_results()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogResult(
                    new Mock<DbCommand>().Object, new DbCommandInterceptionContext<DbDataReader>());

                Assert.Equal(Strings.CommandLogComplete(0, "null", ""), GetSingleLine(writer));
            }

            [Fact]
            public void LogResult_handles_failed_commands()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).LogResult(
                    new Mock<DbCommand>().Object,
                    new DbCommandInterceptionContext<DbDataReader> { Exception = new Exception("I do not like them!") });

                Assert.Equal(Strings.CommandLogFailed(0, "I do not like them!", ""), GetSingleLine(writer));
            }

            [Fact]
            public void LogResult_handles_canceled_commands()
            {
                var writer = new StringWriter();

                var interceptionContext = new DbCommandInterceptionContext<DbDataReader>();
                interceptionContext.MutableData.TaskStatus = TaskStatus.Canceled;

                new DatabaseLogFormatter(writer.Write).LogResult(
                    new Mock<DbCommand>().Object,
                    interceptionContext);

                Assert.Equal(Strings.CommandLogCanceled(0, ""), GetSingleLine(writer));
            }

            [Fact]
            public void LogResult_uses_different_timers_per_interception_context()
            {
                var writer = new StringWriter();
                var formatter = new DatabaseLogFormatter(writer.Write);

                var interceptionContexts = new DbCommandInterceptionContext<int>[10];
                var elapsedTimes = new long[10];

                for (var i = 0; i < 10; i++)
                {
                    interceptionContexts[i] = new DbCommandInterceptionContext<int> { Result = 77 };
                }

                for (var i = 0; i < 10; i++)
                {
                    elapsedTimes[i] = GetElapsed(formatter, interceptionContexts[i], false, i * 5);
                }

                for (var i = 0; i < 10; i++)
                {
                    formatter.LogResult(new Mock<DbCommand>().Object, interceptionContexts[i]);

                    Assert.Equal(Strings.CommandLogComplete(elapsedTimes[i], "77", ""), GetLines(writer).Skip(i).First());
                }
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void LogResult_logs_elapsed_time_for_completed_commands(bool useObsoleteTimer)
            {
                var writer = new StringWriter();
                var formatter = new DatabaseLogFormatter(writer.Write);
                var interceptionContext = new DbCommandInterceptionContext<int> { Result = 77 };

                var elapsed = GetElapsed(formatter, interceptionContext, useObsoleteTimer);

                formatter.LogResult(new Mock<DbCommand>().Object, interceptionContext);

                Assert.Equal(Strings.CommandLogComplete(elapsed, "77", ""), GetSingleLine(writer));
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void LogResult_logs_elapsed_time_for_failed_commands(bool useObsoleteTimer)
            {
                var writer = new StringWriter();
                var formatter = new DatabaseLogFormatter(writer.Write);
                var interceptionContext = new DbCommandInterceptionContext<int> { Exception = new Exception("I do not like them!") };

                var elapsed = GetElapsed(formatter, interceptionContext, useObsoleteTimer);

                formatter.LogResult(new Mock<DbCommand>().Object, interceptionContext);

                Assert.Equal(Strings.CommandLogFailed(elapsed, "I do not like them!", ""), GetSingleLine(writer));
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void LogResult_logs_elapsed_time_for_canceled_commands(bool useObsoleteTimer)
            {
                var writer = new StringWriter();
                var formatter = new DatabaseLogFormatter(writer.Write);

                var interceptionContext = new DbCommandInterceptionContext<int>();
                interceptionContext.MutableData.TaskStatus = TaskStatus.Canceled;

                var elapsed = GetElapsed(formatter, interceptionContext, useObsoleteTimer);

                formatter.LogResult(new Mock<DbCommand>().Object, interceptionContext);

                Assert.Equal(Strings.CommandLogCanceled(elapsed, ""), GetSingleLine(writer));
            }

            private static long GetElapsed(
                DatabaseLogFormatter formatter, 
                DbCommandInterceptionContext interceptionContext, 
                bool useObsoleteTimer,
                int time = 10)
            {
                var stopwatch = useObsoleteTimer 
#pragma warning disable 618
                    ? formatter.Stopwatch
#pragma warning restore 618
                    : formatter.GetStopwatch(interceptionContext);

                stopwatch.Restart();
                Thread.Sleep(time);
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            }
        }

        public class Committed : TestBase
        {
            [Fact]
            public void Committed_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "transaction",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.Committed(null, new DbTransactionInterceptionContext()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.Committed(new Mock<DbTransaction>().Object, null)).ParamName);
            }

            [Fact]
            public void Committed_logs()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).Committed(new Mock<DbTransaction>().Object, new DbTransactionInterceptionContext());
                Assert.True(_resourceVerifier.IsMatch("TransactionCommittedLog", GetSingleLine(writer), new AnyValueParameter(), ""));
            }

            [Fact]
            public void Committed_logs_exceptions()
            {
                var writer = new StringWriter();
                var interceptionContext = new DbTransactionInterceptionContext();
                interceptionContext.Exception = new Exception("Boo");
                new DatabaseLogFormatter(writer.Write).Committed(new Mock<DbTransaction>().Object, interceptionContext);
                Assert.True(
                    _resourceVerifier.IsMatch("TransactionCommitErrorLog", GetSingleLine(writer), new AnyValueParameter(), "Boo", ""));
            }
        }

        public class RolledBack : TestBase
        {
            [Fact]
            public void RolledBack_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "transaction",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.RolledBack(null, new DbTransactionInterceptionContext()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.RolledBack(new Mock<DbTransaction>().Object, null)).ParamName);
            }

            [Fact]
            public void RolledBack_logs()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).RolledBack(new Mock<DbTransaction>().Object, new DbTransactionInterceptionContext());
                Assert.True(_resourceVerifier.IsMatch("TransactionRolledBackLog", GetSingleLine(writer), new AnyValueParameter(), ""));
            }

            [Fact]
            public void RolledBack_logs_exceptions()
            {
                var writer = new StringWriter();
                var interceptionContext = new DbTransactionInterceptionContext();
                interceptionContext.Exception = new Exception("Boo");
                new DatabaseLogFormatter(writer.Write).RolledBack(new Mock<DbTransaction>().Object, interceptionContext);
                Assert.True(
                    _resourceVerifier.IsMatch("TransactionRollbackErrorLog", GetSingleLine(writer), new AnyValueParameter(), "Boo", ""));
            }
        }

        public class Disposing_Transaction : TestBase
        {
            [Fact]
            public void Disposing_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "transaction",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.Disposing(null, new DbTransactionInterceptionContext()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.Disposing(new Mock<DbTransaction>().Object, null)).ParamName);
            }

            [Fact]
            public void Disposing_logs_if_transaction_still_active()
            {
                var writer = new StringWriter();
                var transactionMock = new Mock<DbTransaction>();
                transactionMock.Protected().Setup<DbConnection>("DbConnection").Returns(new Mock<DbConnection>().Object);
                new DatabaseLogFormatter(writer.Write).Disposing(transactionMock.Object, new DbTransactionInterceptionContext());
                Assert.True(_resourceVerifier.IsMatch("TransactionDisposedLog", GetSingleLine(writer), new AnyValueParameter(), ""));
            }

            [Fact]
            public void Disposing_does_not_log_if_transaction_not_active()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).Disposing(new Mock<DbTransaction>().Object, new DbTransactionInterceptionContext());
                Assert.Null(GetSingleLine(writer));
            }
        }

        public class BeganTransaction : TestBase
        {
            [Fact]
            public void BeganTransaction_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "connection",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.BeganTransaction(null, new BeginTransactionInterceptionContext()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.BeganTransaction(new Mock<DbConnection>().Object, null)).ParamName);
            }

            [Fact]
            public void BeganTransaction_logs()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).BeganTransaction(
                    new Mock<DbConnection>().Object, new BeginTransactionInterceptionContext());
                Assert.True(_resourceVerifier.IsMatch("TransactionStartedLog", GetSingleLine(writer), new AnyValueParameter(), ""));
            }

            [Fact]
            public void BeganTransaction_logs_exceptions()
            {
                var writer = new StringWriter();
                var interceptionContext = new BeginTransactionInterceptionContext();
                interceptionContext.Exception = new Exception("Boo");
                new DatabaseLogFormatter(writer.Write).BeganTransaction(new Mock<DbConnection>().Object, interceptionContext);
                Assert.True(
                    _resourceVerifier.IsMatch("TransactionStartErrorLog", GetSingleLine(writer), new AnyValueParameter(), "Boo", ""));
            }
        }

        public class Closed : TestBase
        {
            [Fact]
            public void Closed_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "connection",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.Closed(null, new DbConnectionInterceptionContext()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.Closed(new Mock<DbConnection>().Object, null)).ParamName);
            }

            [Fact]
            public void Closed_logs()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).Closed(new Mock<DbConnection>().Object, new DbConnectionInterceptionContext());
                Assert.True(_resourceVerifier.IsMatch("ConnectionClosedLog", GetSingleLine(writer), new AnyValueParameter(), ""));
            }

            [Fact]
            public void Closed_logs_exceptions()
            {
                var writer = new StringWriter();
                var interceptionContext = new DbConnectionInterceptionContext();
                interceptionContext.Exception = new Exception("Boo");
                new DatabaseLogFormatter(writer.Write).Closed(new Mock<DbConnection>().Object, interceptionContext);
                Assert.True(_resourceVerifier.IsMatch("ConnectionCloseErrorLog", GetSingleLine(writer), new AnyValueParameter(), "Boo", ""));
            }
        }

        public class Disposing_Connection : TestBase
        {
            [Fact]
            public void Disposing_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "connection",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.Disposing(null, new DbConnectionInterceptionContext()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.Disposing(new Mock<DbConnection>().Object, null)).ParamName);
            }

            [Fact]
            public void Disposing_logs_if_connection_open()
            {
                var writer = new StringWriter();
                var connectionMock = new Mock<DbConnection>();
                connectionMock.Setup(m => m.State).Returns(ConnectionState.Open);
                new DatabaseLogFormatter(writer.Write).Disposing(connectionMock.Object, new DbConnectionInterceptionContext());
                Assert.True(_resourceVerifier.IsMatch("ConnectionDisposedLog", GetSingleLine(writer), new AnyValueParameter(), ""));
            }

            [Fact]
            public void Disposing_does_not_log_if_connection_closed()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).Disposing(new Mock<DbConnection>().Object, new DbConnectionInterceptionContext());
                Assert.Null(GetSingleLine(writer));
            }
        }

        public class Opened : TestBase
        {
            [Fact]
            public void Opened_validates_arguments()
            {
                var formatter = new DatabaseLogFormatter(new StringWriter().Write);

                Assert.Equal(
                    "connection",
                    Assert.Throws<ArgumentNullException>(
                        () => formatter.Opened(null, new DbConnectionInterceptionContext()))
                        .ParamName);
                Assert.Equal(
                    "interceptionContext",
                    Assert.Throws<ArgumentNullException>(() => formatter.Opened(new Mock<DbConnection>().Object, null)).ParamName);
            }

            [Fact]
            public void Opened_logs()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).Opened(new Mock<DbConnection>().Object, new DbConnectionInterceptionContext());
                Assert.True(_resourceVerifier.IsMatch("ConnectionOpenedLog", GetSingleLine(writer), new AnyValueParameter(), ""));
            }

            [Fact]
            public void Opened_logs_async()
            {
                var writer = new StringWriter();
                new DatabaseLogFormatter(writer.Write).Opened(
                    new Mock<DbConnection>().Object, new DbConnectionInterceptionContext().AsAsync());
                Assert.True(_resourceVerifier.IsMatch("ConnectionOpenedLogAsync", GetSingleLine(writer), new AnyValueParameter(), ""));
            }

            [Fact]
            public void Opened_logs_exceptions()
            {
                var writer = new StringWriter();
                var interceptionContext = new DbConnectionInterceptionContext();
                interceptionContext.Exception = new Exception("Boo");
                new DatabaseLogFormatter(writer.Write).Opened(new Mock<DbConnection>().Object, interceptionContext);
                Assert.True(_resourceVerifier.IsMatch("ConnectionOpenErrorLog", GetSingleLine(writer), new AnyValueParameter(), "Boo", ""));
            }

            [Fact]
            public void Opened_logs_exceptions_async()
            {
                var writer = new StringWriter();
                var interceptionContext = new DbConnectionInterceptionContext().AsAsync();
                interceptionContext.Exception = new Exception("Boo");
                new DatabaseLogFormatter(writer.Write).Opened(new Mock<DbConnection>().Object, interceptionContext);
                Assert.True(
                    _resourceVerifier.IsMatch("ConnectionOpenErrorLogAsync", GetSingleLine(writer), new AnyValueParameter(), "Boo", ""));
            }

            [Fact]
            public void Opened_logs_canceled()
            {
                var writer = new StringWriter();
                var interceptionContext = new DbConnectionInterceptionContext().AsAsync();
                interceptionContext.MutableData.TaskStatus = TaskStatus.Canceled;
                new DatabaseLogFormatter(writer.Write).Opened(new Mock<DbConnection>().Object, interceptionContext);
                Assert.True(_resourceVerifier.IsMatch("ConnectionOpenCanceledLog", GetSingleLine(writer), new AnyValueParameter(), ""));
            }
        }

        private static DbCommand CreateCommand(string commandText, params DbParameter[] parameters)
        {
            var mockParameters = new Mock<DbParameterCollection>();
            mockParameters.As<IEnumerable>().Setup(m => m.GetEnumerator()).Returns(parameters.GetEnumerator());

            var mockCommand = new Mock<DbCommand>();
            mockCommand.Setup(m => m.CommandText).Returns(commandText);
            mockCommand.Protected().Setup<DbParameterCollection>("DbParameterCollection").Returns(mockParameters.Object);

            return mockCommand.Object;
        }

        private static DbParameter CreateParameter(
            string name,
            ParameterDirection direction,
            bool isNullable,
            DbType type,
            int size,
            byte precision,
            byte scale,
            object value)
        {
            var parameter = new Mock<DbParameter>();
            parameter.Setup(m => m.ParameterName).Returns(name);
            parameter.Setup(m => m.Direction).Returns(direction);
            parameter.Setup(m => m.IsNullable).Returns(isNullable);
            parameter.Setup(m => m.DbType).Returns(type);
            parameter.Setup(m => m.Size).Returns(size);
            parameter.As<IDbDataParameter>().Setup(m => m.Precision).Returns(precision);
            parameter.As<IDbDataParameter>().Setup(m => m.Scale).Returns(scale);
            parameter.Setup(m => m.Value).Returns(value);

            return parameter.Object;
        }

        private static string GetSingleLine(TextWriter writer)
        {
            return GetLines(writer).FirstOrDefault();
        }

        private static string[] GetLines(TextWriter writer)
        {
            return writer.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
