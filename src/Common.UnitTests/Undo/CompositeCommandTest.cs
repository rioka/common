﻿/*
 * Copyright 2006-2015 Bastian Eicher
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace NanoByte.Common.Undo
{
    /// <summary>
    /// Contains test methods for <see cref="CompositeCommand"/>.
    /// </summary>
    [TestFixture]
    public class CompositeCommandTest
    {
        private class MockCommand : IUndoCommand
        {
            private readonly Action _executeCallback, _undoCallback;

            public MockCommand(Action executeCallback, Action undoCallback)
            {
                _executeCallback = executeCallback;
                _undoCallback = undoCallback;
            }

            public void Execute()
            {
                _executeCallback();
            }

            public void Undo()
            {
                _undoCallback();
            }
        }

        [Test(Description = "Makes sure executing and undoing a CompositeCommand correctly executes and undos the contained child commands.")]
        public void TestExecuteUndo()
        {
            var executeCalls = new List<int>(3);
            var undoCalls = new List<int>(3);
            var command = new CompositeCommand(
                new MockCommand(() => executeCalls.Add(0), () => undoCalls.Add(0)),
                new MockCommand(() => executeCalls.Add(1), () => undoCalls.Add(1)),
                new MockCommand(() => executeCalls.Add(2), () => undoCalls.Add(2)));

            command.Execute();
            executeCalls.Should().Equal(new[] {0, 1, 2}, because: "Child commands should be executed in ascending order");

            command.Undo();
            undoCalls.Should().Equal(new[] {2, 1, 0}, because: "Child commands should be undone in descending order");
        }

        [Test(Description = "Makes sure exceptions while executing cause rollbacks to occur.")]
        public void TestExecuteRollback()
        {
            var executeCalls = new List<int>(3);
            var undoCalls = new List<int>(3);
            var command = new CompositeCommand(
                new MockCommand(() => executeCalls.Add(0), () => undoCalls.Add(0)),
                new MockCommand(() => executeCalls.Add(1), () => undoCalls.Add(1)),
                new MockCommand(() => { throw new OperationCanceledException(); }, () => undoCalls.Add(2)));

            Assert.Throws<OperationCanceledException>(command.Execute, "Exceptions should be passed through after rollback");
            executeCalls.Should().Equal(new[] {0, 1}, because: "After an exception the rest of the commands should not be executed");
            undoCalls.Should().Equal(new[] {1, 0}, because: "After an exception all successful executions should be undone");
        }

        [Test(Description = "Makes sure exceptions while undoing cause rollbacks to occur.")]
        public void TestUndoRollback()
        {
            var executeCalls = new List<int>(3);
            var undoCalls = new List<int>(3);
            var command = new CompositeCommand(
                new MockCommand(() => executeCalls.Add(0), () => { throw new OperationCanceledException(); }),
                new MockCommand(() => executeCalls.Add(1), () => undoCalls.Add(1)),
                new MockCommand(() => executeCalls.Add(2), () => undoCalls.Add(2)));

            command.Execute();
            executeCalls.Should().Equal(new[] {0, 1, 2}, because: "Child commands should be executed in ascending order");

            executeCalls.Clear();
            Assert.Throws<OperationCanceledException>(command.Undo, "Exceptions should be passed through after rollback");
            undoCalls.Should().Equal(new[] {2, 1}, because: "After an exception the rest of the undoes should not be performed");
            executeCalls.Should().Equal(new[] {1, 2}, because: "After an exception all successful undoes should be re-executed");
        }

        [Test(Description = "Makes sure a correct order of calling Execute() and Undo() is enforced.")]
        public void TestWrongOrder()
        {
            var command = new CompositeCommand();
            Assert.Throws<InvalidOperationException>(command.Undo, "Should not allow Undo before Execute");

            command.Execute();
            Assert.Throws<InvalidOperationException>(command.Execute, "Should not allow two Executes without an Undo in between");
        }
    }
}
