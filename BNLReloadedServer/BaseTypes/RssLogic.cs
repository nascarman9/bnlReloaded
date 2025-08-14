using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RssLogic
{
    public LocalizedString? ArticlesUrl { get; set; }

    public LocalizedString? PromoBoxUrl { get; set; }

    public LocalizedString? BannerUrl { get; set; }

    public LocalizedString? DemoArticlesUrl { get; set; }

    public LocalizedString? DemoPromoBoxUrl { get; set; }

    public LocalizedString? DemoBannerUrl { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(ArticlesUrl != null, PromoBoxUrl != null, BannerUrl != null, DemoArticlesUrl != null,
        DemoPromoBoxUrl != null, DemoBannerUrl != null).Write(writer);
      if (ArticlesUrl != null)
        LocalizedString.WriteRecord(writer, ArticlesUrl);
      if (PromoBoxUrl != null)
        LocalizedString.WriteRecord(writer, PromoBoxUrl);
      if (BannerUrl != null)
        LocalizedString.WriteRecord(writer, BannerUrl);
      if (DemoArticlesUrl != null)
        LocalizedString.WriteRecord(writer, DemoArticlesUrl);
      if (DemoPromoBoxUrl != null)
        LocalizedString.WriteRecord(writer, DemoPromoBoxUrl);
      if (DemoBannerUrl != null)
        LocalizedString.WriteRecord(writer, DemoBannerUrl);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      ArticlesUrl = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      PromoBoxUrl = bitField[1] ? LocalizedString.ReadRecord(reader) : null;
      BannerUrl = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      DemoArticlesUrl = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      DemoPromoBoxUrl = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      DemoBannerUrl = bitField[5] ? LocalizedString.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, RssLogic value) => value.Write(writer);

    public static RssLogic ReadRecord(BinaryReader reader)
    {
      var rssLogic = new RssLogic();
      rssLogic.Read(reader);
      return rssLogic;
    }
}