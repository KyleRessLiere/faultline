namespace Faultline.Core
{
    /// <summary>
    /// One pick taken at the camp the run is standing at, and not yet applied.
    /// </summary>
    /// <remarks>
    /// <b>Recorded, not applied — this is what makes "both tables spent" construction.</b> The cards
    /// land on ducks in exactly one place, the camp's exit, and the exit is reached only when no seat
    /// has a legal pick left. So there is no state in which one player's card has been given and the
    /// camp has advanced (D-251).
    /// <para>
    /// Deferring is also forced by the derived table: <see cref="CampCatalogue.EligibleFor"/> reads a
    /// duck's loadout, so applying player A's card as it arrived would change what player B is
    /// redealt, and B's own recorded table would be refused as one the seed never dealt.
    /// </para>
    /// </remarks>
    /// <param name="Player">Whose table the pick came off.</param>
    /// <param name="Index">Index into that player's <see cref="CampSeat.Offers"/>.</param>
    public readonly record struct CampPick(Team Player, int Index);
}
