using System.Collections.Generic;
using Spellwright.Data;

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
    /// Static ASCII art portraits for each NPC archetype and expression.
    /// Each portrait is ~8 lines tall, ~14 chars wide using Unicode box-drawing.
    /// </summary>
    public static class NPCPortraitData
    {
        private static readonly Dictionary<NPCArchetype, Dictionary<NPCExpression, string>> Portraits = new()
        {
            [NPCArchetype.Riddlemaster] = new Dictionary<NPCExpression, string>
            {
                [NPCExpression.Neutral] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u2580\u2580\u2580\\ \u2502",
                    "  \u2502\u2502 \u25cb  \u25cb \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u2500\u2500\u2500 \u2502\u2502",
                    "  \u2502 \\\u2584\u2584\u2584/ \u2502",
                    "  \u2502  \u2551\u2551   \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Amused] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u2580\u2580\u2580\\ \u2502",
                    "  \u2502\u2502 \u02d8  \u02d8 \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u256d\u2500\u256e \u2502\u2502",
                    "  \u2502 \\\u2584\u2584\u2584/ \u2502",
                    "  \u2502  \u2551\u2551   \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Impressed] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u2580\u2580\u2580\\ \u2502",
                    "  \u2502\u2502 \u25ce  \u25ce \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u25cb\u25cb\u25cb \u2502\u2502",
                    "  \u2502 \\\u2584\u2584\u2584/ \u2502",
                    "  \u2502  \u2551\u2551   \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Angry] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u2580\u2580\u2580\\ \u2502",
                    "  \u2502\u2502 \u00d7  \u00d7 \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u256d\u2501\u256e \u2502\u2502",
                    "  \u2502 \\\u2584\u2584\u2584/ \u2502",
                    "  \u2502  \u2551\u2551   \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Defeated] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u2580\u2580\u2580\\ \u2502",
                    "  \u2502\u2502 \u2013  \u2013 \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u256e\u2500\u256d \u2502\u2502",
                    "  \u2502 \\\u2584\u2584\u2584/ \u2502",
                    "  \u2502  \u2551\u2551   \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Victorious] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 /\u2580\u2580\u2580\\ \u2502",
                    "  \u2502\u2502 \u0302  \u0302 \u2502\u2502",
                    "  \u2502\u2502  \u25b8  \u2502\u2502",
                    "  \u2502\u2502 \u2580\u2580\u2580 \u2502\u2502",
                    "  \u2502 \\\u2584\u2584\u2584/ \u2502",
                    "  \u2502  \u2551\u2551   \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518")
            },

            [NPCArchetype.TricksterMerchant] = new Dictionary<NPCExpression, string>
            {
                [NPCExpression.Neutral] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502  \u2580\u2580\u2580\u2580  \u2502",
                    "  \u2502 (\u25c9 _ \u25c9) \u2502",
                    "  \u2502  <  >  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502  \u2584\u2584\u2584\u2584  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Amused] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502  \u2580\u2580\u2580\u2580  \u2502",
                    "  \u2502 (\u02d8 v \u02d8) \u2502",
                    "  \u2502  <$$>  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502  \u2584\u2584\u2584\u2584  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Impressed] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502  \u2580\u2580\u2580\u2580  \u2502",
                    "  \u2502 (\u25ce o \u25ce) \u2502",
                    "  \u2502  <  >  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502  \u2584\u2584\u2584\u2584  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Angry] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502  \u2580\u2580\u2580\u2580  \u2502",
                    "  \u2502 (\u00d7 _ \u00d7) \u2502",
                    "  \u2502  <!!\u00a1>  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502  \u2584\u2584\u2584\u2584  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Defeated] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502  \u2580\u2580\u2580\u2580  \u2502",
                    "  \u2502 (T _ T) \u2502",
                    "  \u2502  <  >  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502  \u2584\u2584\u2584\u2584  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Victorious] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502  \u2580\u2580\u2580\u2580  \u2502",
                    "  \u2502 (\u0302 w \u0302) \u2502",
                    "  \u2502  <$$>  \u2502",
                    "  \u2502  /||\\ \u2502",
                    "  \u2502 / || \\ \u2502",
                    "  \u2502  \u2584\u2584\u2584\u2584  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518")
            },

            [NPCArchetype.SilentLibrarian] = new Dictionary<NPCExpression, string>
            {
                [NPCExpression.Neutral] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 |\u25a1  \u25a1| \u2502",
                    "  \u2502 |  ..  | \u2502",
                    "  \u2502 | \u2500\u2500 | \u2502",
                    "  \u2502 |\u2584\u2584\u2584\u2584| \u2502",
                    "  \u2502  \u2502\u2551\u2551\u2502  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Amused] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 |\u25a1  \u25a1| \u2502",
                    "  \u2502 |  ..  | \u2502",
                    "  \u2502 | \u256d\u256e | \u2502",
                    "  \u2502 |\u2584\u2584\u2584\u2584| \u2502",
                    "  \u2502  \u2502\u2551\u2551\u2502  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Impressed] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 |\u25ce  \u25ce| \u2502",
                    "  \u2502 |  ..  | \u2502",
                    "  \u2502 | \u25cb\u25cb | \u2502",
                    "  \u2502 |\u2584\u2584\u2584\u2584| \u2502",
                    "  \u2502  \u2502\u2551\u2551\u2502  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Angry] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 |\u25a0  \u25a0| \u2502",
                    "  \u2502 |  ..  | \u2502",
                    "  \u2502 | \u2501\u2501 | \u2502",
                    "  \u2502 |\u2584\u2584\u2584\u2584| \u2502",
                    "  \u2502  \u2502\u2551\u2551\u2502  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Defeated] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 |\u2013  \u2013| \u2502",
                    "  \u2502 |  ..  | \u2502",
                    "  \u2502 | \u256e\u256d | \u2502",
                    "  \u2502 |\u2584\u2584\u2584\u2584| \u2502",
                    "  \u2502  \u2502\u2551\u2551\u2502  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518"),
                [NPCExpression.Victorious] = string.Join("\n",
                    "  \u250c\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510",
                    "  \u2502 [\u2261\u2261\u2261\u2261] \u2502",
                    "  \u2502 |\u0302  \u0302| \u2502",
                    "  \u2502 |  ..  | \u2502",
                    "  \u2502 | \u2580\u2580 | \u2502",
                    "  \u2502 |\u2584\u2584\u2584\u2584| \u2502",
                    "  \u2502  \u2502\u2551\u2551\u2502  \u2502",
                    "  \u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518")
            }
        };

        // Boss uses the Riddlemaster frame but with different default expression
        private static readonly Dictionary<NPCExpression, string> BossPortraits = new()
        {
            [NPCExpression.Neutral] = string.Join("\n",
                "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                "  \u2551 /\u2593\u2593\u2593\\ \u2551",
                "  \u2551\u2502 \u2620  \u2620 \u2502\u2551",
                "  \u2551\u2502  \u25c6  \u2502\u2551",
                "  \u2551\u2502 \u2261\u2261\u2261 \u2502\u2551",
                "  \u2551 \\\u2593\u2593\u2593/ \u2551",
                "  \u2551  \u2503\u2503   \u2551",
                "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
            [NPCExpression.Amused] = string.Join("\n",
                "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                "  \u2551 /\u2593\u2593\u2593\\ \u2551",
                "  \u2551\u2502 \u02d8  \u02d8 \u2502\u2551",
                "  \u2551\u2502  \u25c6  \u2502\u2551",
                "  \u2551\u2502 \u256d\u2501\u256e \u2502\u2551",
                "  \u2551 \\\u2593\u2593\u2593/ \u2551",
                "  \u2551  \u2503\u2503   \u2551",
                "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
            [NPCExpression.Impressed] = string.Join("\n",
                "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                "  \u2551 /\u2593\u2593\u2593\\ \u2551",
                "  \u2551\u2502 \u25c9  \u25c9 \u2502\u2551",
                "  \u2551\u2502  \u25c6  \u2502\u2551",
                "  \u2551\u2502 \u25cb\u25cb\u25cb \u2502\u2551",
                "  \u2551 \\\u2593\u2593\u2593/ \u2551",
                "  \u2551  \u2503\u2503   \u2551",
                "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
            [NPCExpression.Angry] = string.Join("\n",
                "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                "  \u2551 /\u2593\u2593\u2593\\ \u2551",
                "  \u2551\u2502 \u2620  \u2620 \u2502\u2551",
                "  \u2551\u2502  \u25c6  \u2502\u2551",
                "  \u2551\u2502 \u2501\u2501\u2501 \u2502\u2551",
                "  \u2551 \\\u2593\u2593\u2593/ \u2551",
                "  \u2551  \u2503\u2503   \u2551",
                "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
            [NPCExpression.Defeated] = string.Join("\n",
                "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                "  \u2551 /\u2593\u2593\u2593\\ \u2551",
                "  \u2551\u2502 x  x \u2502\u2551",
                "  \u2551\u2502  \u25c6  \u2502\u2551",
                "  \u2551\u2502 \u256e\u2500\u256d \u2502\u2551",
                "  \u2551 \\\u2593\u2593\u2593/ \u2551",
                "  \u2551  \u2503\u2503   \u2551",
                "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d"),
            [NPCExpression.Victorious] = string.Join("\n",
                "  \u2554\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2557",
                "  \u2551 /\u2593\u2593\u2593\\ \u2551",
                "  \u2551\u2502 \u0302  \u0302 \u2502\u2551",
                "  \u2551\u2502  \u25c6  \u2502\u2551",
                "  \u2551\u2502 \u2580\u2580\u2580 \u2502\u2551",
                "  \u2551 \\\u2593\u2593\u2593/ \u2551",
                "  \u2551  \u2503\u2503   \u2551",
                "  \u255a\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u255d")
        };

        /// <summary>
        /// Gets the ASCII art portrait for a given archetype and expression.
        /// Boss NPCs get a special double-border frame.
        /// </summary>
        public static string GetPortrait(NPCArchetype archetype, NPCExpression expression, bool isBoss = false)
        {
            if (isBoss && BossPortraits.TryGetValue(expression, out var bossArt))
                return bossArt;

            if (Portraits.TryGetValue(archetype, out var expressions) &&
                expressions.TryGetValue(expression, out var art))
                return art;

            // Fallback: neutral Riddlemaster
            return Portraits[NPCArchetype.Riddlemaster][NPCExpression.Neutral];
        }
    }
}
