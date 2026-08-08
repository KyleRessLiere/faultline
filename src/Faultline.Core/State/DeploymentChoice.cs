namespace Faultline.Core
{
    /// <summary>
    /// A player's blind answer to step 1 of the deployment draft: place first, or place second.
    /// </summary>
    /// <remarks>
    /// MASTER_DESIGN §3 (locked y). The answer is a <b>preference</b>, not a claim — two players may
    /// want the same thing, which is exactly when the seeded coin fires. The question is asked blind
    /// on purpose: every other shared decision in this game is blind, and an open question invites
    /// the more experienced player to answer for both.
    /// </remarks>
    public enum DeploymentChoice
    {
        /// <summary>Place first — reveal your setup, and activate first in return.</summary>
        PlaceFirst = 0,

        /// <summary>Place second — see their setup before committing, and activate second.</summary>
        PlaceSecond = 1,
    }
}
