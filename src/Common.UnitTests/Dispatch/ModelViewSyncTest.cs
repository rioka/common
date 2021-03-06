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
using System.Linq;
using FluentAssertions;
using NanoByte.Common.Collections;
using NUnit.Framework;

namespace NanoByte.Common.Dispatch
{
    /// <summary>
    /// Contains test methods for <see cref="ModelViewSync{TModel,TView}"/>.
    /// </summary>
    [TestFixture]
    public class ModelViewSyncTest
    {
        #region Mock classes
        private abstract class ModelBase : IChangeNotify<ModelBase>
        {
            private string _id;

            public string ID
            {
                get { return _id; }
                set
                {
                    _id = value;
                    Changed?.Invoke(this);
                }
            }

            public void Rebuild()
            {
                ChangedRebuild?.Invoke(this);
            }

            public event Action<ModelBase> Changed;
            public event Action<ModelBase> ChangedRebuild;
        }

        private class SpecificModel : ModelBase
        {}

        private abstract class ViewBase : IDisposable
        {
            public string ID { get; set; }

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private class SpecificView : ViewBase
        {}
        #endregion

        [Test]
        public void Initialize()
        {
            var model = new MonitoredCollection<ModelBase> {new SpecificModel {ID = "abc"}};
            var view = new List<ViewBase>();
            using (var sync = new ModelViewSync<ModelBase, ViewBase>(model, view))
            {
                sync.Register((SpecificModel element) => new SpecificView(), (element, representation) => representation.ID = element.ID);
                sync.Initialize();

                view[0].Should().BeOfType<SpecificView>();
                view[0].ID.Should().Be("abc");
            }
        }

        [Test]
        public void Lookup()
        {
            var model = new MonitoredCollection<ModelBase> {new SpecificModel {ID = "abc"}};
            var view = new List<ViewBase>();
            using (var sync = new ModelViewSync<ModelBase, ViewBase>(model, view))
            {
                sync.Register((SpecificModel element) => new SpecificView(), (element, representation) => representation.ID = element.ID);
                sync.Initialize();

                sync.Lookup(view[0]).Should().BeSameAs(model[0]);
                sync.Invoking(x => x.Lookup(new SpecificView()))
                    .ShouldThrow<KeyNotFoundException>();
            }
        }

        [Test]
        public void Representations()
        {
            var model = new MonitoredCollection<ModelBase> {new SpecificModel {ID = "abc"}};
            var view = new List<ViewBase>();
            using (var sync = new ModelViewSync<ModelBase, ViewBase>(model, view))
            {
                sync.Register((SpecificModel element) => new SpecificView(), (element, representation) => representation.ID = element.ID);
                sync.Initialize();

                sync.Representations.First().Should().BeSameAs(view[0]);
            }
        }

        [Test]
        public void MonitoringAdd()
        {
            var model = new MonitoredCollection<ModelBase>();
            var view = new List<ViewBase>();
            using (var sync = new ModelViewSync<ModelBase, ViewBase>(model, view))
            {
                sync.Register((SpecificModel element) => new SpecificView(), (element, representation) => representation.ID = element.ID);
                sync.Initialize();

                model.Add(new SpecificModel {ID = "abc"});

                view[0].Should().BeOfType<SpecificView>();
                view[0].ID.Should().Be("abc");
            }
        }

        [Test]
        public void MonitoringRemove()
        {
            var model = new MonitoredCollection<ModelBase> {new SpecificModel {ID = "abc"}};
            var view = new List<ViewBase>();
            using (var sync = new ModelViewSync<ModelBase, ViewBase>(model, view))
            {
                sync.Register((SpecificModel element) => new SpecificView(), (element, representation) => representation.ID = element.ID);
                sync.Initialize();

                var originalRepresentation = view[0];
                model.RemoveAt(0);

                view.Should().BeEmpty();
                originalRepresentation.Disposed.Should().BeTrue();
            }
        }

        [Test]
        public void MonitoringChanged()
        {
            var model = new MonitoredCollection<ModelBase> {new SpecificModel {ID = "abc"}};
            var view = new List<ViewBase>();
            using (var sync = new ModelViewSync<ModelBase, ViewBase>(model, view))
            {
                sync.Register((SpecificModel element) => new SpecificView(), (element, representation) => representation.ID = element.ID);
                sync.Initialize();

                model[0].ID = "xyz";

                view[0].ID.Should().Be("xyz");
            }
        }

        [Test]
        public void MonitoringChangedRebuild()
        {
            var model = new MonitoredCollection<ModelBase> {new SpecificModel {ID = "abc"}};
            var view = new List<ViewBase>();
            using (var sync = new ModelViewSync<ModelBase, ViewBase>(model, view))
            {
                sync.Register((SpecificModel element) => new SpecificView(), (element, representation) => representation.ID = element.ID);
                sync.Initialize();

                var originalRepresentation = view[0];
                model[0].Rebuild();
                view[0].Should().NotBeSameAs(originalRepresentation);
            }
        }

        [Test]
        public void Dispose()
        {
            var model = new MonitoredCollection<ModelBase> {new SpecificModel {ID = "abc"}};
            var view = new List<ViewBase>();
            using (var sync = new ModelViewSync<ModelBase, ViewBase>(model, view))
            {
                sync.Register((SpecificModel element) => new SpecificView(), (element, representation) => representation.ID = element.ID);
                sync.Initialize();
            }
            view.Should().BeEmpty();
        }
    }
}
