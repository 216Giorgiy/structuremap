﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using StructureMap.Graph;

namespace StructureMap.Building
{
    public class ConcreteBuild : IBuildStep
    {
        private readonly Type _concreteType;
        private readonly ConstructorStep _constructor;
        private readonly IList<Setter> _setters = new List<Setter>();

        public ConcreteBuild(Type concreteType) : this(concreteType, new Plugin(concreteType).GetConstructor())
        {
            
        }

        public ConcreteBuild(Type concreteType, ConstructorStep constructor)
        {
            _concreteType = concreteType;
            _constructor = constructor;
        }

        protected ConcreteBuild(Type concreteType, ConstructorInfo constructor) : this(concreteType, new ConstructorStep(constructor))
        {
        }

        public Type ConcreteType
        {
            get { return _concreteType; }
        }

        public void Add(Setter setter)
        {
            _setters.Add(setter);
        }

        public void Add(MemberInfo member, IBuildStep value)
        {
            _setters.Add(new Setter(member, value));
        }

        public ConstructorStep Constructor
        {
            get { return _constructor; }
        }


        public string Description { get; private set; }


        public Delegate ToDelegate()
        {
            var inner = ToExpression();

            var lambdaType = typeof (Func<,>).MakeGenericType(typeof (IContext), _concreteType);
            var argument = Expression.Parameter(typeof (IContext), "c");
            
            var lambda = Expression.Lambda(lambdaType, inner, argument);

            return lambda.Compile();
        }

        public Func<IContext, T> ToDelegate<T>()
        {
            return ToDelegate() as Func<IContext, T>;
        } 

        public Expression ToExpression()
        {
            var newExpr = _constructor.ToExpression();
            
            if (!_setters.Any())
            {
                return newExpr;
            }
            
            return Expression.MemberInit(newExpr, _setters.Select(x => x.ToBinding()));
        }

        
    }

    public class ConcreteBuild<T> : ConcreteBuild
    {
        public static ConcreteBuild<T> For(Expression<Func<T>> expression)
        {
            var finder = new ConstructorFinderVisitor();
            finder.Visit(expression);

            ConstructorInfo ctor = finder.Constructor;

            return new ConcreteBuild<T>(ctor);
        }

        public ConcreteBuild(ConstructorInfo constructor)
            : base(typeof(T), constructor)
        {
        }

        public ConcreteBuild()
            : base(typeof(T))
        {
        }

        public void Set<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            var member = ReflectionHelper.GetMember(expression);

            Add(new Setter(member, Constant.For(value)));
        }
    }
}