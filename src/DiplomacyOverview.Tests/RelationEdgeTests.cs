using System;
using System.Collections.Generic;
using DiplomacyOverview.Core;
using Xunit;

namespace DiplomacyOverview.Tests
{
    public class RelationEdgeTests
    {
        [Fact]
        public void Create_CanonicalizesEndpointOrder_RegardlessOfArgumentOrder()
        {
            var ab = RelationEdge.Create("A", "B", RelationKind.War);
            var ba = RelationEdge.Create("B", "A", RelationKind.War);

            Assert.Equal(ab.NodeA, ba.NodeA);
            Assert.Equal(ab.NodeB, ba.NodeB);
            Assert.Equal("A", ab.NodeA);
            Assert.Equal("B", ab.NodeB);
        }

        [Fact]
        public void Create_AlreadyOrderedArguments_KeepsOrder()
        {
            var edge = RelationEdge.Create("Aserai", "Vlandia", RelationKind.Alliance);

            Assert.Equal("Aserai", edge.NodeA);
            Assert.Equal("Vlandia", edge.NodeB);
        }

        [Fact]
        public void Create_SameNodeTwice_Throws()
        {
            Assert.Throws<ArgumentException>(() => RelationEdge.Create("A", "A", RelationKind.War));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Create_NullOrEmptyFirstNode_Throws(string? a)
        {
            Assert.Throws<ArgumentException>(() => RelationEdge.Create(a!, "B", RelationKind.War));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Create_NullOrEmptySecondNode_Throws(string? b)
        {
            Assert.Throws<ArgumentException>(() => RelationEdge.Create("A", b!, RelationKind.War));
        }

        [Fact]
        public void Create_NoneKind_Throws()
        {
            Assert.Throws<ArgumentException>(() => RelationEdge.Create("A", "B", RelationKind.None));
        }

        [Fact]
        public void Create_MultiFlagKind_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                RelationEdge.Create("A", "B", RelationKind.War | RelationKind.Alliance));
        }

        [Fact]
        public void Create_AllFlagsCombined_Throws()
        {
            var everything = RelationKind.War | RelationKind.Alliance | RelationKind.NonAggressionPact | RelationKind.CallToWar;

            Assert.Throws<ArgumentException>(() => RelationEdge.Create("A", "B", everything));
        }

        [Theory]
        [InlineData(RelationKind.War)]
        [InlineData(RelationKind.Alliance)]
        [InlineData(RelationKind.NonAggressionPact)]
        [InlineData(RelationKind.CallToWar)]
        public void Create_EachSingleFlag_Succeeds(RelationKind kind)
        {
            var edge = RelationEdge.Create("A", "B", kind);

            Assert.Equal(kind, edge.Kind);
        }

        [Fact]
        public void Create_WithoutDetails_DetailsIsNull()
        {
            var edge = RelationEdge.Create("A", "B", RelationKind.War);

            Assert.Null(edge.Details);
        }

        [Fact]
        public void Create_WithDetails_PreservesPayload()
        {
            var details = new Dictionary<string, string> { ["expires"] = "12" };

            var edge = RelationEdge.Create("A", "B", RelationKind.NonAggressionPact, details);

            Assert.NotNull(edge.Details);
            Assert.Equal("12", edge.Details!["expires"]);
        }
    }
}
