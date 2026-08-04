using System;
using Faultline.Core;

namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// The picking ceremony in front of <see cref="CampPickCommand"/>: both players' tables are on
/// screen at once, each player picks and confirms their own, and the two confirmed picks go to Core
/// as one command.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not the vote's ceremony, and deliberately not.</b> A vote is one decision two people make
/// about the same thing, so it is masked and sequenced. A camp is two decisions about two separate
/// tables drawn from two separate squads (MASTER_DESIGN §8.5, simultaneous and independent) — there
/// is nothing for A's pick to spoil about B's, so both are shown, either may go first, and each
/// player confirms their own.
/// </para>
/// <para>
/// <b>There is no skip.</b> A player with cards on the table has to take one — camps are the reward
/// and declining a reward is not a decision worth a button (§8.5). The one index that is not a card
/// is <see cref="CampPickCommand.NoPick"/>, which belongs to a player who was dealt nothing at all,
/// and this flow confirms such a player automatically because there is nothing to ask them.
/// </para>
/// <para>
/// <b>It decides nothing.</b> What is on the table is <see cref="Camp.Draw"/>'s answer, whether the
/// picks are legal is <see cref="Camp.Resolve"/>'s, and where the run goes afterwards is
/// <see cref="Campaign.Advance"/>'s. This holds two integers and two booleans.
/// </para>
/// </remarks>
public sealed class CampFlow
{
    private int _countA;
    private int _countB;
    private int _pickA = CampPickCommand.NoPick;
    private int _pickB = CampPickCommand.NoPick;

    /// <summary>True while a camp is on screen.</summary>
    public bool Open { get; private set; }

    /// <summary>Whether Player A has locked their pick in.</summary>
    public bool ConfirmedA { get; private set; }

    /// <summary>Whether Player B has locked their pick in.</summary>
    public bool ConfirmedB { get; private set; }

    /// <summary>Player A's highlighted card, or <c>null</c> while they have picked none.</summary>
    public int? SelectedA => _pickA >= 0 ? _pickA : (int?)null;

    /// <summary>Player B's highlighted card, or <c>null</c> while they have picked none.</summary>
    public int? SelectedB => _pickB >= 0 ? _pickB : (int?)null;

    /// <summary>True once both players have confirmed, so the command can be sent.</summary>
    public bool Ready => Open && ConfirmedA && ConfirmedB;

    /// <summary>Whether that player has confirmed.</summary>
    /// <param name="player">Which player.</param>
    /// <returns>Whether their pick is locked in.</returns>
    public bool Confirmed(Team player) => player == Team.PlayerA ? ConfirmedA : ConfirmedB;

    /// <summary>That player's highlighted card, or <c>null</c>.</summary>
    /// <param name="player">Which player.</param>
    /// <returns>The index into their draw.</returns>
    public int? Selected(Team player) => player == Team.PlayerA ? SelectedA : SelectedB;

    /// <summary>How many cards that player was dealt.</summary>
    /// <param name="player">Which player.</param>
    /// <returns>The count.</returns>
    public int Count(Team player) => player == Team.PlayerA ? _countA : _countB;

    /// <summary>
    /// Opens the ceremony on a table. Re-opening on the same table leaves it alone, so a re-render
    /// between the two picks does not throw away one already taken.
    /// </summary>
    /// <param name="table">The table, from <see cref="Camp.Draw"/>.</param>
    /// <exception cref="ArgumentNullException">There is no table.</exception>
    public void Begin(CampTable table)
    {
        if (table is null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        if (Open && _countA == table.OffersA.Count && _countB == table.OffersB.Count)
        {
            return;
        }

        _countA = table.OffersA.Count;
        _countB = table.OffersB.Count;
        _pickA = CampPickCommand.NoPick;
        _pickB = CampPickCommand.NoPick;

        // A player dealt nothing has nothing to be asked. Their index is NoPick — the absence of a
        // table, never a decline — and waiting for them to press a button would deadlock the camp.
        ConfirmedA = _countA == 0;
        ConfirmedB = _countB == 0;
        Open = true;
    }

    /// <summary>
    /// Highlights one of that player's cards. An index that is not a card on their own table is
    /// refused, and so is any pick after they have confirmed.
    /// </summary>
    /// <param name="player">Which player is picking.</param>
    /// <param name="index">Index into their draw.</param>
    /// <returns>Whether the pick was taken.</returns>
    public bool Select(Team player, int index)
    {
        if (!Open || Confirmed(player) || index < 0 || index >= Count(player))
        {
            return false;
        }

        if (player == Team.PlayerA)
        {
            _pickA = index;
        }
        else if (player == Team.PlayerB)
        {
            _pickB = index;
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Locks that player's pick in. Refused while they have picked nothing — there is no skip, so a
    /// player with cards on the table cannot confirm their way past them.
    /// </summary>
    /// <param name="player">Which player.</param>
    /// <returns>Whether the confirmation was taken.</returns>
    public bool Confirm(Team player)
    {
        if (!Open || Confirmed(player) || Selected(player) is null)
        {
            return false;
        }

        if (player == Team.PlayerA)
        {
            ConfirmedA = true;
        }
        else if (player == Team.PlayerB)
        {
            ConfirmedB = true;
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>Unlocks a confirmed pick, so a player can change their mind before the other lands.</summary>
    /// <param name="player">Which player.</param>
    /// <returns>Whether anything was unlocked.</returns>
    /// <remarks>
    /// Legal only while there is still a card to go back to: a player who was dealt nothing was
    /// confirmed by <see cref="Begin"/> rather than by pressing anything, and has nothing to reopen.
    /// </remarks>
    public bool Reopen(Team player)
    {
        if (!Open || !Confirmed(player) || Count(player) == 0)
        {
            return false;
        }

        if (player == Team.PlayerA)
        {
            ConfirmedA = false;
        }
        else if (player == Team.PlayerB)
        {
            ConfirmedB = false;
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>Player A's pick as the command carries it.</summary>
    public int PickA => _pickA;

    /// <summary>Player B's pick as the command carries it.</summary>
    public int PickB => _pickB;

    /// <summary>Shuts the ceremony down and forgets the picks.</summary>
    public void Close()
    {
        Open = false;
        ConfirmedA = false;
        ConfirmedB = false;
        _countA = 0;
        _countB = 0;
        _pickA = CampPickCommand.NoPick;
        _pickB = CampPickCommand.NoPick;
    }
}
