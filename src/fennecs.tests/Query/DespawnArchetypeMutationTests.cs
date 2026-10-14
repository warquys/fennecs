// SPDX-License-Identifier: MIT

namespace fennecs.tests.Query;

/// <summary>
/// Despawning a relation target migrates the Entities that relate to it into a Signature they may
/// never have had, which creates an Archetype — and a Query tracks new Archetypes the moment they
/// appear. Any bulk despawn that enumerates its Archetypes must therefore not enumerate the live set.
/// </summary>
public class DespawnArchetypeMutationTests
{
    [Fact]
    public void Despawn_All_With_Relations_To_Two_Targets()
    {
        using var world = new World();

        var parent = world.Spawn();
        var otherParent = world.Spawn();

        // Losing one parent leaves {int >> otherParent}, a Signature no Archetype has yet.
        world.Spawn().Add(0, parent).Add(0, otherParent);

        world.All.Despawn();

        Assert.Equal(0, world.Count);
    }


    [Fact]
    public void Truncate_With_Relations_To_Two_Targets()
    {
        using var world = new World();

        var parent = world.Spawn();
        var otherParent = world.Spawn();

        for (var i = 0; i < 10; i++) world.Spawn().Add(i, parent).Add(i, otherParent);

        // Truncate makes one pass over the Archetypes it matched when it started. Entities that
        // migrate into a *new* Archetype during that pass are not in it, and survive — which is why
        // Despawn() loops. What matters here is that the pass completes instead of throwing.
        world.All.Truncate(0);

        Assert.True(world.Count < 12);

        world.All.Despawn();
        Assert.Equal(0, world.Count);
    }


    [Fact]
    public void Stream_Despawn_With_Relations_To_Two_Targets()
    {
        using var world = new World();

        // The targets carry a relation of their own, so the Stream matches them too — despawning
        // them from inside its loop is what reshapes the child into a brand new Archetype.
        var root = world.Spawn();
        var parent = world.Spawn().Add(1, root);
        var otherParent = world.Spawn().Add(1, root);

        for (var i = 0; i < 10; i++) world.Spawn().Add(i, parent).Add(i, otherParent);

        world.Query<int>(Match.Any).Stream().Despawn();

        // root, plus the ten children: having lost both relations they no longer match <int>,
        // so the Stream correctly stops covering them.
        Assert.Equal(11, world.Count);
    }


    [Fact]
    public void Despawn_All_Survives_A_Chain_Of_Relations()
    {
        using var world = new World();

        // Each Entity relates to the previous one, so despawning any of them reshapes several others.
        var chain = new Entity[16];
        chain[0] = world.Spawn();
        chain[1] = world.Spawn().Add(1, chain[0]);
        for (var i = 2; i < chain.Length; i++) chain[i] = world.Spawn().Add(i, chain[i - 1]).Add(i, chain[0]);

        world.All.Despawn();

        Assert.Equal(0, world.Count);
    }
}
