﻿using System;
using System.Linq.Expressions;
using NUnit.Framework;
using StructureMap.Building;
using StructureMap.Building.Interception;

namespace StructureMap.Testing.Building.Interception
{
    [TestFixture]
    public class FuncInterceptorTester
    {
        private FuncInterceptor<ITarget> theInterceptor;

        [SetUp]
        public void SetUp()
        {
            theInterceptor = new FuncInterceptor<ITarget>(x => new DecoratedTarget(x));
        }

        [Test]
        public void role_is_decorator()
        {
            theInterceptor.Role.ShouldEqual(InterceptorRole.Decorates);
        }

        [Test]
        public void accepts_type()
        {
            theInterceptor.Accepts.ShouldEqual(typeof (ITarget));
        }

        [Test]
        public void return_type()
        {
            theInterceptor.Returns.ShouldEqual(typeof (ITarget));
        }

        [Test]
        public void description_comes_from_the_body()
        {
            theInterceptor.Description.ShouldEqual("new DecoratedTarget(ITarget)");
        }

        [Test]
        public void description_when_uses_IContext_too()
        {
            theInterceptor = new FuncInterceptor<ITarget>((c, t) => new ContextKeepingTarget(c, t));

            theInterceptor.Description.ShouldEqual("new ContextKeepingTarget(IBuildSession, ITarget)");
        }

        [Test]
        public void compile_and_use_by_itself_not_using_IBuildSession()
        {
            var variable = Expression.Variable(typeof(ITarget), "target");

            var expression = theInterceptor.ToExpression(Parameters.Session, variable);

            var lambdaType = typeof(Func<ITarget, ITarget>);
            var lambda = Expression.Lambda(lambdaType, expression, variable);

            var func = lambda.Compile().As<Func<ITarget, ITarget>>();

            var target = new Target();
            var decorated = func(target);

            decorated.ShouldBeOfType<DecoratedTarget>()
                .Inner.ShouldBeTheSameAs(target);

        }


        [Test]
        public void compile_and_use_by_itself_using_IBuildSession()
        {
            theInterceptor = new FuncInterceptor<ITarget>((c, t) => new ContextKeepingTarget(c, t));

            var variable = Expression.Variable(typeof(ITarget), "target");

            var expression = theInterceptor.ToExpression(Parameters.Session, variable);

            var lambdaType = typeof(Func<IBuildSession, ITarget, ITarget>);
            var lambda = Expression.Lambda(lambdaType, expression, Parameters.Session, variable);

            var func = lambda.Compile().As<Func<IBuildSession, ITarget, ITarget>>();

            var target = new Target();
            var session = new FakeBuildSession();
            var decorated = func(session, target).ShouldBeOfType<ContextKeepingTarget>();

            decorated
                .Inner.ShouldBeTheSameAs(target);

            decorated.Context.ShouldBeTheSameAs(session);

        }


    }

    public class DecoratedTarget : ITarget
    {
        private readonly ITarget _inner;

        public DecoratedTarget(ITarget inner)
        {
            _inner = inner;
        }

        public ITarget Inner
        {
            get { return _inner; }
        }

        public void Activate()
        {
            
        }


    }

    public class ContextKeepingTarget : ITarget
    {
        private readonly IContext _context;
        private readonly ITarget _inner;

        public ContextKeepingTarget(IBuildSession context, ITarget inner)
        {
            _context = context;
            _inner = inner;
        }

        public IContext Context
        {
            get { return _context; }
        }

        public ITarget Inner
        {
            get { return _inner; }
        }

        public void Activate()
        {
            throw new NotImplementedException();
        }
    }
}