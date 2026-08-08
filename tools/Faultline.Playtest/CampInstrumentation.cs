using System.Globalization;
using System.Text;
using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// One camp <b>table</b>, and everything the card taken off it went on to do.
/// </summary>
/// <remarks>
/// <b>One row per table, never one per camp.</b> A camp deals two tables of two and takes a pick from
/// each (D-247), so collapsing them into a row would hide exactly the thing this stage exists to make
/// visible — what each player was offered and what each player took. The <c>owner</c> column is the
/// row's identity, not a decoration.
/// </remarks>
/// <param name="Camp">Which camp of the run this was, counting from 1.</param>
/// <param name="NodeId">The map node the camp followed.</param>
/// <param name="Lane">Safe or hungry, which is what priced the rarity roll.</param>
/// <param name="CardA">The first card on this player's table.</param>
/// <param name="CardB">The second card on this player's table.</param>
/// <param name="Taken">The card taken off it.</param>
/// <param name="Passed">The card left on it.</param>
/// <param name="Recipient">The duck it went on.</param>
/// <param name="Owner">The player whose table this row is.</param>
/// <param name="Bound">Which of §8.6's rows narrowed the pool while this table was dealt.</param>
public sealed record CampOfferRecord(
    int Camp,
    string NodeId,
    string Lane,
    string CardA,
    string CardB,
    string Taken,
    string Passed,
    string Recipient,
    string Owner,
    string Bound)
{
    /// <summary>How many times the card fired in play after it was taken.</summary>
    public int Triggers { get; set; }

    /// <summary>How many of those triggers crossed the flock boundary.</summary>
    public int CrossFlockTriggers { get; set; }

    /// <summary>
    /// How many times the card changed the CHOSEN ACTION — not merely its result.
    /// </summary>
    /// <remarks>
    /// The counterfactual: at every decision the owning player made, the policy is asked twice — once
    /// on the board as it stands, and once on the same board with this one card lifted off the
    /// duck's loadout and nothing else changed. When the two answers are different <em>commands</em>,
    /// the card changed the chosen action. When they are the same command, the card may still have
    /// changed what that action did, and that is what <see cref="Triggers"/> counts instead.
    /// </remarks>
    public int ChangedChosenAction { get; set; }

    /// <summary>Decisions the counterfactual was run against, so the count above has a denominator.</summary>
    public int DecisionsWatched { get; set; }

    /// <summary>Enemies downed by a command in which this card fired.</summary>
    public int ThreatsSolved { get; set; }

    /// <summary>Objective structures touched by a command in which this card fired.</summary>
    public int ObjectivesTouched { get; set; }

    /// <summary>The record as one CSV row.</summary>
    /// <returns>The row, without a trailing newline.</returns>
    public string Row() => string.Join(",", new[]
    {
        Number(Camp), Quote(NodeId), Quote(Lane),
        Quote(CardA), Quote(CardB), Quote(Taken), Quote(Passed),
        Quote(Recipient), Quote(Owner), Quote(Bound),
        Number(Triggers), Number(CrossFlockTriggers),
        Number(ChangedChosenAction), Number(DecisionsWatched),
        Number(ThreatsSolved), Number(ObjectivesTouched),
    });

    /// <summary>The CSV header this record's rows sit under.</summary>
    public static string Header =>
        "camp,node,lane,card_a,card_b,taken,passed,recipient,owner,bound,"
        + "triggers,cross_flock_triggers,changed_chosen_action,decisions_watched,"
        + "threats_solved,objectives_touched";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Quote(string? text) => "\"" + (text ?? string.Empty).Replace("\"", "\"\"") + "\"";
}

/// <summary>
/// Plays a run with a policy and writes down what each camp card did — the instrumentation
/// MASTER_DESIGN §8.6's design test asks for: <i>does a card change what the players ATTEMPT on the
/// next board?</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>This runner exists because <see cref="RunHarness"/> cannot pass a camp.</b> Its loop knows
/// <see cref="RunPhase.AtNode"/> and a fight and nothing else, so the first won fight parks the run
/// at <see cref="RunPhase.AtCamp"/> and the next iteration dereferences a null board. That is the
/// crash the harness notes have always attributed to <c>brawler</c>. The loop below answers camps
/// and votes as well, so a run can actually reach its capstone.
/// </para>
/// <para>
/// <b>The measure, and what it cannot see.</b> "Changed the chosen action" is a per-decision
/// counterfactual against one policy: same board, same policy, the card lifted off the loadout.
/// It cannot see (1) a decision a <em>human</em> would have made differently — it measures one
/// evaluator's preference ordering and nothing else; (2) a card that changed the chosen action by
/// changing the board several turns earlier, because both branches are scored against the board the
/// card already helped produce; (3) the difference between "made a different action correct" and
/// "made an action possible that was not on the legal list before" — removing Spotter removes the
/// shot entirely, and this counts that as a change; (4) anything at all after the card's own owner
/// stops having decisions, which is what a downed duck is.
/// </para>
/// </remarks>
public static class CampInstrumentation
{
    /// <summary>Safety stop, counted across the whole run.</summary>
    private const int MaxCommands = 200000;

    /// <summary>
    /// Plays one run and returns one record per camp.
    /// </summary>
    /// <param name="campaign">Campaign to play.</param>
    /// <param name="seed">Run seed.</param>
    /// <param name="policy">How the simulated players decide.</param>
    /// <param name="pick">
    /// Which card to take at each camp, by index. A run that takes card 0 everywhere and one that
    /// takes card 1 everywhere are the two halves of the acceptance: same seed, different picks.
    /// </param>
    /// <param name="stopAfterCamps">Stop once this many camps have been resolved; zero for the whole run.</param>
    /// <returns>The records, and why the run stopped.</returns>
    public static (IReadOnlyList<CampOfferRecord> Records, string Reason, RunState Final) Play(
        CampaignDefinition campaign, int seed, Policy policy, int pick, int stopAfterCamps = 0)
    {
        var run = Campaign.Start(campaign, seed).NewState;
        var rng = new DeterministicRng(seed);
        var records = new List<CampOfferRecord>();
        var live = new List<Live>();

        int commands = 0;
        string reason = "completed";

        while (run.Phase != RunPhase.Complete && commands < MaxCommands)
        {
            if (run.Fight is { Round: > RunHarness.StallRound })
            {
                reason = $"stalled: {run.Fight.Fight.Id} reached round {run.Fight.Round}";
                break;
            }

            RunCommand command;

            if (run.Phase == RunPhase.AtNode)
            {
                command = new EnterNodeCommand();
            }
            else if (run.Phase == RunPhase.AtCamp)
            {
                var table = Camp.Draw(run);

                if (table.Seats.Count == 0)
                {
                    command = new CampPickCommand(table, Team.PlayerA, CampPickCommand.NoPick);
                }
                else
                {
                    // A camp does not resolve until BOTH tables are spent (D-247), so the camp is
                    // taken here in one go rather than one command per loop iteration — and it writes
                    // ONE ROW PER TABLE. The two are never collapsed: which player was offered what
                    // is the whole question this instrument was built to answer.
                    int camp = run.CampsHeld + 1;

                    foreach (var seat in table.Seats)
                    {
                        int index = pick < seat.Offers.Count ? pick : seat.Offers.Count - 1;

                        var record = Record(run, table, seat, index, camp);
                        records.Add(record);
                        live.Add(new Live(record, seat.Offers[index]));

                        run = Campaign.ApplyRun(
                            run, new CampPickCommand(table, seat.Player, index)).NewState;
                        commands++;
                    }

                    if (stopAfterCamps > 0 && CampsIn(records) >= stopAfterCamps)
                    {
                        reason = "stopped after " + CampsIn(records) + " camps";
                        break;
                    }

                    continue;
                }
            }
            else if (run.Phase == RunPhase.AtVote)
            {
                // Self-agreeing, so no coin is flipped and the route is the same on both halves of
                // the acceptance (MASTER_DESIGN §8.5's harness contract).
                var door = run.Doors()[0];
                command = new VoteCommand(door, door);
            }
            else if (run.Phase == RunPhase.AtChoice)
            {
                // Whatever Core offers first. A campfire, an Offer and a Strait take three different
                // commands and this runner has no opinion about any of them — the decline-all stance
                // §8.5's harness contract asks for is a choice between the options Core lists, not a
                // command typed in here.
                var choices = Campaign.LegalRunCommands(run);
                if (choices.Count == 0)
                {
                    reason = "nothing legal at the choice on node " + run.NodeIndex;
                    break;
                }

                command = choices[0];
            }
            else if (run.Fight is null)
            {
                reason = "no board at phase " + run.Phase;
                break;
            }
            else
            {
                var enemy = Game.NextEnemyCommand(run.Fight);
                if (enemy is not null)
                {
                    command = new PlayCommand(enemy);
                }
                else
                {
                    var legal = Game.LegalCommands(run.Fight);
                    if (legal.Count == 0)
                    {
                        reason = "no legal command on node " + run.NodeIndex;
                        break;
                    }

                    var chosen = policy.Choose(run.Fight, legal, rng);
                    Counterfactual(run.Fight, legal, policy, chosen, live, run);
                    command = new PlayCommand(chosen);
                }
            }

            RunStepResult step;
            try
            {
                step = Campaign.ApplyRun(run, command);
            }
            catch (InvalidOperationException error)
            {
                reason = "refused: " + error.Message;
                break;
            }

            commands++;

            var board = step.FinalBoard ?? run.Fight;
            Absorb(step.FightEvents, board, live, run);

            run = step.NewState;
        }

        return (records, reason, run);
    }

    /// <summary>Writes the records as CSV.</summary>
    /// <param name="path">File to write.</param>
    /// <param name="rows">Rows to write, in order, each already labelled with its run.</param>
    public static void Write(string path, IReadOnlyList<string> rows)
    {
        var text = new StringBuilder();
        text.Append("run,").Append(CampOfferRecord.Header).Append('\n');
        foreach (string row in rows)
        {
            text.Append(row).Append('\n');
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, text.ToString());
    }

    /// <summary>How many camps a run of records covers — two tables share one camp number.</summary>
    private static int CampsIn(List<CampOfferRecord> records)
    {
        var seen = new HashSet<int>();
        foreach (var record in records)
        {
            seen.Add(record.Camp);
        }

        return seen.Count;
    }

    /// <summary>
    /// One row for one player's table. The cross-table rows travel on <see cref="CampTable.Bound"/>
    /// and the per-table rows on <see cref="CampSeat.Bound"/>; both go in the row's <c>bound</c>
    /// column, per-table first, because a reader of one row wants that table's reasons first.
    /// </summary>
    private static CampOfferRecord Record(
        RunState run, CampTable table, CampSeat seat, int pick, int camp)
    {
        var offers = seat.Offers;
        var taken = offers[pick];
        var passed = offers[pick == 0 ? offers.Count - 1 : 0];
        var duck = run.FindUnit(taken.Duck);

        var bound = new List<string>(seat.Bound);
        bound.AddRange(table.Bound);

        return new CampOfferRecord(
            camp,
            run.CurrentMapNode?.Id ?? run.NodeIndex.ToString(CultureInfo.InvariantCulture),
            (run.CurrentMapNode?.Lane ?? MapLane.Neutral).ToString(),
            offers[0].Name,
            offers.Count > 1 ? offers[1].Name : string.Empty,
            taken.Name,
            offers.Count > 1 ? passed.Name : string.Empty,
            duck is null ? "?" : Naming.Of(duck.Kind),
            seat.Player.ToString(),
            string.Join("; ", bound));
    }

    /// <summary>
    /// The counterfactual, run once per decision per live card: what would this policy have chosen
    /// on the same board with this card lifted off?
    /// </summary>
    private static void Counterfactual(
        GameState board,
        IReadOnlyList<Command> legal,
        Policy policy,
        Command chosen,
        List<Live> live,
        RunState run)
    {
        foreach (var card in live)
        {
            if (Fielded(run, board, card.Offer.Duck) is not { } unit)
            {
                continue;
            }

            // Every player decision, not only the card's own owner's. Five of the eight modifiers are
            // RELAY cards whose whole point is that they change what the OTHER flock attempts —
            // Crossing Shot changes the preview the initiating player reads, and gating this on the
            // Archer's own turns would have measured the one player the card is not addressed to.
            if (Actor(board, chosen) is not { } actor || !actor.Team.IsPlayer())
            {
                continue;
            }

            var without = board.WithUnit(unit with { Loadout = Without(unit.Loadout, card.Offer) });
            var legalWithout = Game.LegalCommands(without);
            if (legalWithout.Count == 0)
            {
                continue;
            }

            card.Record.DecisionsWatched++;

            // A fresh generator per branch, seeded the same: a policy that consults the dice must
            // consult the same dice on both sides or the difference is the dice and not the card.
            var otherwise = policy.Choose(without, legalWithout, new DeterministicRng(board.RngState));
            var same = policy.Choose(board, legal, new DeterministicRng(board.RngState));

            if (!Equals(otherwise, same))
            {
                card.Record.ChangedChosenAction++;
            }
        }
    }

    /// <summary>Counts what each live card's events did in one command's worth of play.</summary>
    private static void Absorb(
        IReadOnlyList<GameEvent> events, GameState? board, List<Live> live, RunState run)
    {
        if (events.Count == 0 || live.Count == 0)
        {
            return;
        }

        int downed = 0;
        int structures = 0;
        foreach (var e in events)
        {
            if (e is UnitDowned down && down.Team == Team.Enemy)
            {
                downed++;
            }

            if (e is StructureDamaged or StructureDestroyed or StructureAttacked)
            {
                structures++;
            }
        }

        foreach (var card in live)
        {
            var unit = board is null ? null : Fielded(run, board, card.Offer.Duck);
            int fired = 0;
            int crossed = 0;

            foreach (var e in events)
            {
                if (!Fires(card.Offer, e, unit))
                {
                    continue;
                }

                fired++;
                if (CrossesFlocks(card.Offer, e))
                {
                    crossed++;
                }
            }

            if (fired == 0)
            {
                continue;
            }

            card.Record.Triggers += fired;
            card.Record.CrossFlockTriggers += crossed;
            card.Record.ThreatsSolved += downed;
            card.Record.ObjectivesTouched += structures;
        }
    }

    /// <summary>
    /// Whether one event is this card firing. Named per card rather than inferred, because "the card
    /// did something" is exactly the claim the instrumentation is making and a heuristic would make
    /// it quietly.
    /// </summary>
    private static bool Fires(CampOffer offer, GameEvent e, Unit? unit)
    {
        if (offer.Category != OfferCategory.Technique || unit is null)
        {
            return false;
        }

        return offer.AsTechnique switch
        {
            TechniqueModifier.RattlingImpact => e is Rattled rattled && rattled.ByUnitId == unit.Id,
            TechniqueModifier.HandOff => e is HandOffGranted granted && granted.ByUnitId == unit.Id,
            TechniqueModifier.CrossingShot => e is CrossingShotFired shot && shot.ArcherId == unit.Id,
            TechniqueModifier.ShelterStep => e is StepBanked banked && banked.ByGuard == unit.Id,

            // The three that change an action rather than adding an event are counted off the shape
            // of what happened: a Vanguard who moved without spending a point followed in, a
            // Wardbearer whose Spear pushed spent Force, and a drag that stopped short was Short Line.
            TechniqueModifier.FollowIn =>
                e is UnitMoved moved && moved.UnitId == unit.Id && moved.Cost == 0 && moved.Path.Count == 1,
            TechniqueModifier.StoredForce =>
                e is UnitPushed spear && spear.By == unit.Id && spear.Kind == DisplacementKind.Push,
            TechniqueModifier.ShortLine =>
                e is UnitPushed drag && drag.By == unit.Id && drag.Kind == DisplacementKind.Pull,

            // Spotter adds no event at all: it opens a shot that was illegal. A shot from inside her
            // own minimum range is the only visible trace it leaves.
            TechniqueModifier.Spotter =>
                e is UnitAttacked shotIn
                && shotIn.AttackerId == unit.Id
                && shotIn.From.DistanceTo(shotIn.To) < unit.Template.MinRange,

            _ => false,
        };
    }

    /// <summary>Whether this firing crossed the flock boundary — the RELAY question §8.6 is about.</summary>
    private static bool CrossesFlocks(CampOffer offer, GameEvent e) =>
        offer.Category == OfferCategory.Technique
        && offer.AsTechnique switch
        {
            TechniqueModifier.RattlingImpact => e is Rattled,
            TechniqueModifier.HandOff => e is HandOffGranted,
            TechniqueModifier.CrossingShot => e is CrossingShotFired,
            TechniqueModifier.ShelterStep => e is StepBanked,
            TechniqueModifier.Spotter => e is UnitAttacked,
            _ => false,
        };

    private static Unit? Fielded(RunState run, GameState board, RunUnitId duck)
    {
        foreach (var binding in run.Bindings)
        {
            if (binding.RunUnitId.Equals(duck))
            {
                return board.FindUnit(binding.UnitId);
            }
        }

        return null;
    }

    private static Unit? Actor(GameState board, Command command) =>
        board.FindUnit(RunLog.ActorOf(command));

    private static DuckLoadout Without(DuckLoadout loadout, CampOffer offer)
    {
        if (offer.Category != OfferCategory.Technique)
        {
            return loadout;
        }

        var kept = new List<TechniqueModifier>();
        foreach (var technique in loadout.Techniques)
        {
            if (technique != offer.AsTechnique)
            {
                kept.Add(technique);
            }
        }

        return loadout with { Techniques = kept };
    }

    private sealed class Live
    {
        public Live(CampOfferRecord record, CampOffer offer)
        {
            Record = record;
            Offer = offer;
        }

        public CampOfferRecord Record { get; }

        public CampOffer Offer { get; }
    }
}
