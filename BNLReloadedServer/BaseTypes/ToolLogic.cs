namespace BNLReloadedServer.BaseTypes;

public class ToolLogic(GearData data, byte index)
{
    public byte ToolIndex = index;
    public bool IsBreakUse;
    public bool IsReleaseUse;

    public GearData Gear { get; private set; } = data;

    public Tool? Tool => Gear.Card.Tools?[ToolIndex];

    public Unit Unit => Gear.Unit;

    public GearAmmo? GetAmmoData()
    {
        return Tool?.Ammo != null ? Gear.Ammo[Tool.Ammo.AmmoIndex] : null;
    }

    public void TakeAmmo(float? rate = null) => GetAmmoData()?.TakeAmmo(rate ?? Tool?.Ammo?.Rate ?? 0);
    
    public Ammo? TakeAmmoUpdate(float? rate = null) => GetAmmoData()?.TakeAmmoUpdate(rate ?? Tool?.Ammo?.Rate ?? 0);

    public bool IsEnoughAmmoToUse()
    {
        var ammoData = GetAmmoData();
        return ammoData == null || ammoData.IsEnoughAmmoToUse(Tool?.Ammo?.Rate ?? 0);
    }

    public bool IsAvailableToEquip()
    {
        var ammoData = GetAmmoData();
        return ammoData == null || !ammoData.IsOutOfAmmo(Tool?.Ammo?.Rate ?? 0);
    }

    public bool IsOutOfAmmo()
    {
        var ammoData = GetAmmoData();
        return ammoData != null && ammoData.IsOutOfAmmo(Tool?.Ammo?.Rate ?? 0);
    }

    public bool IsRequireReloading()
    {
        var ammoData = GetAmmoData();
        return ammoData != null && ammoData.IsRequireToReload(Tool?.Ammo?.Rate ?? 0);
    }

    public bool IsPossibleToReload()
    {
        var ammoData = GetAmmoData();
        return ammoData != null && ammoData.IsPossibleToReload(Tool?.Ammo?.Rate ?? 0);
    }

    public bool IsAutoReloadLastShot()
    {
        return Tool?.Ammo is { AutoReloadAfterLastShot: true };
    }

    public bool IsAutoReloadOnEmptyGunFire()
    {
        return Tool?.Ammo is { AutoReloadOnEmptyGunFire: true };
    }

    public bool IsAutoReloadOnEmptyGunSwitch()
    {
      return Tool?.Ammo is { AutoReloadOnEmptyGunSwitch: true };
    }

    public void Equip()
    {
    }

    public void Unequip()
    {
    }

    public bool ValidateUse()
    {
        if (Tool is ToolAiming)
        {
            return true;
        }
        return DateTimeOffset.Now.ToUnixTimeMilliseconds() > Gear.LastShotEndTime;
    }

    public void OnStartUse()
    {
    }

    public void OnEndUse()
    {
    }

    protected Unit GetUnit() => Gear.Unit;
}