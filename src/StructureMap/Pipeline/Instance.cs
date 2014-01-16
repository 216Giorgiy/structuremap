using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using StructureMap.Building;
using StructureMap.Building.Interception;
using StructureMap.Diagnostics;
using StructureMap.Graph;
using StructureMap.TypeRules;

namespace StructureMap.Pipeline
{
    public abstract class Instance : HasLifecycle, IDescribed
    {
        private readonly string _originalName;
        private string _name = Guid.NewGuid().ToString();


        private PluginFamily _parent;

        private readonly IList<IInterceptor> _interceptors = new List<IInterceptor>();

        /// <summary>
        /// Add an interceptor to only this Instance
        /// </summary>
        /// <param name="interceptor"></param>
        public void AddInterceptor(IInterceptor interceptor)
        {
            if (ReturnedType != null && !ReturnedType.CanBeCastTo(interceptor.Accepts))
            {
                throw new ArgumentOutOfRangeException("ReturnedType {0} cannot be cast to the Interceptor Accepts type {1}".ToFormat(ReturnedType.GetFullName(), interceptor.Accepts.GetFullName()));
            }

            _interceptors.Add(interceptor);
        }

        protected Instance()
        {
            _originalName = _name;
        }

        /// <summary>
        /// Strategy for how this Instance would be built as
        /// an inline dependency in the parent Instance's
        /// "Build Plan"
        /// </summary>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        public abstract IDependencySource ToDependencySource(Type pluginType);

        /// <summary>
        /// Creates an IDependencySource that can be used to build the object
        /// represented by this Instance 
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="policies"></param>
        /// <returns></returns>
        public virtual IDependencySource ToBuilder(Type pluginType, Policies policies)
        {
            return ToDependencySource(pluginType);
        }

        [Obsolete("Try to eliminate the bi-directional dependency here.")]
        public PluginFamily Parent
        {
            get { return _parent; }
            internal set
            {
                _parent = value;
                scopedParent = _parent;
            }
        }

        public IEnumerable<IInterceptor> Interceptors
        {
            get { return _interceptors; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public abstract string Description { get; }

        public InstanceToken CreateToken()
        {
            return new InstanceToken(Name, Description);
        }

        /// <summary>
        /// The known .Net Type built by this Instance.  May be null when indeterminate.
        /// </summary>
        public abstract Type ReturnedType { get; }

        /// <summary>
        /// Does this Instance have a user-defined name?
        /// </summary>
        /// <returns></returns>
        public bool HasExplicitName()
        {
            return _name != _originalName;
        }

        /// <summary>
        /// Return the closed type value for this Instance
        /// when starting from an open generic type
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public virtual Instance CloseType(Type[] types)
        {
            return this;
        }

        private readonly object _buildLock = new object();
        private IBuildPlan _plan;

        /// <summary>
        /// Resolves the IBuildPlan for this Instance.  The result is remembered
        /// for subsequent requests
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="policies"></param>
        /// <returns></returns>
        public IBuildPlan ResolveBuildPlan(Type pluginType, Policies policies)
        {
            if (_plan == null)
            {
                lock (_buildLock)
                {
                    if (_plan == null)
                    {
                        _plan = buildPlan(pluginType, policies);
                    }
                }
            }

            return _plan;
        }

        /// <summary>
        /// Clears out any remembered IBuildPlan for this Instance
        /// </summary>
        public void ClearBuildPlan()
        {
            lock (_buildLock)
            {
                _plan = null;
            }
        }

        private IBuildPlan buildPlan(Type pluginType, Policies policies)
        {
            var builderSource = ToBuilder(pluginType, policies);
            var interceptors = policies.Interceptors.SelectInterceptors(ReturnedType ?? pluginType).Union(_interceptors);
            return new BuildPlan(pluginType, this, builderSource, interceptors);
        }

        /// <summary>
        /// Creates a hash that is unique for this Instance and PluginType combination
        /// </summary>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        public int InstanceKey(Type pluginType)
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0)*397) ^
                       (pluginType != null ? pluginType.AssemblyQualifiedName.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Is this Instance created uniquely upon each request?
        /// </summary>
        /// <returns></returns>
        public bool IsUnique()
        {
            return Lifecycle is UniquePerRequestLifecycle;
        }

    }
}