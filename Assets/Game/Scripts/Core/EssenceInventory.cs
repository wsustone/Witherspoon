using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Core
{
    public class EssenceInventory : MonoBehaviour
    {
        private readonly Dictionary<EssenceDefinition, int> _counts = new();

        public int GetCount(EssenceDefinition essence)
        {
            if (essence == null) return 0;
            return _counts.TryGetValue(essence, out var c) ? c : 0;
        }

        public void AddEssence(EssenceDefinition essence, int amount)
        {
            if (essence == null || amount <= 0) return;
            _counts.TryGetValue(essence, out var c);
            _counts[essence] = c + amount;
        }

        public bool TryConsume(EssenceDefinition essence, int amount)
        {
            if (essence == null || amount <= 0) return false;
            _counts.TryGetValue(essence, out var c);
            if (c < amount) return false;
            c -= amount;
            if (c <= 0) _counts.Remove(essence);
            else _counts[essence] = c;
            return true;
        }
    }
}
