﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using NUnit.Framework;
using StructureMap.Testing.Widget;

namespace StructureMap.Testing.Acceptance
{
    /*
     * TODO

     * 12.) Use open generics
     * 13.) custom interception policy
     */

    [TestFixture]
    public class interception_acceptance_tests
    {
        [Test]
        public void activate_by_action()
        {
            var container = new Container(x => {
                x.For<IWidget>().Use<AWidget>();
                x.For<Activateable>()
                    .OnCreationForAll("Mark the object as activated", o => o.Activated = true);
            });

            container.GetInstance<IWidget>()
                .ShouldBeOfType<AWidget>()
                .Activated.ShouldBeTrue();
        }

        [Test]
        public void activate_by_expression_action()
        {
            var container = new Container(x =>
            {
                x.For<IWidget>().Use<AWidget>();
                x.For<Activateable>()
                    .OnCreationForAll(o => o.Activate());
            });

            container.GetInstance<IWidget>()
                .ShouldBeOfType<AWidget>()
                .Activated.ShouldBeTrue();
        }

        [Test]
        public void activate_by_action_using_IContext()
        {
            var container = new Container(x =>
            {
                x.For<IWidget>().Use<AWidget>();
                x.For<IWidget>()
                    .OnCreationForAll("just keeping them", (c, w) => c.GetInstance<WidgetKeeper>().Kept.Add(w));    

                x.ForSingletonOf<WidgetKeeper>();
            });

            var widget = container.GetInstance<IWidget>();
            container.GetInstance<WidgetKeeper>()
                .Kept.Single()
                .ShouldBeTheSameAs(widget);
        }

        [Test]
        public void activate_by_expression_and_IContext()
        {
            var container = new Container(x =>
            {
                x.For<IWidget>().Use<AWidget>();
                x.For<IWidget>()
                    .OnCreationForAll((c, w) => c.GetInstance<WidgetKeeper>().Keep(w));

                x.ForSingletonOf<WidgetKeeper>();
            });

            var widget = container.GetInstance<IWidget>();
            container.GetInstance<WidgetKeeper>()
                .Kept.Single()
                .ShouldBeTheSameAs(widget);
        }

        [Test]
        public void decorate_with_type()
        {
            var container = new Container(x => {
                x.For<IWidget>().DecorateAllWith<WidgetHolder>();
                x.For<IWidget>().Use<AWidget>();
            });

            container.GetInstance<IWidget>()
                .ShouldBeOfType<WidgetHolder>()
                .Inner
                .ShouldBeOfType<AWidget>();
        }

        [Test]
        public void decorate_with_type_and_inline_dependency()
        {
            var container = new Container(x =>
            {
                x.For<IWidget>().DecorateAllWith<NamedWidgetHolder>()
                    .Ctor<string>("name").Is("Frank Sinatra");

                x.For<IWidget>().Use<AWidget>();
            });

            var holder = container.GetInstance<IWidget>()
                .ShouldBeOfType<NamedWidgetHolder>();

            holder.Name.ShouldEqual("Frank Sinatra");
            holder.Inner.ShouldBeOfType<AWidget>();
        }

        [Test]
        public void decorate_with_additional_filter_on_instance()
        {
            var container = new Container(x =>
            {
                x.For<IWidget>()
                    .DecorateAllWith<WidgetHolder>(i => i.ReturnedType.Name.StartsWith("B"));

                x.For<IWidget>().AddInstances(widgets => {
                    widgets.Type<AWidget>().Named("A");
                    widgets.Type<BWidget>().Named("B");
                    widgets.Type<CWidget>().Named("C");
                });
            });

            container.GetInstance<IWidget>("A").ShouldBeOfType<AWidget>();
            container.GetInstance<IWidget>("B")
                .ShouldBeOfType<WidgetHolder>()
                .Inner.ShouldBeOfType<BWidget>();

            container.GetInstance<IWidget>("C").ShouldBeOfType<CWidget>();
        }

        [Test]
        public void decorate_by_func()
        {
            var container = new Container(x => {
                x.For<IWidget>().DecorateAllWith("Hold on to it", w => new WidgetHolder(w));
                x.For<IWidget>().Use<AWidget>();
            });

            container.GetInstance<IWidget>()
                .ShouldBeOfType<WidgetHolder>()
                .Inner
                .ShouldBeOfType<AWidget>();
        }

        [Test]
        public void decorate_by_expression()
        {
            var container = new Container(x =>
            {
                x.For<IWidget>().DecorateAllWith(w => new WidgetHolder(w));
                x.For<IWidget>().Use<AWidget>();
            });

            container.GetInstance<IWidget>()
                .ShouldBeOfType<WidgetHolder>()
                .Inner
                .ShouldBeOfType<AWidget>();
        }

        [Test]
        public void decorate_with_expression_and_IContext()
        {
            var container = new Container(x =>
            {
                x.For<IWidget>().DecorateAllWith((c,w) => c.GetInstance<WidgetWrapper>().Wrap(w));
                x.For<IWidget>().Use<AWidget>();
            });

            container.GetInstance<IWidget>()
                .ShouldBeOfType<WidgetDecorator>()
                .Inner
                .ShouldBeOfType<AWidget>();
        }

        [Test]
        public void decorate_with_func_and_IContext()
        {
            var container = new Container(x =>
            {
                x.For<IWidget>().DecorateAllWith("Using WidgetWrapper",(c, w) => {
                    return c.GetInstance<WidgetWrapper>().Wrap(w);
                });

                x.For<IWidget>().Use<AWidget>();
            });

            container.GetInstance<IWidget>()
                .ShouldBeOfType<WidgetDecorator>()
                .Inner
                .ShouldBeOfType<AWidget>();
        }
    }

    public abstract class Activateable
    {
        public bool Activated { get; set; }

        public void Activate()
        {
            Activated = true;
        }
    }

    public class WidgetKeeper
    {
        public readonly IList<IWidget> Kept = new List<IWidget>();

        public void Keep(IWidget widget)
        {
            Kept.Add(widget);
        }
    }


    public interface IWidget
    {
    }

    public class DefaultWidget : IWidget{}

    public class AWidget : Activateable, IWidget 
    {

    }

    public class BWidget : Activateable, IWidget 
    {

    }

    public class CWidget : Activateable, IWidget 
    {
        
    }

    public class WidgetHolder : IWidget
    {
        private readonly IWidget _inner;

        public WidgetHolder(IWidget inner)
        {
            _inner = inner;
        }

        public IWidget Inner
        {
            get { return _inner; }
        }
    }

    public class NamedWidgetHolder : WidgetHolder
    {
        private readonly string _name;

        public NamedWidgetHolder(string name, IWidget inner) : base(inner)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }
    }

    public class WidgetWrapper
    {
        public IWidget Wrap(IWidget widget)
        {
            return new WidgetDecorator(widget);
        }
    }

    public class WidgetDecorator : IWidget
    {
        private readonly IWidget _inner;

        public WidgetDecorator(IWidget inner)
        {
            _inner = inner;
        }

        public IWidget Inner
        {
            get { return _inner; }
        }
    }
}