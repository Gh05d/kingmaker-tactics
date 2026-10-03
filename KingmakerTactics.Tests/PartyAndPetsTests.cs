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

        // Kingmaker's Player.Party may already list a pet whose master is in the party
        // (Player.AddCharacterToLists checks Descriptor.IsPet + Master) — no duplicates.
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
