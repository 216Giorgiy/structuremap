﻿using System.Diagnostics;
using System.Security.Cryptography;
using NUnit.Framework;
using StructureMap.Query;
using StructureMap.Testing.Widget;

namespace StructureMap.Testing.Diagnostics
{
    [TestFixture]
    public class when_building_a_build_plan_for_a_single_instance
    {
        private string theDescription;

        [SetUp]
        public void SetUp()
        {
            var container = Container.For<VisualizationRegistry>();
            theDescription = container.Model.For<IDevice>()
                .Default.DescribeBuildPlan();
        }

        [Test]
        public void should_have_a_title()
        {
            theDescription.ShouldContain("Build Plan for Instance StructureMap.Testing.Diagnostics.DefaultDevice");
        }

        [Test]
        public void should_specify_the_plugin_type()
        {
            theDescription.ShouldContain("PluginType: StructureMap.Testing.Diagnostics.IDevice");
        }

        [Test]
        public void should_display_the_lifecycle()
        {
            theDescription.ShouldContain("Lifecycle: Transient");
        }
    }


    [TestFixture]
    public class BuildPlanVisualizationSmokeTester
    {
        private readonly IContainer theContainer = Container.For<VisualizationRegistry>();

        [Test]
        public void no_arg_constructor()
        {
            var description = theContainer.Model.For<IDevice>()
                .Default.DescribeBuildPlan();

            description.ShouldContain("new DefaultDevice()");
        }

        [Test]
        public void simple_build_by_lambda()
        {
            var description = theContainer.Model.For<IDevice>()
                .Find("A")
                .DescribeBuildPlan();

            description.ShouldContain("Lambda: new ADevice()");
        }

        [Test]
        public void simple_build_by_object()
        {
            var description = theContainer.Model.For<IDevice>()
                .Find("B")
                .DescribeBuildPlan();

            

            description.ShouldContain("Value: StructureMap.Testing.Diagnostics.BDevice");
        }

        [Test]
        public void single_ctor_arg_with_constant()
        {
            var description = theContainer.Model
                .Find<Rule>("Red")
                .DescribeBuildPlan();

            description.ShouldContain("┗ String color = Value: Red");
        }

        [Test]
        public void multiple_ctor_args_with_constants()
        {
            var description = theContainer.Model
                .Find<IDevice>("GoodSimpleArgs")
                .DescribeBuildPlan();

            Debug.WriteLine(description);

            description.ShouldContain("┣ String color = Value: Blue");
            description.ShouldContain("┣ String direction = Value: North");
            description.ShouldContain("┗ String name = Value: Declan");
        }

        [Test]
        public void multiple_ctor_args_and_setters_with_constants()
        {
            var description = theContainer.Model
                .Find<IDevice>("MixedCtorAndSetter")
                .DescribeBuildPlan();

            Debug.WriteLine(description);

            description.ShouldContain("┣ String color = Value: Blue");
            description.ShouldContain("┣ String direction = Value: North");
            description.ShouldContain("┗ String name = Value: Declan");
        }

        /*
         * TODO's

         * setters
         * problems
         * inline dependencies
         * default dependencies
         * inline lambda
         * inline reference
         * complex lambda?
         */
    }



}