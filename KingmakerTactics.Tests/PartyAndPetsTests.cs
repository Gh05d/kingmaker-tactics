using System.Collections.Generic;
using KingmakerTactics.Compatibility;
using Xunit;

namespace KingmakerTactics.Tests {
    public class PartyAndPetsTests {
        class U { public string N; public U Pet; }

        [Fact]
        public void IncludesEachMemberThenItsPet() {
            var dog = new U { N = "dog" };
            var a = new U { N = "a", Pet = dog };
            var b = new U { N = "b" };
            var r = KingmakerShim.MergePartyAndPets(new[] { a, b }, u => u.Pet);
            Assert.Equal(new[] { "a", "dog", "b" }, r.ConvertAll(u => u.N).ToArray());
        }

        // Kingmaker's Player.Party never lists pets (AddCharacterToLists, IL); the merge still
        // must not duplicate one if a caller ever passes a list that does.
        [Fact]
        public void PetAlreadyInPartyIsNotDuplicated() {
            var dog = new U { N = "dog" };
            var a = new U { N = "a", Pet = dog };
            var r = KingmakerShim.MergePartyAndPets(new[] { a, dog }, u => u.Pet);
            Assert.Equal(2, r.Count);
        }

        [Fact]
        public void NullMembersAreSkipped() {
            var r = KingmakerShim.MergePartyAndPets(new U[] { null, new U { N = "a" } }, u => u.Pet);
            Assert.Single(r);
        }
    }
}
