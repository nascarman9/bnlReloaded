using BNLReloadedServer.Database;

namespace BNLReloadedServer.BaseTypes;

public class GearData
{
  public Key Key;
  public int Index;
  public List<GearAmmo> Ammo = [];
  public List<ToolLogic> Tools = [];
  public Unit Unit;
  public float LastShotEndTime;

  public GearData(Unit unit, Key key, int index)
  {
    Unit = unit;
    Key = key;
    Index = index;
    if (Card.Tools != null)
    {
        for (var index1 = 0; index1 < Card.Tools.Count; ++index1)
          Tools.Add(Card.Tools[index1].CreateToolLogic(this, (byte) index1));
    }
    if (Card.Ammo == null)
      return;
    for (var index2 = 0; index2 < Card.Ammo.Count; ++index2)
      Ammo.Add(new GearAmmo(unit, Key, index2));
  }

  public CardGear Card => Databases.Catalogue.GetCard<CardGear>(Key)!;

  public void ServerUpdateAmmo(List<Ammo> ammo)
  {
    foreach (var ammo1 in ammo)
      Ammo[ammo1.Index].ServerUpdateAmmo(ammo1);
  }

  public ToolLogic? GetTool(byte toolIndex)
  {
    return toolIndex < Tools.Count ? Tools[toolIndex] : null;
  }

  public void Equip()
  {
    foreach (var tool in Tools)
      tool.Equip();
  }

  public void Unequip()
  {
    foreach (var tool in Tools)
      tool.Unequip();
  }

  public bool IsAvailableToEquip()
  {
    return Tools.Any(tool => tool.IsAvailableToEquip());
  }

  public bool IsOutOfAmmo()
  {
    return Tools.All(tool => tool.IsOutOfAmmo());
  }

  public bool IsReloadOnSwitchRequired()
  {
    return Tools.Any(tool => tool.IsAutoReloadOnEmptyGunSwitch() && tool.IsRequireReloading());
  }

  public bool IsAllToolsRequireReload()
  {
    return Tools.All(tool => tool.IsRequireReloading());
  }

  public bool IsPossibleToReload()
  {
    return Card.Reload != null && Tools.Any(tool => tool.IsPossibleToReload());
  }

  public float GetReloadTime()
  {
    if (Card.Reload == null)
      return 0.0f;
    return Card.Reload.Type switch
    {
      ReloadType.FullClip => (Card.Reload! as ReloadFullClip).ReloadTime,
      ReloadType.Partial => (Card.Reload! as ReloadPartial).ReloadTime,
      _ => 0.0f
    };
  }
}