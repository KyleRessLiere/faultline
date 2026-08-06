using System;

namespace Faultline.Core
{
    /// <summary>
    /// One face of a Still Pond: what it gives, what it costs the run, whether it can be taken right
    /// now, and — when it cannot — why not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The invariant lives in the constructor.</b> MASTER_DESIGN §8.8 ends the Still Pond ruling
    /// with the sentence the node exists to hold: <em>never both full health and a free Rare</em>. A
    /// face that pairs <see cref="PondHealing.Full"/> with any <see cref="PondReward"/> cannot be
    /// constructed, so no later pond, generator or tuning pass can produce one by accident. Checking
    /// it at the point of use instead would have made the rule a habit rather than a law.
    /// </para>
    /// <para>
    /// <b>A refusal names its reason.</b> An unavailable face still has a name and printed terms —
    /// it is drawn, honestly, saying why it is not yet payable — but it carries no
    /// <see cref="Command"/>, so there is no way to send one. An unavailable face with no
    /// <see cref="Refusal"/> is rejected here for the same reason a silent no-op is a bug.
    /// </para>
    /// </remarks>
    public sealed record PondChoice
    {
        /// <summary>Builds one face of a pond, and holds §8.8's invariant while doing it.</summary>
        /// <param name="name">What the face is called on screen.</param>
        /// <param name="terms">What taking it does, printed before it is taken.</param>
        /// <param name="healing">How much health it gives back.</param>
        /// <param name="reward">What it hands out on top.</param>
        /// <param name="clearsBedraggled">Whether a duck that rests here loses the downed mark.</param>
        /// <param name="command">The command that takes it, or <c>null</c> when it cannot be taken.</param>
        /// <param name="refusal">Why it cannot be taken, or empty when it can.</param>
        /// <exception cref="ArgumentException">
        /// The face pairs full healing with a reward, or is unavailable without saying why, or is
        /// available with nothing to send.
        /// </exception>
        public PondChoice(
            string name,
            string terms,
            PondHealing healing,
            PondReward reward,
            bool clearsBedraggled,
            RunCommand? command,
            string refusal)
        {
            if (healing == PondHealing.Full && reward != PondReward.None)
            {
                throw new ArgumentException(
                    "'" + name + "' would pay full health and " + reward + " together. A Still Pond "
                    + "never pays both full health and a free Rare (MASTER_DESIGN §8.8).",
                    nameof(reward));
            }

            if (command is null && refusal.Length == 0)
            {
                throw new ArgumentException(
                    "'" + name + "' cannot be taken and does not say why. Every refusal names its "
                    + "reason.",
                    nameof(refusal));
            }

            if (command is not null && refusal.Length > 0)
            {
                throw new ArgumentException(
                    "'" + name + "' carries both a command and a refusal, so a player cannot tell "
                    + "whether it is on offer.",
                    nameof(refusal));
            }

            Name = name;
            Terms = terms;
            Healing = healing;
            Reward = reward;
            ClearsBedraggled = clearsBedraggled;
            Command = command;
            Refusal = refusal;
        }

        /// <summary>What the face is called on screen.</summary>
        public string Name { get; }

        /// <summary>What taking it does, in full, before it is taken.</summary>
        public string Terms { get; }

        /// <summary>How much health it gives back.</summary>
        public PondHealing Healing { get; }

        /// <summary>What it hands out on top of the healing.</summary>
        public PondReward Reward { get; }

        /// <summary>
        /// Whether a duck that takes this face loses the downed mark. §8.8's Deep Forge is the one
        /// face that does not: downed ducks return at a quarter and stay Bedraggled for boss round 1.
        /// </summary>
        public bool ClearsBedraggled { get; }

        /// <summary>The command that takes it, or <c>null</c> when it is not on offer.</summary>
        public RunCommand? Command { get; }

        /// <summary>Why it is not on offer, or empty when it is.</summary>
        public string Refusal { get; }

        /// <summary>True when a player may take this face right now.</summary>
        public bool Available => Command is not null;

        /// <inheritdoc/>
        public override string ToString() =>
            Name + " (" + Healing + ", " + Reward + (Available ? ")" : ", refused)");
    }
}
