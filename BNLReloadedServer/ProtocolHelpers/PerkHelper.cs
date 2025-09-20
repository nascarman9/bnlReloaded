using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;

namespace BNLReloadedServer.ProtocolHelpers;

public static class PerkHelper
{
    public static List<Key> ConvertGear(this List<Key> gearList, List<Key> perkList)
    {
        var result = new List<Key>(gearList);
        foreach (var perk in perkList)
        {
            if (Databases.Catalogue.GetCard<CardPerk>(perk) is not { PerkMods: not null } cp) continue;
            foreach (var mod in cp.PerkMods)
            {
                if (mod is PerkModGear gearMod && gearList.Contains(gearMod.ReplaceFrom) && result.Contains(gearMod.ReplaceFrom))
                {
                    result[result.IndexOf(gearMod.ReplaceFrom)] = gearMod.ReplaceTo;
                }        
            }
        }
        
        return result;
    }

    public static Dictionary<Key, ulong?> ExtractEffects(List<Key> perkList)
    {
        var effectResult = new Dictionary<Key, ulong?>();
        foreach (var perk in perkList)
        {
            if (Databases.Catalogue.GetCard<CardPerk>(perk) is not { PerkMods: not null } cp) continue;
            foreach (var mod in cp.PerkMods)
            {
                if (mod is not PerkModEffect { Constant: not null } effectMod) continue;
                foreach (var effect in effectMod.Constant)
                {
                    var card = Databases.Catalogue.GetCard<CardEffect>(effect);
                    if (card is null) continue;
                    if (effectResult.ContainsKey(effect))
                    {
                        effectResult[effect] += (ulong?) card.Duration;
                    }
                    else
                    {
                        effectResult.Add(effect, (ulong?) card.Duration);
                    }
                }
            }
        }
        
        return effectResult;
    }

    public static List<Key> ConvertPassives(this List<Key> passives, List<Key> perkList)
    {
        var result = new List<Key>(passives);
        var replIdx = 0;
        foreach (var perk in perkList)
        {
            if (Databases.Catalogue.GetCard<CardPerk>(perk) is not { PerkMods: not null } cp) continue;
            foreach (var mod in cp.PerkMods)
            {
                if (mod is not PerkModPassive { ReplaceEffects: not null }passive) continue;
                foreach (var pass in passive.ReplaceEffects)
                {
                    if (replIdx >= result.Count)
                    {
                        result.Add(pass);
                    }
                    else
                    {
                        result[replIdx] = pass;
                    }
                    replIdx++;
                }
            }
        }
        
        return result;
    }

    public static Key ConvertAbility(this Key ability, List<Key> perkList)
    {
        foreach (var perk in perkList)
        {
            if (Databases.Catalogue.GetCard<CardPerk>(perk) is not { PerkMods: not null } cp) continue;
            foreach (var mod in cp.PerkMods)
            {
                if (mod is not PerkModAbility abilityMod) continue;
                return abilityMod.ReplaceAbility;
            }
        }
        
        return ability;
    }

    public static Dictionary<int, Key> ConvertDevices(this Dictionary<int, Key> devices, List<Key> perkList)
    {
        var result = new Dictionary<int, Key>(devices);
        foreach (var perk in perkList)
        {
            if (Databases.Catalogue.GetCard<CardPerk>(perk) is not { PerkMods: not null } cp) continue;
            foreach (var mod in cp.PerkMods)
            {
                if (mod is not PerkModDevice { ReplaceFrom: not null } deviceMod) continue;
                foreach (var device in deviceMod.ReplaceFrom.Where(device => devices.ContainsValue(device) && result.ContainsValue(device)))
                {
                    result[result.First(kv => kv.Value == device).Key] = deviceMod.ReplaceTo;
                }
            }
        }
        
        return result;
    }
}