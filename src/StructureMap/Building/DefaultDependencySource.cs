﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace StructureMap.Building
{
    public class DefaultDependencySource : IDependencySource
    {
        private readonly Type _dependencyType;

        public static MethodInfo SessionMethod =
            typeof (IContext).GetMethods()
                .FirstOrDefault(x => x.Name == "GetInstance" && x.IsGenericMethod && x.GetParameters().Count() == 0);


        public DefaultDependencySource(Type dependencyType)
        {
            _dependencyType = dependencyType;
        }

        public Type DependencyType
        {
            get { return _dependencyType; }
        }

        public Type ReturnedType
        {
            get
            {
                return _dependencyType;
            }
        }

        public string Description { get; private set; }

        public Expression ToExpression(ParameterExpression session)
        {
            return Expression.Call(session, SessionMethod.MakeGenericMethod(_dependencyType));
        }
    }
}