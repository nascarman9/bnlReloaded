using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<ShopCategory>))]
public abstract class ShopCategory : IJsonFactory<ShopCategory>
{
    public abstract ShopCategoryType Type { get; }

    public LocalizedString? Name { get; set; }

    public List<Key>? Items { get; set; }

    public ShopCategoryLabel Label { get; set; } = ShopCategoryLabel.None;

    public static ShopCategory CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<ShopCategoryType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, ShopCategory value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static ShopCategory ReadVariant(BinaryReader reader)
    {
        var shopCategory = Create(reader.ReadByteEnum<ShopCategoryType>());
        shopCategory.Read(reader);
        return shopCategory;
    }

    public static ShopCategory Create(ShopCategoryType type)
    {
        return type switch
        {
            ShopCategoryType.Common => new ShopCategoryCommon(),
            ShopCategoryType.Featured => new ShopCategoryFeatured(),
            ShopCategoryType.Bundles => new ShopCategoryBundles(),
            ShopCategoryType.Perks => new ShopCategoryPerks(),
            ShopCategoryType.Boosters => new ShopCategoryBoosters(),
            ShopCategoryType.Lootboxes => new ShopCategoryLootboxes(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}