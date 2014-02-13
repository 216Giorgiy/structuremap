﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using StructureMap.Building;
using StructureMap.Building.Interception;
using StructureMap.Pipeline;
using StructureMap.TypeRules;

namespace StructureMap.Diagnostics
{
    public class BuildPlanVisualizer : IBuildPlanVisitor
    {
        private readonly IPipelineGraph _pipeline;
        private readonly TextTreeWriter _writer;
        private readonly int _maxLevels;

        public BuildPlanVisualizer(IPipelineGraph pipeline, bool deep = false, int levels = 0)
        {
            _pipeline = pipeline;
            _writer = new TextTreeWriter();

            _maxLevels = deep ? int.MaxValue : levels;
        }

        public int MaxLevels
        {
            get { return _maxLevels; }
        }

        public void Constructor(ConstructorStep constructor)
        {
            _writer.Line(constructor.Description);
            if (constructor.Arguments.Any())
            {
                _writer.StartSection<OutlineWithBars>();

                constructor.Arguments.Each(arg => {
                    var depVisualizer = new DependencyVisualizer(arg.Title, _writer);
                    arg.Dependency.AcceptVisitor(depVisualizer);
                });

                _writer.EndSection();
            }

        }


        public void Setter(Setter setter)
        {
            var depVisualizer = new DependencyVisualizer(setter.Title, _writer);
            setter.AssignedValue.AcceptVisitor(depVisualizer);
        }

        public void Activator(IInterceptor interceptor)
        {
            _writer.Line(interceptor.Description);
        }

        public void Decorator(IInterceptor interceptor)
        {
            throw new NotImplementedException();
        }

        private readonly Stack<Instance> _instanceStack = new Stack<Instance>(); 

        public void Instance(Type pluginType, Instance instance)
        {
            if (_instanceStack.Any() && _instanceStack.Peek() == instance) return;

            if (_instanceStack.Contains(instance))
            {
                _writer.Line("Bi-Directional Relationship Detected w/ Instance {0}, PluginType {1}", instance.Description, pluginType.GetTypeName());
            }

            if (_writer.LineCount > 0)
            {
                _writer.BlankLines(3);
            }

            _instanceStack.Push(instance);


            _writer.Line("Build Plan for Instance {0}", instance.Description, instance.Name);
            if (pluginType != null) _writer.Line("PluginType: " + pluginType.GetFullName());
            _writer.Line("Lifecycle: " + (instance.Lifecycle ?? Lifecycles.Transient).Description);

            var plan = instance.ResolveBuildPlan(pluginType, _pipeline.Policies);

            plan.AcceptVisitor(this);
            _instanceStack.Pop();
        }

        public void InnerBuilder(IDependencySource inner)
        {
            _writer.Line(inner.Description);
        }

        public void Write(TextWriter writer)
        {
            _writer.WriteAll(writer);
        }
    }


    public class DependencyVisualizer : IDependencyVisitor
    {
        private readonly string _title;
        private readonly TextTreeWriter _writer;

        public DependencyVisualizer(string title, TextTreeWriter writer)
        {
            _title = title;
            _writer = writer;
        }

        private void write(string text)
        {
            _writer.Line(_title + text);
        }

        public void Constant(Constant constant)
        {
            Dependency(constant);
        }

        public void Default(Type pluginType)
        {
            write("**Default**");
        }

        public void Referenced(ReferencedDependencySource source)
        {
            write("Instance named '{0}'".ToFormat(source.Name));
        }

        public void InlineEnumerable(IEnumerableDependencySource source)
        {
            throw new NotImplementedException();
        }

        public void AllPossibleOf(Type pluginType)
        {
            write("All registered Instances of " + pluginType.GetFullName());
        }

        public void Concrete(ConcreteBuild build)
        {
            throw new NotImplementedException();
        }

        public void Lifecycled(LifecycleDependencySource source)
        {
            throw new NotImplementedException();
        }

        public void Dependency(IDependencySource source)
        {
            write(source.Description);
        }

        public void Problem(DependencyProblem problem)
        {
            write(problem.Message);
        }
    }
}