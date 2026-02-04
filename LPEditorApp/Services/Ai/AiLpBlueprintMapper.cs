using System.Linq;
using LPEditorApp.Models;
using LPEditorApp.Models.Ai;

namespace LPEditorApp.Services.Ai;

public class AiLpBlueprintMapper
{
    private const int CouponPeriodTextLimit = 120;
    private const int CampaignBodyAdditionLimit = 140;

    public void ApplyToContent(ContentModel content, LpBlueprint blueprint, TemplateProject? template)
    {
        if (content is null || blueprint is null)
        {
            return;
        }

        var hero = FindSection(blueprint, "hero");
        var offer = FindSection(blueprint, "offer");
        var howto = FindSection(blueprint, "howto");
        var notes = FindSection(blueprint, "notes");
        var footer = FindSection(blueprint, "footer");

        content.Meta.PageTitle = blueprint.Meta?.Title ?? content.Meta.PageTitle;
        content.Meta.Description = BuildDescription(hero);

        var heroApplied = ApplyHero(content, hero);
        var notesApplied = ApplyNotes(content, notes);
        var footerApplied = ApplyFooter(content, footer);
        var howtoApplied = ApplyHowTo(content, howto);
        var offerApplied = ApplyOffer(content, offer, heroApplied, notesApplied);

        content.CustomSections = new List<CustomSectionModel>();

        var sectionKeys = new List<string>();
        if (heroApplied)
        {
            sectionKeys.Add(ResolveSectionKey("campaign-content", "campaignContent", template, content));
        }
        if (offerApplied)
        {
            sectionKeys.Add(ResolveSectionKey("coupon-period", "couponPeriod", template, content));
        }
        if (howtoApplied)
        {
            sectionKeys.Add(ResolveSectionKey("coupon-flow", "couponFlow", template, content));
        }
        if (notesApplied || HasNotes(content))
        {
            sectionKeys.Add(ResolveSectionKey("coupon-notes", "couponNotes", template, content));
        }
        if (footerApplied)
        {
            sectionKeys.Add("countdown");
        }

        content.SectionGroups = sectionKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => new SectionGroupModel { Key = key, Enabled = true })
            .ToList();

        content.Sections.CampaignContent.Enabled = heroApplied;
        content.Sections.CouponPeriod.Enabled = offerApplied;
        content.Sections.CouponFlow.Enabled = howtoApplied;
        content.Sections.CouponNotes.Enabled = notesApplied || HasNotes(content);
        content.Campaign.ShowCountdown = footerApplied;
    }

    private static bool ApplyHero(ContentModel content, LpBlueprintSection? section)
    {
        if (section?.Props is null)
        {
            return false;
        }

        if (!HasSectionContent(section))
        {
            return false;
        }

        content.Sections.CampaignContent.Title = section.Props.Heading ?? string.Empty;
        content.Sections.CampaignContent.Body = CombineText(section.Props.Subheading, section.Props.Body);
        content.Sections.CampaignContent.Notes = (section.Props.Bullets ?? new List<string>())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => new TextItemModel { Text = text })
            .ToList();

        return true;
    }

    private bool ApplyOffer(ContentModel content, LpBlueprintSection? section, bool heroApplied, bool notesApplied)
    {
        if (section?.Props is null)
        {
            return false;
        }

        if (!HasSectionContent(section))
        {
            return false;
        }

        content.Sections.CouponPeriod.Title = section.Props.Heading ?? "オファー";
        content.Sections.CouponPeriod.InputMode = "manual";
        var offerInfo = ExtractOfferInfo(section);
        var periodText = offerInfo.Periods.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(periodText))
        {
            if (periodText.Length > CouponPeriodTextLimit)
            {
                offerInfo.ExtraNotes.Add(periodText[CouponPeriodTextLimit..]);
                periodText = periodText[..CouponPeriodTextLimit];
            }
            content.Sections.CouponPeriod.Text = periodText;
        }
        else
        {
            content.Sections.CouponPeriod.Text = string.Empty;
        }

        if (offerInfo.Benefits.Count > 0)
        {
            var benefitLine = string.Join(" / ", offerInfo.Benefits.Distinct());
            if (benefitLine.Length > CampaignBodyAdditionLimit)
            {
                offerInfo.ExtraNotes.Add(benefitLine);
            }
            else
            {
                var currentBody = content.Sections.CampaignContent.Body ?? string.Empty;
                var append = string.IsNullOrWhiteSpace(currentBody)
                    ? $"特典：{benefitLine}"
                    : currentBody + "\n" + $"特典：{benefitLine}";
                content.Sections.CampaignContent.Body = append;
            }
        }

        var conditionNotes = offerInfo.Conditions.Concat(offerInfo.ExtraNotes).ToList();
        if (conditionNotes.Count > 0)
        {
            AppendNotes(content, conditionNotes, "【利用条件】");
        }

        return !string.IsNullOrWhiteSpace(content.Sections.CouponPeriod.Text);
    }

    private static bool ApplyHowTo(ContentModel content, LpBlueprintSection? section)
    {
        if (section?.Props is null)
        {
            return false;
        }

        if (!HasSectionContent(section))
        {
            return false;
        }

        content.Sections.CouponFlow.Title = section.Props.Heading ?? "ご利用の流れ";
        content.Sections.CouponFlow.Lead = section.Props.Subheading ?? string.Empty;
        content.Sections.CouponFlow.Note = section.Props.Disclaimer ?? string.Empty;
        content.Sections.CouponFlow.ButtonLabel = section.Props.CtaText ?? "詳しく見る";
        content.Sections.CouponFlow.Items = (section.Props.Bullets ?? new List<string>())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => new TextItemModel { Text = text })
            .ToList();

        return true;
    }

    private static bool ApplyNotes(ContentModel content, LpBlueprintSection? section)
    {
        if (section?.Props is null)
        {
            return false;
        }

        if (!HasSectionContent(section))
        {
            return false;
        }

        content.Sections.CouponNotes.Title = section.Props.Heading ?? "注意事項";
        var bullets = section.Props.Bullets ?? new List<string>();
        content.Sections.CouponNotes.TextLines = bullets
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => new StyledTextItem { Text = text })
            .ToList();

        return true;
    }

    private static bool ApplyFooter(ContentModel content, LpBlueprintSection? section)
    {
        if (section?.Props is null)
        {
            return false;
        }

        if (!HasSectionContent(section))
        {
            return false;
        }

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(section.Props.Body))
        {
            lines.Add(section.Props.Body!);
        }
        if (section.Props.Bullets is not null)
        {
            lines.AddRange(section.Props.Bullets.Where(text => !string.IsNullOrWhiteSpace(text)));
        }
        if (!string.IsNullOrWhiteSpace(section.Props.Disclaimer))
        {
            lines.Add(section.Props.Disclaimer!);
        }

        content.Campaign.FooterLines = lines
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => new StyledTextItem { Text = text })
            .ToList();

        return true;
    }

    private static string BuildDescription(LpBlueprintSection? hero)
    {
        if (hero?.Props is null)
        {
            return string.Empty;
        }

        return CombineText(hero.Props.Subheading, hero.Props.Body);
    }

    private static string CombineText(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return second ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(second))
        {
            return first ?? string.Empty;
        }
        return $"{first}\n{second}";
    }

    private static LpBlueprintSection? FindSection(LpBlueprint blueprint, string type)
    {
        return blueprint.Sections?.FirstOrDefault(section => string.Equals(section.Type, type, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasSectionContent(LpBlueprintSection section)
    {
        var props = section.Props;
        if (props is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(props.Heading)
            || !string.IsNullOrWhiteSpace(props.Subheading)
            || !string.IsNullOrWhiteSpace(props.Body)
            || !string.IsNullOrWhiteSpace(props.CtaText)
            || !string.IsNullOrWhiteSpace(props.Disclaimer))
        {
            return true;
        }

        if (props.Bullets?.Any(text => !string.IsNullOrWhiteSpace(text)) == true)
        {
            return true;
        }

        if (props.Items?.Any(item => !string.IsNullOrWhiteSpace(item.Title) || !string.IsNullOrWhiteSpace(item.Text)) == true)
        {
            return true;
        }

        return false;
    }

    private static bool HasNotes(ContentModel content)
    {
        return content.Sections.CouponNotes.TextLines.Any(line => !string.IsNullOrWhiteSpace(line.Text));
    }

    private static void AppendNotes(ContentModel content, IEnumerable<string> lines, string header)
    {
        var notes = content.Sections.CouponNotes;
        notes.TextLines ??= new List<StyledTextItem>();
        if (string.IsNullOrWhiteSpace(notes.Title))
        {
            notes.Title = "注意事項";
        }

        var set = new HashSet<string>(notes.TextLines.Select(line => line.Text.Trim()), StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(header) && !set.Contains(header))
        {
            notes.TextLines.Insert(0, new StyledTextItem { Text = header });
            set.Add(header);
        }

        foreach (var line in lines)
        {
            var cleaned = line?.Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                continue;
            }

            if (set.Add(cleaned))
            {
                notes.TextLines.Add(new StyledTextItem { Text = cleaned });
            }
        }
    }

    private static OfferInfo ExtractOfferInfo(LpBlueprintSection section)
    {
        var info = new OfferInfo();
        var items = section.Props.Items ?? new List<LpBlueprintItem>();

        foreach (var item in items)
        {
            var title = item.Title ?? string.Empty;
            var text = item.Text ?? string.Empty;
            var combined = string.Join(" ", new[] { title, text }.Where(t => !string.IsNullOrWhiteSpace(t)));
            if (string.IsNullOrWhiteSpace(combined))
            {
                continue;
            }

            if (IsPeriodText(combined))
            {
                info.Periods.Add(TextIfEmptyFallback(text, combined));
            }
            else if (IsConditionText(combined))
            {
                info.Conditions.Add(TextIfEmptyFallback(text, combined));
            }
            else if (IsBenefitText(combined))
            {
                info.Benefits.Add(TextIfEmptyFallback(text, combined));
            }
            else
            {
                info.ExtraNotes.Add(TextIfEmptyFallback(text, combined));
            }
        }

        if (items.Count == 0 && section.Props.Bullets is not null)
        {
            foreach (var bullet in section.Props.Bullets)
            {
                if (string.IsNullOrWhiteSpace(bullet))
                {
                    continue;
                }

                if (IsPeriodText(bullet))
                {
                    info.Periods.Add(bullet);
                }
                else if (IsConditionText(bullet))
                {
                    info.Conditions.Add(bullet);
                }
                else if (IsBenefitText(bullet))
                {
                    info.Benefits.Add(bullet);
                }
                else
                {
                    info.ExtraNotes.Add(bullet);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(section.Props.Body))
        {
            info.Benefits.Add(section.Props.Body);
        }

        return info;
    }

    private static string TextIfEmptyFallback(string text, string fallback)
    {
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static bool IsPeriodText(string text)
    {
        var keywords = new[] { "期間", "実施期間", "キャンペーン期間", "開催期間", "利用期間", "有効期限", "期限", "まで" };
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBenefitText(string text)
    {
        var keywords = new[] { "特典", "割引", "ポイント", "還元", "プレゼント", "値引", "OFF", "%" };
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsConditionText(string text)
    {
        var keywords = new[] { "条件", "対象", "回数", "金額", "除外", "上限", "最低", "以上", "未満", "初回", "限定", "先着", "抽選", "併用不可", "利用不可" };
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class OfferInfo
    {
        public List<string> Periods { get; } = new();
        public List<string> Benefits { get; } = new();
        public List<string> Conditions { get; } = new();
        public List<string> ExtraNotes { get; } = new();
    }

    private static string ResolveSectionKey(string kebab, string camel, TemplateProject? template, ContentModel content)
    {
        if (content.SectionGroups.Any(group => string.Equals(group.Key, kebab, StringComparison.OrdinalIgnoreCase)))
        {
            return kebab;
        }
        if (content.SectionGroups.Any(group => string.Equals(group.Key, camel, StringComparison.OrdinalIgnoreCase)))
        {
            return camel;
        }
        if (template is not null && template.SectionGroupKeys.Any(key => string.Equals(key, kebab, StringComparison.OrdinalIgnoreCase)))
        {
            return kebab;
        }
        if (template is not null && template.SectionGroupKeys.Any(key => string.Equals(key, camel, StringComparison.OrdinalIgnoreCase)))
        {
            return camel;
        }
        return camel;
    }
}
