using System;
using System.Collections.Generic;
using Faultline.Core;

namespace Faultline.Web.Shell.RunMap;

/// <summary>
/// The masked-pick ceremony in front of <see cref="VoteCommand"/>: A picks, B picks without seeing
/// it, then both are revealed and sent to Core in one command.
/// </summary>
/// <remarks>
/// <para>
/// <b>Blindness is this class's whole job.</b> <see cref="VoteCommand"/> deliberately carries both
/// picks and there is no half-voted state in Core for one to arrive into — "the moment the rules
/// hear about a vote is the moment it is already decided". So the sequencing, the masking and the
/// reveal are a property of the picking surface, and they live here, in one small object with a
/// stage and two strings.
/// </para>
/// <para>
/// <b>It decides nothing.</b> Which doors exist comes from <see cref="RunState.Doors"/>; whether
/// there is a vote at all comes from <see cref="RunState.Phase"/>; and who won a split is the
/// seeded coin inside <see cref="Campaign.ApplyRun"/>. This holds two picks and the order they were
/// made in.
/// </para>
/// <para>
/// <b>There is no way back.</b> <see cref="Close"/> is the only exit from
/// <see cref="VoteStage.Revealed"/> and it clears the picks; nothing re-opens a vote for a fork
/// already settled, because Core never returns to <see cref="RunPhase.AtVote"/> and a shell that
/// offered the button anyway would be offering a re-vote (MASTER_DESIGN §8.5).
/// </para>
/// </remarks>
public sealed class VoteFlow
{
    private readonly List<string> _doors = new();
    private string _a = string.Empty;
    private string _b = string.Empty;

    /// <summary>How far the ceremony has got.</summary>
    public VoteStage Stage { get; private set; } = VoteStage.Closed;

    /// <summary>The doors being picked between, in the order Core offered them.</summary>
    public IReadOnlyList<string> Doors => _doors;

    /// <summary>True while a pick is being taken and must not be shown to the other player.</summary>
    public bool Masked => Stage is VoteStage.PickingA or VoteStage.PickingB;

    /// <summary>Whose pick is being taken, or <c>null</c> when none is.</summary>
    public Team? Picker => Stage switch
    {
        VoteStage.PickingA => Team.PlayerA,
        VoteStage.PickingB => Team.PlayerB,
        _ => null,
    };

    /// <summary>
    /// Player A's pick — <c>null</c> until both picks are in. This is the mask: while B is choosing
    /// there is no property on this object that hands A's answer over.
    /// </summary>
    public string? PickA => Stage == VoteStage.Revealed ? _a : null;

    /// <summary>Player B's pick, or <c>null</c> until both picks are in.</summary>
    public string? PickB => Stage == VoteStage.Revealed ? _b : null;

    /// <summary>True once both picks are in and shown.</summary>
    public bool Revealed => Stage == VoteStage.Revealed;

    /// <summary>True when the revealed picks differ, so sending them will cost a coin.</summary>
    public bool IsSplit => Revealed && !string.Equals(_a, _b, StringComparison.Ordinal);

    /// <summary>
    /// Opens the ceremony on a fork's doors. Re-opening on the same doors leaves it alone, so a
    /// re-render mid-pick does not throw away a pick already taken.
    /// </summary>
    /// <param name="doors">The doors, from <see cref="RunState.Doors"/>.</param>
    public void Open(IReadOnlyList<string> doors)
    {
        if (doors is null)
        {
            throw new ArgumentNullException(nameof(doors));
        }

        if (Stage != VoteStage.Closed && Same(doors))
        {
            return;
        }

        _doors.Clear();
        _doors.AddRange(doors);
        _a = string.Empty;
        _b = string.Empty;
        Stage = VoteStage.PickingA;
    }

    /// <summary>Takes the current player's pick and hands the device on.</summary>
    /// <param name="nodeId">The door picked.</param>
    /// <exception cref="InvalidOperationException">Nobody is picking.</exception>
    public void Pick(string nodeId)
    {
        switch (Stage)
        {
            case VoteStage.PickingA:
                _a = nodeId ?? string.Empty;
                Stage = VoteStage.PickingB;
                return;

            case VoteStage.PickingB:
                _b = nodeId ?? string.Empty;
                Stage = VoteStage.Revealed;
                return;

            default:
                throw new InvalidOperationException(
                    "No pick is being taken. A vote is opened once per fork and never re-opened.");
        }
    }

    /// <summary>
    /// The command the revealed picks make. Sent to Core once, whereupon the fork is settled.
    /// </summary>
    /// <returns>The vote.</returns>
    /// <exception cref="InvalidOperationException">The picks are not both in yet.</exception>
    public VoteCommand Command() => Stage == VoteStage.Revealed
        ? new VoteCommand(_a, _b)
        : throw new InvalidOperationException("Both picks have to be in before a vote can be cast.");

    /// <summary>Shuts the ceremony down and forgets the picks. The only exit.</summary>
    public void Close()
    {
        _doors.Clear();
        _a = string.Empty;
        _b = string.Empty;
        Stage = VoteStage.Closed;
    }

    private bool Same(IReadOnlyList<string> doors)
    {
        if (doors.Count != _doors.Count)
        {
            return false;
        }

        for (int i = 0; i < doors.Count; i++)
        {
            if (!string.Equals(doors[i], _doors[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
