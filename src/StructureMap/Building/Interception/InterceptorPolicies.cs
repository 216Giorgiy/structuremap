﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace StructureMap.Building.Interception
{
    public class InterceptorPolicies
    {
        private readonly IList<IInterceptorPolicy> _policies = new List<IInterceptorPolicy>();

        public void Add(IInterceptorPolicy policy)
        {
            _policies.Fill(policy);
        }

        public IList<IInterceptorPolicy> Policies
        {
            get { return _policies; }
        }

        public IEnumerable<IInterceptor> SelectInterceptors(Type returnedType)
        {
            return _policies.SelectMany(x => x.DetermineInterceptors(returnedType));
        } 
    }
}