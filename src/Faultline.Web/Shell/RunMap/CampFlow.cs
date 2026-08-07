using System;
using Faultline.Core;

namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// The picking ceremony in front of <see cref="CampPickCommand"/>: two cards on one table, one of
/// them highlighted, and one confirmation that sends it.
/// </summary>
/// <remarks>
/// <para>
/// <b>One table, one pick.</b> The camp used to deal each player their own pair and ask both — the
/// shape D-127 argued for. MASTER_DESIGN §8.6's director rows are written about a single table
/// spanning the squad ("different classes, preferably different players") and count which player's
/// ducks the last two picks went to, so the table became one and the ceremony with it (D-154). Which
/// player's duck a card belongs to is printed on the card, and it is the interesting part: taking
/// the Archer's card is choosing not to take the Vanguard's.
/// </para>
/// <para>
/// <b>There is no skip.</b> A flock with cards on the table has to take one — camps are the reward
/// and declining a reward is not a decision worth a button (§8.5). The one index that is not a card
/// is <see cref="CampPickCommand.NoPick"/>, which belongs to a camp that dealt nothing at all, and
/// this flow confirms that case automatically because there is nothing to ask.
/// </para>
/// <para>
/// <b>It decides nothing.</b> What is on the table is <see cref="Camp.Draw"/>'s answer, whether the
/// pick is legal is <see cref="Camp.Resolve"/>'s, and where the run goes afterwards is
/// <see cref="Campaign.Advance"/>'s. This holds one integer and two booleans.
/// </para>
/// </remarks>
public sealed class CampFlow
{
    private int _count;
    private int _pick = CampPickCommand.NoPick;

    /// <summary>True while a camp is on screen.</summary>
    public bool Open { get; private set; }

    /// <summary>Whether the pick is locked in.</summary>
    public bool Confirmed { get; private set; }

    /// <summary>The highlighted card, or <c>null</c> while none is picked.</summary>
    public int? Selected => _pick >= 0 ? _pick : (int?)null;

    /// <summary>True once the pick is confirmed, so the command can be sent.</summary>
    public bool Ready => Open && Confirmed;

    /// <summary>How many cards are on the table.</summary>
    public int Count => _count;

    /// <summary>The pick as the command carries it.</summary>
    public int Pick => _pick;

    /// <summary>
    /// Opens the ceremony on a table. Re-opening on the same table leaves it alone, so a re-render
    /// between the pick and the confirmation does not throw away the pick.
    /// </summary>
    /// <param name="table">The table, from <see cref="Camp.Draw"/>.</param>
    /// <exception cref="ArgumentNullException">There is no table.</exception>
    public void Begin(CampTable table)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        Begin(table.Offers.Count);
    }

    /// <summary>
    /// Opens the ceremony on a table of <paramref name="offers"/> cards, whatever dealt them.
    /// </summary>
    /// <remarks>
    /// The ceremony was always generic — pick one of N, confirm, optionally change your mind — and
    /// only <see cref="Begin(CampTable)"/> knew what a camp was. The gilt destination's legendary
    /// pair is the same ceremony over a different table, so it takes this rather than growing a
    /// second copy with its own off-by-one bugs (D-222).
    /// </remarks>
    /// <param name="offers">How many cards are on the table; zero is a table that dealt nothing.</param>
    /// <exception cref="ArgumentOutOfRangeException">A negative number of cards.</exception>
    public void Begin(int offers)
    {
        if (offers < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offers), "A table cannot deal fewer than no cards.");
        }

        if (Open && _count == offers)
        {
            return;
        }

        _count = offers;
        _pick = CampPickCommand.NoPick;

        // A camp that dealt nothing has nothing to ask about. Its index is NoPick — the absence of a
        // table, never a decline — and waiting for a button would deadlock the camp.
        Confirmed = _count == 0;
        Open = true;
    }

    /// <summary>
    /// Highlights one of the cards. An index that is not on the table is refused, and so is any pick
    /// after the confirmation.
    /// </summary>
    /// <param name="index">Index into <see cref="CampTable.Offers"/>.</param>
    /// <returns>Whether the pick was taken.</returns>
    public bool Select(int index)
    {
        if (!Open || Confirmed || index < 0 || index >= _count)
        {
            return false;
        }

        _pick = index;
        return true;
    }

    /// <summary>
    /// Locks the pick in. Refused while nothing is picked — there is no skip, so a flock with cards
    /// on the table cannot confirm its way past them.
    /// </summary>
    /// <returns>Whether the confirmation was taken.</returns>
    public bool Confirm()
    {
        if (!Open || Confirmed || Selected is null)
        {
            return false;
        }

        Confirmed = true;
        return true;
    }

    /// <summary>Unlocks a confirmed pick, so the flock can change its mind before the command goes.</summary>
    /// <returns>Whether anything was unlocked.</returns>
    /// <remarks>
    /// Legal only while there is a card to go back to: a camp that dealt nothing was confirmed by
    /// <see cref="Begin"/> rather than by anybody pressing anything, and has nothing to reopen.
    /// </remarks>
    public bool Reopen()
    {
        if (!Open || !Confirmed || _count == 0)
        {
            return false;
        }

        Confirmed = false;
        return true;
    }

    /// <summary>Shuts the ceremony down and forgets the pick.</summary>
    public void Close()
    {
        Open = false;
        Confirmed = false;
        _count = 0;
        _pick = CampPickCommand.NoPick;
    }
}
