using System.Collections.Generic;

namespace Spellwright.Encounter
{
    /// <summary>
    /// NPC expression states for portrait reactions.
    /// </summary>
    public enum NPCExpression
    {
        Neutral,
        Amused,
        Impressed,
        Angry,
        Defeated,
        Victorious
    }

    /// <summary>
    /// Static ASCII art portraits keyed by NPC display name and expression.
    /// Each portrait is ~8 lines tall using Unicode box-drawing characters.
    /// Regular NPCs use light borders (┌─┐), boss uses double borders (╔═╗).
    /// </summary>
    public static class NPCPortraitData
    {
        // ── Portraits keyed by NPC display name ─────────────────

        private static readonly Dictionary<string, Dictionary<NPCExpression, string>> Portraits = new()
        {
            // ── Riddlemaster: Robed mystic with rune-adorned hood and magic staff ──
            ["Riddlemaster"] = new Dictionary<NPCExpression, string>
            {
                [NPCExpression.Neutral] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u25c7\u2580\u25c7\\  \u2502",
                    "  \u2502\u2502 \u25cb  \u25cb \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u2500\u2500\u2500 \u2502\u2502",
                    "  \u2502 \\\u2593\u2593\u2593/ \u2502",
                    "  \u2502  \u256c\u2551\u256c  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Amused] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u25c7\u2580\u25c7\\  \u2502",
                    "  \u2502\u2502 \u02d8  \u02d8 \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u256d\u2500\u256e \u2502\u2502",
                    "  \u2502 \\\u2593\u2593\u2593/ \u2502",
                    "  \u2502  \u256c\u2551\u256c  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Impressed] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u25c7\u2580\u25c7\\  \u2502",
                    "  \u2502\u2502 \u25ce  \u25ce \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u25cb\u25cb\u25cb \u2502\u2502",
                    "  \u2502 \\\u2593\u2593\u2593/ \u2502",
                    "  \u2502  \u256c\u2551\u256c  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Angry] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u25c7\u2580\u25c7\\  \u2502",
                    "  \u2502\u2502 \u00d7  \u00d7 \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u256d\u2501\u256e \u2502\u2502",
                    "  \u2502 \\\u2593\u2593\u2593/ \u2502",
                    "  \u2502  \u256c\u2551\u256c  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Defeated] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u25c7\u2580\u25c7\\  \u2502",
                    "  \u2502\u2502 \u2013  \u2013 \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u256e\u2500\u256d \u2502\u2502",
                    "  \u2502 \\\u2593\u2593\u2593/ \u2502",
                    "  \u2502  \u256c\u2551\u256c  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Victorious] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u25c7\u2580\u25c7\\  \u2502",
                    "  \u2502\u2502 \u0302  \u0302 \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u2580\u2580\u2580 \u2502\u2502",
                    "  \u2502 \\\u2593\u2593\u2593/ \u2502",
                    "  \u2502  \u256c\u2551\u256c  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518")
            },

            // ── Trickster Merchant: Cunning trader with top hat and diamond-studded boots ──
            ["Trickster Merchant"] = new Dictionary<NPCExpression, string>
            {
                [NPCExpression.Neutral] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 \u250c\u2580\u2580\u2580\u2580\u2510 \u2502",
                    "  \u2502 (\u25c9 _ \u25c9) \u2502",
                    "  \u2502  <  >  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502 \u25c6\u2584\u2584\u2584\u2584\u25c6 \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Amused] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 \u250c\u2580\u2580\u2580\u2580\u2510 \u2502",
                    "  \u2502 (\u02d8 v \u02d8) \u2502",
                    "  \u2502  <$$>  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502 \u25c6\u2584\u2584\u2584\u2584\u25c6 \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Impressed] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 \u250c\u2580\u2580\u2580\u2580\u2510 \u2502",
                    "  \u2502 (\u25ce o \u25ce) \u2502",
                    "  \u2502  <  >  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502 \u25c6\u2584\u2584\u2584\u2584\u25c6 \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Angry] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 \u250c\u2580\u2580\u2580\u2580\u2510 \u2502",
                    "  \u2502 (\u00d7 _ \u00d7) \u2502",
                    "  \u2502  <!!\u00a1>  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502 \u25c6\u2584\u2584\u2584\u2584\u25c6 \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Defeated] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 \u250c\u2580\u2580\u2580\u2580\u2510 \u2502",
                    "  \u2502 (T _ T) \u2502",
                    "  \u2502  <  >  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502 \u25c6\u2584\u2584\u2584\u2584\u25c6 \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Victorious] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 \u250c\u2580\u2580\u2580\u2580\u2510 \u2502",
                    "  \u2502 (\u0302 w \u0302) \u2502",
                    "  \u2502  <$$>  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502 \u25c6\u2584\u2584\u2584\u2584\u25c6 \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518")
            },

            // ── Silent Librarian: Austere scholar with book-hat and scroll-bracket body ──
            ["Silent Librarian"] = new Dictionary<NPCExpression, string>
            {
                [NPCExpression.Neutral] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 {\u25a1  \u25a1} \u2502",
                    "  \u2502 {  ..  } \u2502",
                    "  \u2502 { \u2500\u2500 } \u2502",
                    "  \u2502 {\u2584\u2584\u2584\u2584} \u2502",
                    "  \u2502  \u2558\u2551\u2551\u255b  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Amused] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 {\u25a1  \u25a1} \u2502",
                    "  \u2502 {  ..  } \u2502",
                    "  \u2502 { \u256d\u256e } \u2502",
                    "  \u2502 {\u2584\u2584\u2584\u2584} \u2502",
                    "  \u2502  \u2558\u2551\u2551\u255b  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Impressed] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 {\u25ce  \u25ce} \u2502",
                    "  \u2502 {  ..  } \u2502",
                    "  \u2502 { \u25cb\u25cb } \u2502",
                    "  \u2502 {\u2584\u2584\u2584\u2584} \u2502",
                    "  \u2502  \u2558\u2551\u2551\u255b  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Angry] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 {\u25a0  \u25a0} \u2502",
                    "  \u2502 {  ..  } \u2502",
                    "  \u2502 { \u2501\u2501 } \u2502",
                    "  \u2502 {\u2584\u2584\u2584\u2584} \u2502",
                    "  \u2502  \u2558\u2551\u2551\u255b  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Defeated] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 {\u2013  \u2013} \u2502",
                    "  \u2502 {  ..  } \u2502",
                    "  \u2502 { \u256e\u256d } \u2502",
                    "  \u2502 {\u2584\u2584\u2584\u2584} \u2502",
                    "  \u2502  \u2558\u2551\u2551\u255b  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Victorious] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 {\u0302  \u0302} \u2502",
                    "  \u2502 {  ..  } \u2502",
                    "  \u2502 { \u2580\u2580 } \u2502",
                    "  \u2502 {\u2584\u2584\u2584\u2584} \u2502",
                    "  \u2502  \u2558\u2551\u2551\u255b  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518")
            },

            // ── The Whisperer (Boss): Eldritch shadow with smoke hood and tendrils ──
            ["The Whisperer"] = new Dictionary<NPCExpression, string>
            {
                [NPCExpression.Neutral] = string.Join("\n",
                    "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                    "  \u2551 /\u2591\u2593\u2591\\ \u2551",
                    "  \u2551\u2502 \u2620  \u2620 \u2502\u2551",
                    "  \u2551\u2502  \u25c6  \u2502\u2551",
                    "  \u2551\u2502 \u2261\u2261\u2261 \u2502\u2551",
                    "  \u2551 \\\u2591\u2593\u2591/ \u2551",
                    "  \u2551  \u2248\u2503\u2503\u2248  \u2551",
                    "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
                [NPCExpression.Amused] = string.Join("\n",
                    "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                    "  \u2551 /\u2591\u2593\u2591\\ \u2551",
                    "  \u2551\u2502 \u02d8  \u02d8 \u2502\u2551",
                    "  \u2551\u2502  \u25c6  \u2502\u2551",
                    "  \u2551\u2502 \u256d\u2501\u256e \u2502\u2551",
                    "  \u2551 \\\u2591\u2593\u2591/ \u2551",
                    "  \u2551  \u2248\u2503\u2503\u2248  \u2551",
                    "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
                [NPCExpression.Impressed] = string.Join("\n",
                    "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                    "  \u2551 /\u2591\u2593\u2591\\ \u2551",
                    "  \u2551\u2502 \u25c9  \u25c9 \u2502\u2551",
                    "  \u2551\u2502  \u25c6  \u2502\u2551",
                    "  \u2551\u2502 \u25cb\u25cb\u25cb \u2502\u2551",
                    "  \u2551 \\\u2591\u2593\u2591/ \u2551",
                    "  \u2551  \u2248\u2503\u2503\u2248  \u2551",
                    "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
                [NPCExpression.Angry] = string.Join("\n",
                    "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                    "  \u2551 /\u2591\u2593\u2591\\ \u2551",
                    "  \u2551\u2502 \u2620  \u2620 \u2502\u2551",
                    "  \u2551\u2502  \u25c6  \u2502\u2551",
                    "  \u2551\u2502 \u2501\u2501\u2501 \u2502\u2551",
                    "  \u2551 \\\u2591\u2593\u2591/ \u2551",
                    "  \u2551  \u2248\u2503\u2503\u2248  \u2551",
                    "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
                [NPCExpression.Defeated] = string.Join("\n",
                    "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                    "  \u2551 /\u2591\u2593\u2591\\ \u2551",
                    "  \u2551\u2502 x  x \u2502\u2551",
                    "  \u2551\u2502  \u25c6  \u2502\u2551",
                    "  \u2551\u2502 \u256e\u2500\u256d \u2502\u2551",
                    "  \u2551 \\\u2591\u2593\u2591/ \u2551",
                    "  \u2551  \u2248\u2503\u2503\u2248  \u2551",
                    "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
                [NPCExpression.Victorious] = string.Join("\n",
                    "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                    "  \u2551 /\u2591\u2593\u2591\\ \u2551",
                    "  \u2551\u2502 \u0302  \u0302 \u2502\u2551",
                    "  \u2551\u2502  \u25c6  \u2502\u2551",
                    "  \u2551\u2502 \u2580\u2580\u2580 \u2502\u2551",
                    "  \u2551 \\\u2591\u2593\u2591/ \u2551",
                    "  \u2551  \u2248\u2503\u2503\u2248  \u2551",
                    "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d")
            }
        };

        /// <summary>
        /// Fallback portrait used when an NPC ID is not found.
        /// </summary>
        private static readonly string FallbackPortrait = string.Join("\n",
            "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
            "  \u2502  ????  \u2502",
            "  \u2502  (. .)  \u2502",
            "  \u2502   __   \u2502",
            "  \u2502  /||\\ \u2502",
            "  \u2502 / || \\ \u2502",
            "  \u2502  \u2584\u2584\u2584\u2584  \u2502",
            "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518");

        /// <summary>
        /// Gets the ASCII art portrait for a given NPC identity and expression.
        /// NPC identity should match the NPC's display name.
        /// </summary>
        public static string GetPortrait(string npcId, NPCExpression expression)
        {
            if (npcId != null &&
                Portraits.TryGetValue(npcId, out var expressions) &&
                expressions.TryGetValue(expression, out var art))
                return art;

            // Try neutral expression as fallback for unknown expression
            if (npcId != null &&
                Portraits.TryGetValue(npcId, out var exprs) &&
                exprs.TryGetValue(NPCExpression.Neutral, out var neutralArt))
                return neutralArt;

            return FallbackPortrait;
        }
    }
}
