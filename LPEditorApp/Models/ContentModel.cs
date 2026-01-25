using System.Text.Json.Serialization;

namespace LPEditorApp.Models;

public class MetaModel
{
    [JsonPropertyName("pageTitle")]
    public string PageTitle { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class CampaignModel
{
    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = "YYYY-MM-DD";

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = "YYYY-MM-DD";

    [JsonPropertyName("countdownEnd")]
    public string CountdownEnd { get; set; } = "YYYY-MM-DDT23:59:59";

    [JsonPropertyName("showCountdown")]
    public bool ShowCountdown { get; set; } = true;

    [JsonPropertyName("endedMessage")]
    public string EndedMessage { get; set; } = "キャンペーン終了しました";

    [JsonPropertyName("footerFontFamily")]
    public string? FooterFontFamily { get; set; }

    [JsonPropertyName("footerLines")]
    public List<StyledTextItem> FooterLines { get; set; } = new();
}

public class HeaderModel
{
    [JsonPropertyName("logoImage")]
    public string LogoImage { get; set; } = "images/logo.png";

    [JsonPropertyName("logoAlt")]
    public string LogoAlt { get; set; } = string.Empty;
}

public class HeroModel
{
    [JsonPropertyName("imagePc")]
    public string ImagePc { get; set; } = "images/mv.png";

    [JsonPropertyName("imageSp")]
    public string ImageSp { get; set; } = "images/mv_sp.png";

    [JsonPropertyName("alt")]
    public string Alt { get; set; } = string.Empty;
}

public class TextItemModel
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("emphasis")]
    public bool Emphasis { get; set; }

    [JsonPropertyName("bold")]
    public bool Bold { get; set; }

    [JsonPropertyName("useColor")]
    public bool UseColor { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("fontSize")]
    public int? FontSize { get; set; }
}

public class CampaignContentModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public List<TextItemModel> Notes { get; set; } = new();

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("textAlign")]
    public string? TextAlign { get; set; }
}

public class CampaignStyleModel
{
    [JsonPropertyName("boxColor")]
    public string BoxColor { get; set; } = string.Empty;

    [JsonPropertyName("headingColor")]
    public string HeadingColor { get; set; } = string.Empty;

    [JsonPropertyName("headingBackgroundColor")]
    public string HeadingBackgroundColor { get; set; } = string.Empty;

    [JsonPropertyName("frameBorderColor")]
    public string FrameBorderColor { get; set; } = string.Empty;

    [JsonPropertyName("textColor")]
    public string TextColor { get; set; } = string.Empty;

    [JsonPropertyName("mvFooterBackgroundColor")]
    public string MvFooterBackgroundColor { get; set; } = string.Empty;

    [JsonPropertyName("backgroundPreset")]
    public string BackgroundPreset { get; set; } = string.Empty;

    [JsonPropertyName("backgroundColorA")]
    public string BackgroundColorA { get; set; } = string.Empty;

    [JsonPropertyName("backgroundColorB")]
    public string BackgroundColorB { get; set; } = string.Empty;

    [JsonPropertyName("mobileAutoPadding")]
    public bool MobileAutoPadding { get; set; }

    [JsonPropertyName("mobileAutoFont")]
    public bool MobileAutoFont { get; set; }
}

public class LpBackgroundModel
{
    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = "solid";

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "color";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "transparent";

    [JsonPropertyName("colorOpacity")]
    public double? ColorOpacity { get; set; }

    [JsonPropertyName("gradientType")]
    public string GradientType { get; set; } = "linear";

    [JsonPropertyName("gradientAngle")]
    public double? GradientAngle { get; set; }

    [JsonPropertyName("gradientColorA")]
    public string GradientColorA { get; set; } = string.Empty;

    [JsonPropertyName("gradientColorB")]
    public string GradientColorB { get; set; } = string.Empty;

    [JsonPropertyName("gradientOpacity")]
    public double? GradientOpacity { get; set; }

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("videoUrl")]
    public string VideoUrl { get; set; } = string.Empty;

    [JsonPropertyName("videoPoster")]
    public string VideoPoster { get; set; } = string.Empty;

    [JsonPropertyName("repeat")]
    public string Repeat { get; set; } = "repeat";

    [JsonPropertyName("position")]
    public string Position { get; set; } = "center top";

    [JsonPropertyName("positionCustom")]
    public string PositionCustom { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = "cover";

    [JsonPropertyName("sizeCustom")]
    public string SizeCustom { get; set; } = string.Empty;

    [JsonPropertyName("attachment")]
    public string Attachment { get; set; } = "scroll";

    [JsonPropertyName("transparentSections")]
    public bool TransparentSections { get; set; }

    [JsonPropertyName("preset")]
    public BackgroundPresetSelection Preset { get; set; } = new();

    [JsonPropertyName("effects")]
    public BackgroundEffects Effects { get; set; } = new();
}

public class SectionBackgroundSettings
{
    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = "solid";
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "inherit";

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("colorOpacity")]
    public double? ColorOpacity { get; set; }

    [JsonPropertyName("gradientType")]
    public string GradientType { get; set; } = "linear";

    [JsonPropertyName("gradientAngle")]
    public double? GradientAngle { get; set; }

    [JsonPropertyName("gradientColorA")]
    public string GradientColorA { get; set; } = string.Empty;

    [JsonPropertyName("gradientColorB")]
    public string GradientColorB { get; set; } = string.Empty;

    [JsonPropertyName("gradientOpacity")]
    public double? GradientOpacity { get; set; }

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("videoUrl")]
    public string VideoUrl { get; set; } = string.Empty;

    [JsonPropertyName("videoPoster")]
    public string VideoPoster { get; set; } = string.Empty;

    [JsonPropertyName("repeat")]
    public string Repeat { get; set; } = "repeat";

    [JsonPropertyName("position")]
    public string Position { get; set; } = "center top";

    [JsonPropertyName("positionCustom")]
    public string PositionCustom { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = "cover";

    [JsonPropertyName("sizeCustom")]
    public string SizeCustom { get; set; } = string.Empty;

    [JsonPropertyName("attachment")]
    public string Attachment { get; set; } = "scroll";

    [JsonPropertyName("preset")]
    public BackgroundPresetSelection Preset { get; set; } = new();

    [JsonPropertyName("effects")]
    public BackgroundEffects Effects { get; set; } = new();
}

public class CouponNotesModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<TextItemModel> Items { get; set; } = new();

    // 旧 Items(TextItemModel) -> 新 TextLines(StyledTextItem) への移行用
    [JsonPropertyName("textLines")]
    public List<StyledTextItem> TextLines { get; set; } = new();

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("textAlign")]
    public string? TextAlign { get; set; }
}

public class CouponPeriodModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("inputMode")]
    public string InputMode { get; set; } = "link";

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("textAlign")]
    public string? TextAlign { get; set; }
}

public class StoreSearchModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("title")]
    public string Title { get; set; } = "キャンペーン対象店舗検索";

    [JsonPropertyName("noticeTitle")]
    public string NoticeTitle { get; set; } = "⚠️ ご注意ください！";

    [JsonPropertyName("noticeItems")]
    public List<TextItemModel> NoticeItems { get; set; } = new()
    {
        new() { Text = "リストに記載があっても、店舗の休業・閉業・移転や、その他の事情により利用できない場合があります。", Emphasis = false },
        new() { Text = "キャンペーン対象店舗であっても、一部掲載していない店舗もございます。", Emphasis = false },
        new() { Text = "データ連携のタイムラグ等により、キャッシュレス決済アプリ内の情報と一部異なる場合があります。", Emphasis = false },
        new() { Text = "店舗は随時追加・更新いたします。", Emphasis = false },
        new() { Text = "一部対象外商品、サービスがあります。", Emphasis = false }
    };

    // 旧 NoticeItems(TextItemModel) -> 新 NoticeLines(StyledTextItem) への移行用
    [JsonPropertyName("noticeLines")]
    public List<StyledTextItem> NoticeLines { get; set; } = new();

    [JsonPropertyName("stores")]
    public List<StoreItemModel> Stores { get; set; } = new();

    [JsonPropertyName("targetLabels")]
    public List<StoreTargetLabelModel> TargetLabels { get; set; } = new();

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("textAlign")]
    public string? TextAlign { get; set; }
}

public class StoreTargetLabelModel
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("backgroundColor")]
    public string BackgroundColor { get; set; } = string.Empty;

    [JsonPropertyName("borderColor")]
    public string BorderColor { get; set; } = string.Empty;

    [JsonPropertyName("textColor")]
    public string TextColor { get; set; } = string.Empty;

    [JsonPropertyName("backgroundOpacity")]
    public double? BackgroundOpacity { get; set; }

    [JsonPropertyName("borderOpacity")]
    public double? BorderOpacity { get; set; }

    [JsonPropertyName("textOpacity")]
    public double? TextOpacity { get; set; }

    [JsonPropertyName("borderWidth")]
    public int? BorderWidth { get; set; }

    [JsonPropertyName("fontSize")]
    public int? FontSize { get; set; }

    [JsonPropertyName("fontBold")]
    public bool? FontBold { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class StoreItemModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("zip")]
    public string Zip { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("couponTarget")]
    public bool CouponTarget { get; set; }

    [JsonPropertyName("raffleTarget")]
    public bool RaffleTarget { get; set; }

    [JsonPropertyName("campaignTarget")]
    public bool CampaignTarget { get; set; }

    [JsonPropertyName("targets")]
    public Dictionary<string, bool> Targets { get; set; } = new();
}

public class RankingRowModel
{
    [JsonPropertyName("rank")]
    public string Rank { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public string Items { get; set; } = string.Empty;
}

public class RankingTextItemModel
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("emphasis")]
    public bool Emphasis { get; set; }

    [JsonPropertyName("bold")]
    public bool Bold { get; set; }

    [JsonPropertyName("useColor")]
    public bool UseColor { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("fontSize")]
    public int? FontSize { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;
}

public partial class RankingSectionModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("title")]
    public string Title { get; set; } = "最新順位はこちら";

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [JsonPropertyName("titleLines")]
    public List<StyledTextItem> TitleLines { get; set; } = new();

    [JsonPropertyName("subtitleLines")]
    public List<StyledTextItem> SubtitleLines { get; set; } = new();

    [JsonPropertyName("periodText")]
    public string PeriodText { get; set; } = string.Empty;

    [JsonPropertyName("asOfText")]
    public string AsOfText { get; set; } = string.Empty;

    [JsonPropertyName("imageLeft")]
    public string ImageLeft { get; set; } = "images/ranking-left.png";

    [JsonPropertyName("imageRight")]
    public string ImageRight { get; set; } = "images/ranking-right.png";

    [JsonPropertyName("rankLabel")]
    public string RankLabel { get; set; } = "順位";

    [JsonPropertyName("amountLabel")]
    public string AmountLabel { get; set; } = "決済金額";

    [JsonPropertyName("itemsLabel")]
    public string ItemsLabel { get; set; } = "品数";

    [JsonPropertyName("headerLabels")]
    public List<string> HeaderLabels { get; set; } = new();

    [JsonPropertyName("tablePreset")]
    public string TablePreset { get; set; } = string.Empty;

    [JsonPropertyName("tableBorderWidth")]
    public int? TableBorderWidth { get; set; }

    [JsonPropertyName("tableHeaderColor")]
    public string TableHeaderColor { get; set; } = string.Empty;

    [JsonPropertyName("tableHeaderTextColor")]
    public string TableHeaderTextColor { get; set; } = string.Empty;

    [JsonPropertyName("tableBorderColor")]
    public string TableBorderColor { get; set; } = string.Empty;

    [JsonPropertyName("tableStripeColor")]
    public string TableStripeColor { get; set; } = string.Empty;

    [JsonPropertyName("tableTextColor")]
    public string TableTextColor { get; set; } = string.Empty;

    [JsonPropertyName("tableTextBold")]
    public bool TableTextBold { get; set; }

    [JsonPropertyName("tableTextStrokeColor")]
    public string TableTextStrokeColor { get; set; } = string.Empty;

    [JsonPropertyName("tableWidthPercent")]
    public int? TableWidthPercent { get; set; } = 86;

    [JsonPropertyName("tableFontFamily")]
    public string? TableFontFamily { get; set; }

    [JsonPropertyName("showCrowns")]
    public bool ShowCrowns { get; set; } = true;

    [JsonPropertyName("periodLabel")]
    public string PeriodLabel { get; set; } = "集計期間";

    [JsonPropertyName("showPeriodLabel")]
    public bool ShowPeriodLabel { get; set; } = true;

    [JsonPropertyName("periodAlign")]
    public string PeriodAlign { get; set; } = "center";

    [JsonPropertyName("asOfAlign")]
    public string AsOfAlign { get; set; } = "center";

    [JsonPropertyName("tableNotes")]
    public string TableNotes { get; set; } = string.Empty;

    [JsonPropertyName("textAlign")]
    public string? TextAlign { get; set; }

    [JsonPropertyName("rows")]
    public List<RankingRowModel> Rows { get; set; } = new();

    [JsonPropertyName("texts")]
    public List<RankingTextItemModel> Texts { get; set; } = new();

    [JsonPropertyName("freeTexts")]
    public List<StyledTextItem> FreeTexts { get; set; } = new();

    [JsonPropertyName("notesItems")]
    public List<StyledTextItem> NotesItems { get; set; } = new();

    [JsonPropertyName("subtitleUnderItems")]
    public List<StyledTextItem> SubtitleUnderItems { get; set; } = new();

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("titleStyle")]
    public TextStyleModel TitleStyle { get; set; } = new();

    [JsonPropertyName("subtitleStyle")]
    public TextStyleModel SubtitleStyle { get; set; } = new();

    [JsonPropertyName("periodStyle")]
    public TextStyleModel PeriodStyle { get; set; } = new();

    [JsonPropertyName("periodLabelStyle")]
    public TextStyleModel PeriodLabelStyle { get; set; } = new();

    [JsonPropertyName("asOfStyle")]
    public TextStyleModel AsOfStyle { get; set; } = new();

    [JsonPropertyName("notesStyle")]
    public TextStyleModel NotesStyle { get; set; } = new();
}

public class TextStyleModel
{
    [JsonPropertyName("fontSize")]
    public int? FontSize { get; set; }

    [JsonPropertyName("bold")]
    public bool Bold { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

public class StyledTextItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("fontSize")]
    public int? FontSize { get; set; }

    [JsonPropertyName("bold")]
    public bool Bold { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("align")]
    public string? Align { get; set; }
}

public class CustomSectionModel
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    // 旧 Body(textarea) -> 新 BodyTextItems(StyledTextItem) への移行用
    [JsonPropertyName("bodyTextItems")]
    public List<StyledTextItem> BodyTextItems { get; set; } = new();

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    [JsonPropertyName("imageAlt")]
    public string ImageAlt { get; set; } = string.Empty;

    [JsonPropertyName("linkUrl")]
    public string LinkUrl { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    // 旧 Notes(textarea) -> 新 ImageNotesItems(StyledTextItem) への移行用
    [JsonPropertyName("imageNotesItems")]
    public List<StyledTextItem> ImageNotesItems { get; set; } = new();

    // 旧 Body(textarea) -> 新 AdditionalTextItems(StyledTextItem) への移行用
    [JsonPropertyName("additionalTextItems")]
    public List<StyledTextItem> AdditionalTextItems { get; set; } = new();

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("textAlign")]
    public string? TextAlign { get; set; }
}

public class ConditionsModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("titleImage")]
    public string TitleImage { get; set; } = "images/title-conditions.png";

    [JsonPropertyName("textImage")]
    public string TextImage { get; set; } = "images/text-conditions.png";

    [JsonPropertyName("deviceText")]
    public string DeviceText { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<TextItemModel> Items { get; set; } = new();

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("textAlign")]
    public string? TextAlign { get; set; }
}

public class ButtonItemModel
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("emphasis")]
    public bool Emphasis { get; set; }
}

public class ContactModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("lead")]
    public string Lead { get; set; } = string.Empty;

    [JsonPropertyName("buttons")]
    public List<ButtonItemModel> Buttons { get; set; } = new();

    [JsonPropertyName("officeHours")]
    public string OfficeHours { get; set; } = string.Empty;

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("textAlign")]
    public string? TextAlign { get; set; }
}

public class BannerModel
{
    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class BannersModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("main")]
    public BannerModel Main { get; set; } = new();

    [JsonPropertyName("magazine")]
    public BannerModel Magazine { get; set; } = new();

    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("textAlign")]
    public string? TextAlign { get; set; }
}

public class SectionGroupModel
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

public class SectionsModel
{
    [JsonPropertyName("campaignContent")]
    public CampaignContentModel CampaignContent { get; set; } = new();

    [JsonPropertyName("couponPeriod")]
    public CouponPeriodModel CouponPeriod { get; set; } = new();

    [JsonPropertyName("storeSearch")]
    public StoreSearchModel StoreSearch { get; set; } = new();

    [JsonPropertyName("couponNotes")]
    public CouponNotesModel CouponNotes { get; set; } = new();

    [JsonPropertyName("ranking")]
    public RankingSectionModel Ranking { get; set; } = new();

    [JsonPropertyName("conditions")]
    public ConditionsModel Conditions { get; set; } = new();

    [JsonPropertyName("contact")]
    public ContactModel Contact { get; set; } = new();

    [JsonPropertyName("banners")]
    public BannersModel Banners { get; set; } = new();
}

public class ContentModel
{
    [JsonPropertyName("templateId")]
    public string TemplateId { get; set; } = "coupon_lp_0122";

    [JsonPropertyName("sectionsOrder")]
    public List<string> SectionsOrder { get; set; } = new() { "campaignContent", "couponPeriod", "storeSearch", "couponNotes", "ranking", "countdown" };

    [JsonPropertyName("sectionGroups")]
    public List<SectionGroupModel> SectionGroups { get; set; } = new();

    [JsonPropertyName("meta")]
    public MetaModel Meta { get; set; } = new();

    [JsonPropertyName("campaign")]
    public CampaignModel Campaign { get; set; } = new();

    [JsonPropertyName("header")]
    public HeaderModel Header { get; set; } = new();

    [JsonPropertyName("hero")]
    public HeroModel Hero { get; set; } = new();

    [JsonPropertyName("sections")]
    public SectionsModel Sections { get; set; } = new();

    [JsonPropertyName("customSections")]
    public List<CustomSectionModel> CustomSections { get; set; } = new();

    [JsonPropertyName("deletedImages")]
    public List<string> DeletedImages { get; set; } = new();

    [JsonPropertyName("campaignStyle")]
    public CampaignStyleModel CampaignStyle { get; set; } = new();

    [JsonPropertyName("lpBackground")]
    public LpBackgroundModel LpBackground { get; set; } = new();

    [JsonPropertyName("sectionBackgrounds")]
    public Dictionary<string, SectionBackgroundSettings> SectionBackgrounds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("sectionStyles")]
    public Dictionary<string, SectionStyleModel> SectionStyles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("sectionAnimations")]
    public Dictionary<string, SectionAnimationModel> SectionAnimations { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("pageEffects")]
    public PageEffectsSetting PageEffects { get; set; } = new();

}
