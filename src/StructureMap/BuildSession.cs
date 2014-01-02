using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap.Building;
using StructureMap.Graph;
using StructureMap.Pipeline;

namespace StructureMap
{
    public class BuildSession : IBuildSession
    {
        private readonly IPipelineGraph _pipelineGraph;
        private readonly ISessionCache _sessionCache;

        public BuildSession(IPipelineGraph pipelineGraph, string requestedName = null, ExplicitArguments args = null)
        {
            _pipelineGraph = pipelineGraph;

            _sessionCache = new SessionCache(this, args);


            RequestedName = requestedName ?? Plugin.DEFAULT;
        }

        protected IPipelineGraph pipelineGraph
        {
            get { return _pipelineGraph; }
        }

        public string RequestedName { get; set; }

        public void BuildUp(object target)
        {
            if (target == null) throw new ArgumentNullException("target");

            var pluggedType = target.GetType();
            var instance = _pipelineGraph.GetDefault(pluggedType) as IConfiguredInstance
                                           ?? new ConfiguredInstance(pluggedType);

            var plan = ConcreteType.BuildUpPlan(pluggedType, instance.Dependencies, _pipelineGraph.Outer.Policies);


            plan.BuildUp(this, target);
        }

        public T GetInstance<T>()
        {
            return (T) GetInstance(typeof (T));
        }

        public object GetInstance(Type pluginType)
        {
            return _sessionCache.GetDefault(pluginType, _pipelineGraph);
        }

        public T GetInstance<T>(string name)
        {
            return (T) CreateInstance(typeof (T), name);
        }

        public object GetInstance(Type pluginType, string name)
        {
            return CreateInstance(pluginType, name);
        }

        public T TryGetInstance<T>() where T : class
        {
            return (T) TryGetInstance(typeof (T));
        }

        public T TryGetInstance<T>(string name) where T : class
        {
            return (T) TryGetInstance(typeof (T), name);
        }

        public object TryGetInstance(Type pluginType)
        {
            return _sessionCache.TryGetDefault(pluginType, _pipelineGraph);
        }

        public object TryGetInstance(Type pluginType, string name)
        {
            return _pipelineGraph.HasInstance(pluginType, name) ? ((IContext) this).GetInstance(pluginType, name) : null;
        }

        public IEnumerable<T> All<T>() where T : class
        {
            return _sessionCache.All<T>();
        }

        public object ResolveFromLifecycle(Type pluginType, Instance instance)
        {
            var cache = instance.Lifecycle.FindCache(_pipelineGraph);
            return cache.Get(pluginType, instance, this);
        }

        public object BuildNewInSession(Type pluginType, Instance instance)
        {
            object returnValue = instance.Build(pluginType, this);
            if (returnValue == null) return null;

            try
            {
                return _pipelineGraph.FindInterceptor(returnValue.GetType()).Process(returnValue, this);
            }
            catch (Exception e)
            {
                throw new StructureMapException(308, e, instance.Name, returnValue.GetType());
            }
        }

        public object BuildNewInOriginalContext(Type pluginType, Instance instance)
        {
            var session = new BuildSession(pipelineGraph.Root(), requestedName: instance.Name);
            return session.BuildNewInSession(pluginType, instance);
        }

        public IEnumerable<T> GetAllInstances<T>()
        {
            return _pipelineGraph.GetAllInstances(typeof (T)).Select(x => (T) FindObject(typeof (T), x)).ToArray();
        }

        public IEnumerable<object> GetAllInstances(Type pluginType)
        {
            IEnumerable<Instance> allInstances = _pipelineGraph.GetAllInstances(pluginType);
            return allInstances.Select(x => FindObject(pluginType, x)).ToArray();
        }

        public static BuildSession ForPluginGraph(PluginGraph graph, ExplicitArguments args = null)
        {
            var pipeline = new RootPipelineGraph(graph);
            return new BuildSession(pipeline, args: args);
        }

        public static BuildSession Empty(ExplicitArguments args = null)
        {
            return ForPluginGraph(new PluginGraph(), args);
        }

        public virtual object CreateInstance(Type pluginType, string name)
        {
            Instance instance = _pipelineGraph.FindInstance(pluginType, name);
            if (instance == null)
            {
                throw new StructureMapException(200, name, pluginType.FullName);
            }

            return FindObject(pluginType, instance);
        }

        // This is where all Creation happens
        public virtual object FindObject(Type pluginType, Instance instance)
        {
            return _sessionCache.GetObject(pluginType, instance);
        }

        public Policies Policies
        {
            get
            {
                return _pipelineGraph.Outer.Policies;
            }
        }
    }
}