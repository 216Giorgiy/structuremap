﻿using System;
using System.Linq.Expressions;

namespace StructureMap.Building
{
    public class Constant : IBuildStep
    {
        private readonly Type _argumentType;
        private readonly object _value;

        public static Constant For<T>(T value)
        {
            return new Constant(typeof(T), value);
        }

        public Constant(Type argumentType, object value)
        {
            _argumentType = argumentType;
            _value = value;
        }

        public string Description { get; private set; }
        public Expression ToExpression()
        {
            return Expression.Constant(_value, _argumentType);
        }
    }
}