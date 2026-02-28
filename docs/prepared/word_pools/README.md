# Spellwright Word Pools

Curated word dictionary for the Spellwright word-guessing roguelike.

## Format

Each `.txt` file contains one word per line, tab-separated with a difficulty rating:

```
word\tdifficulty
```

- **Difficulty 1:** Very common, 3-5 letters (cat, tree, bread)
- **Difficulty 2:** Common, 4-6 letters (tiger, river, butter)
- **Difficulty 3:** Moderate, 5-7 letters (jaguar, canyon, truffle)
- **Difficulty 4:** Uncommon, 6-9 letters (pangolin, stalactite, prosciutto)
- **Difficulty 5:** Rare/long, 8+ letters (archaeopteryx, bioluminescence)

## Categories

| File | Category | ~Count |
|------|----------|--------|
| `animals.txt` | Animals & creatures | 68 |
| `nature.txt` | Nature & geography | 65 |
| `food.txt` | Food & ingredients | 62 |
| `tools.txt` | Tools & instruments | 62 |
| `emotions.txt` | Emotions & feelings | 64 |
| `science.txt` | Science & technology | 62 |
| `mythology.txt` | Mythology & fantasy | 62 |
| `everyday.txt` | Everyday objects | 71 |

**Total: ~516 words**

## Importing into Unity ScriptableObjects

### 1. Define the ScriptableObject

```csharp
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WordPool", menuName = "Spellwright/Word Pool")]
public class WordPool : ScriptableObject
{
    public string category;

    [System.Serializable]
    public struct WordEntry
    {
        public string word;
        [Range(1, 5)] public int difficulty;
    }

    public List<WordEntry> words = new();

    public List<WordEntry> GetByDifficulty(int level)
    {
        return words.FindAll(w => w.difficulty == level);
    }

    public List<WordEntry> GetByDifficultyRange(int min, int max)
    {
        return words.FindAll(w => w.difficulty >= min && w.difficulty <= max);
    }
}
```

### 2. Editor Import Script

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

public class WordPoolImporter : EditorWindow
{
    [MenuItem("Spellwright/Import Word Pools")]
    static void Import()
    {
        string srcDir = "Assets/Data/WordPools/Raw";   // place .txt files here
        string outDir = "Assets/Data/WordPools";

        foreach (var file in Directory.GetFiles(srcDir, "*.txt"))
        {
            string category = Path.GetFileNameWithoutExtension(file);
            string assetPath = $"{outDir}/{category}.asset";

            var pool = AssetDatabase.LoadAssetAtPath<WordPool>(assetPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<WordPool>();
                AssetDatabase.CreateAsset(pool, assetPath);
            }

            pool.category = category;
            pool.words.Clear();

            foreach (var line in File.ReadAllLines(file))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('\t');
                if (parts.Length < 2) continue;

                pool.words.Add(new WordPool.WordEntry
                {
                    word = parts[0].Trim(),
                    difficulty = int.Parse(parts[1].Trim())
                });
            }

            EditorUtility.SetDirty(pool);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Word pools imported successfully!");
    }
}
```

### 3. Usage

1. Copy the `.txt` files to `Assets/Data/WordPools/Raw/`
2. In Unity: **Spellwright → Import Word Pools**
3. ScriptableObject assets appear in `Assets/Data/WordPools/`
4. Reference them in your game manager or word selection system
