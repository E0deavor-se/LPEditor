using System.Net;
using System.Text.RegularExpressions;
using LPEditorApp.Models;
using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using System.Linq;

namespace LPEditorApp.Services;

public class NativePreviewService
{
    private static readonly Regex BreakTag = new(@"<\s*br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string BuildHtml(ContentModel content, LpDesignSpec? spec, LpDecorationSpec? decorationSpec, string? editingSectionKey = null)
    {
        var resolvedSpec = spec ?? new LpDesignSpec();
        var mapper = new AiDesignMapper();
        var mapping = mapper.Map(resolvedSpec);
        var resolvedDecoration = decorationSpec ?? new LpDecorationSpec();
        var decorMapper = new AiDecorationMapper();
        var decorMapping = decorMapper.Map(resolvedDecoration);
        var css = NativePreviewCss.Build(mapping, decorMapping);

        var hero = content.Sections.CampaignContent;
        var offer = content.Sections.CouponPeriod;
        var howto = content.Sections.CouponFlow;
        var notes = content.Sections.CouponNotes;

        var heroNotes = hero.Notes.Select(item => item.Text).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
        var howtoItems = howto.Items.Select(item => item.Text).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
        var notesLines = notes.TextLines.Select(item => item.Text).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
        if (notesLines.Count == 0)
        {
            notesLines = notes.Items.Select(item => item.Text).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
        }
        var footerLines = content.Campaign.FooterLines.Select(line => line.Text).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();

        var sections = new List<string>
        {
            BuildSectionHtml("hero", hero.Title, hero.Body, heroNotes, mapping, resolvedSpec, isHtmlBody: false),
            BuildOfferHtml(offer, mapping, resolvedSpec),
            BuildHowtoHtml(howto, howtoItems, mapping, resolvedSpec),
            BuildNotesHtml(notes.Title, notesLines, mapping, resolvedSpec),
            BuildFooterHtml(footerLines, mapping)
        };

                var containerClass = mapping.Classes.FirstOrDefault(name => name.StartsWith("ai-container-", StringComparison.OrdinalIgnoreCase));
                var bodyClasses = string.Join(" ", mapping.Classes.Concat(decorMapping.Classes));

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
<style>{css}</style>
</head>
<body class=""native-preview {bodyClasses}"">
  <div class=""native-container {(string.IsNullOrWhiteSpace(containerClass) ? string.Empty : containerClass)}"">
    {string.Join("\n", sections.Where(section => !string.IsNullOrWhiteSpace(section)))}
  </div>
</body>
</html>";
    }

        public string BuildReferenceHtml(ContentModel content, LpReferenceStyleSpec spec)
        {
                var mapper = new AiReferenceStyleMapper();
                var mapping = mapper.Map(spec);
                var css = BuildReferenceCss(mapping);

                var heroTitle = string.IsNullOrWhiteSpace(content.Sections.CampaignContent.Title)
                        ? (string.IsNullOrWhiteSpace(content.Meta.PageTitle) ? "キャンペーンタイトル" : content.Meta.PageTitle)
                        : content.Sections.CampaignContent.Title;
                var heroBody = string.IsNullOrWhiteSpace(content.Sections.CampaignContent.Body)
                        ? (string.IsNullOrWhiteSpace(content.Meta.Description) ? "キャンペーンの概要を簡潔に紹介します。" : content.Meta.Description)
                        : content.Sections.CampaignContent.Body;
                var conditions = content.Sections.CampaignContent.Notes
                        .Select(item => item.Text)
                        .Where(text => !string.IsNullOrWhiteSpace(text))
                        .Take(3)
                        .ToList();
                while (conditions.Count < 3)
                {
                        conditions.Add("条件を入力してください");
                }
                var ctaLabel = string.IsNullOrWhiteSpace(content.Sections.CouponFlow.ButtonLabel)
                        ? "今すぐ参加する"
                        : content.Sections.CouponFlow.ButtonLabel;

                var heroHtml = $@"
<section class=""ref-section ref-hero"" data-section-id=""hero"">
    <div class=""ref-hero__media"">
        <div class=""ref-hero__image""></div>
    </div>
    <div class=""ref-hero__content"">
        <h1 class=""ref-h1"">{WebUtility.HtmlEncode(heroTitle)}</h1>
        <p class=""ref-body"">{NormalizeText(heroBody)}</p>
        <ul class=""ref-conditions"">
            {string.Join(string.Empty, conditions.Select(item => $"<li>{WebUtility.HtmlEncode(item)}</li>"))}
        </ul>
        <div class=""ref-cta-wrap"">
            <a class=""ref-cta"" href=""#"">
                {(string.Equals(spec.DecorSpec.Badge, "none", StringComparison.OrdinalIgnoreCase) ? string.Empty : "<span class=\"ref-cta-badge\">NEW</span>")}
                <span>{WebUtility.HtmlEncode(ctaLabel)}</span>
            </a>
        </div>
    </div>
</section>";

                var offer = content.Sections.CouponPeriod;
                var offerHtml = BuildReferenceSectionHtml("offer", offer.Title, offer.Text);

                var howto = content.Sections.CouponFlow;
                var steps = howto.Items.Select(item => item.Text).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
                var howtoHtml = BuildReferenceStepsHtml(howto.Title, howto.Lead, steps);

                var notes = content.Sections.CouponNotes;
                var notesLines = notes.TextLines.Select(item => item.Text).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
                if (notesLines.Count == 0)
                {
                        notesLines = notes.Items.Select(item => item.Text).Where(text => !string.IsNullOrWhiteSpace(text)).ToList();
                }
                var notesHtml = BuildReferenceNotesHtml(notes.Title, notesLines, spec.LayoutRecipe.Notes);

                var ranking = content.Sections.Ranking;
                var rankingHtml = BuildReferenceRankingHtml(ranking);

                var bodyClasses = string.Join(" ", mapping.Classes);
                return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
<style>{css}</style>
</head>
<body class=""ref-preview {bodyClasses}"">
    <div class=""ref-container"">
        {heroHtml}
        {offerHtml}
        {howtoHtml}
        {rankingHtml}
        {notesHtml}
    </div>
</body>
</html>";
        }

    private static string BuildSectionHtml(string key, string? heading, string? body, List<string> bullets, AiDesignMapping mapping, LpDesignSpec spec, bool isHtmlBody)
    {
        if (string.IsNullOrWhiteSpace(heading) && string.IsNullOrWhiteSpace(body) && bullets.Count == 0)
        {
            return string.Empty;
        }

        var sectionClass = BuildSectionClass(mapping);
        var headingClass = BuildHeadingClass(mapping);
        var bodyHtml = string.IsNullOrWhiteSpace(body)
            ? string.Empty
            : WrapBody(isHtmlBody ? body : NormalizeText(body));
        var bulletsHtml = bullets.Count == 0
            ? string.Empty
            : $"<ul class=\"native-bullets\">{string.Join(string.Empty, bullets.Select(item => $"<li>{WebUtility.HtmlEncode(item)}</li>"))}</ul>";
        var headingHtml = string.IsNullOrWhiteSpace(heading)
            ? string.Empty
            : $"<div class=\"{headingClass}\">{WebUtility.HtmlEncode(heading)}</div>";

        return $@"<section class=""{sectionClass}"" data-section-id=""{key}"">{headingHtml}{bodyHtml}{bulletsHtml}</section>";
    }

    private static string BuildOfferHtml(CouponPeriodModel offer, AiDesignMapping mapping, LpDesignSpec spec)
    {
        if (string.IsNullOrWhiteSpace(offer.Title) && string.IsNullOrWhiteSpace(offer.Text))
        {
            return string.Empty;
        }

        var sectionClass = BuildSectionClass(mapping);
        var headingClass = BuildHeadingClass(mapping);
        var headingHtml = string.IsNullOrWhiteSpace(offer.Title) ? string.Empty : $"<div class=\"{headingClass}\">{WebUtility.HtmlEncode(offer.Title)}</div>";
        var bodyHtml = string.IsNullOrWhiteSpace(offer.Text) ? string.Empty : WrapBody(NormalizeText(offer.Text));
        return $@"<section class=""{sectionClass}"" data-section-id=""offer"">{headingHtml}{bodyHtml}</section>";
    }

    private static string BuildHowtoHtml(CouponFlowSectionModel howto, List<string> items, AiDesignMapping mapping, LpDesignSpec spec)
    {
        if (string.IsNullOrWhiteSpace(howto.Title) && items.Count == 0 && string.IsNullOrWhiteSpace(howto.Lead))
        {
            return string.Empty;
        }

        var sectionClass = BuildSectionClass(mapping);
        var headingClass = BuildHeadingClass(mapping);
        var headingHtml = string.IsNullOrWhiteSpace(howto.Title) ? string.Empty : $"<div class=\"{headingClass}\">{WebUtility.HtmlEncode(howto.Title)}</div>";
        var bodyHtml = string.IsNullOrWhiteSpace(howto.Lead) ? string.Empty : WrapBody(NormalizeText(howto.Lead));
        var stepsHtml = items.Count == 0
            ? string.Empty
            : $"<ol class=\"native-steps\">{string.Join(string.Empty, items.Select((item, index) => $"<li><span class=\"native-step-number\">{index + 1}</span><span>{WebUtility.HtmlEncode(item)}</span></li>"))}</ol>";

        var ctaStyle = mapping.Classes.FirstOrDefault(name => name.StartsWith("ai-cta-", StringComparison.OrdinalIgnoreCase)) ?? "ai-cta-solid";
        var ctaClass = ctaStyle.Replace("ai-cta-", "native-cta-");
        var ctaHtml = string.IsNullOrWhiteSpace(howto.ButtonLabel)
            ? string.Empty
            : $"<a class=\"native-cta {ctaClass}\" href=\"#\">{WebUtility.HtmlEncode(howto.ButtonLabel)}</a>";

        return $@"<section class=""{sectionClass}"" data-section-id=""howto"">{headingHtml}{bodyHtml}{stepsHtml}{ctaHtml}</section>";
    }

    private static string BuildNotesHtml(string? title, List<string> notes, AiDesignMapping mapping, LpDesignSpec spec)
    {
        if (notes.Count == 0)
        {
            return string.Empty;
        }

        var sectionClass = BuildSectionClass(mapping);
        var headingClass = BuildHeadingClass(mapping);
        var headingHtml = string.IsNullOrWhiteSpace(title) ? string.Empty : $"<div class=\"{headingClass}\">{WebUtility.HtmlEncode(title)}</div>";
        var listHtml = $"<ul class=\"native-bullets\">{string.Join(string.Empty, notes.Select(item => $"<li>{WebUtility.HtmlEncode(item)}</li>"))}</ul>";

        if (string.Equals(spec.Layout.NotesStyle, "accordion", StringComparison.OrdinalIgnoreCase))
        {
            listHtml = $"<details class=\"native-notes-details\"><summary>注意事項を見る</summary>{listHtml}</details>";
        }

        return $@"<section class=""{sectionClass}"" data-section-id=""notes"">{headingHtml}{listHtml}</section>";
    }

    private static string BuildReferenceSectionHtml(string key, string? heading, string? body)
    {
        if (string.IsNullOrWhiteSpace(heading) && string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        var headingHtml = string.IsNullOrWhiteSpace(heading) ? string.Empty : $"<h2 class=\"ref-h2\">{WebUtility.HtmlEncode(heading)}</h2>";
        var bodyHtml = string.IsNullOrWhiteSpace(body) ? string.Empty : $"<p class=\"ref-body\">{NormalizeText(body)}</p>";
        return $"<section class=\"ref-section\" data-section-id=\"{key}\">{headingHtml}{bodyHtml}</section>";
    }

    private static string BuildReferenceStepsHtml(string? heading, string? lead, List<string> steps)
    {
        if (string.IsNullOrWhiteSpace(heading) && string.IsNullOrWhiteSpace(lead) && steps.Count == 0)
        {
            return string.Empty;
        }

        var headingHtml = string.IsNullOrWhiteSpace(heading) ? string.Empty : $"<h2 class=\"ref-h2\">{WebUtility.HtmlEncode(heading)}</h2>";
        var leadHtml = string.IsNullOrWhiteSpace(lead) ? string.Empty : $"<p class=\"ref-body\">{NormalizeText(lead)}</p>";
        var listHtml = steps.Count == 0
            ? string.Empty
            : $"<ol class=\"ref-steps\">{string.Join(string.Empty, steps.Select((item, index) => $"<li><span class=\"ref-step\">{index + 1}</span><span>{WebUtility.HtmlEncode(item)}</span></li>"))}</ol>";
        return $"<section class=\"ref-section\" data-section-id=\"howto\">{headingHtml}{leadHtml}{listHtml}</section>";
    }

    private static string BuildReferenceNotesHtml(string? heading, List<string> notes, string notesStyle)
    {
        if (notes.Count == 0)
        {
            return string.Empty;
        }

        var headingHtml = string.IsNullOrWhiteSpace(heading) ? string.Empty : $"<h2 class=\"ref-h2\">{WebUtility.HtmlEncode(heading)}</h2>";
        var listHtml = $"<ul class=\"ref-notes\">{string.Join(string.Empty, notes.Select(item => $"<li>{WebUtility.HtmlEncode(item)}</li>"))}</ul>";
        if (string.Equals(notesStyle, "accordion", StringComparison.OrdinalIgnoreCase))
        {
            listHtml = $"<details class=\"ref-notes-details\"><summary>注意事項を見る</summary>{listHtml}</details>";
        }
        return $"<section class=\"ref-section\" data-section-id=\"notes\">{headingHtml}{listHtml}</section>";
    }

    private static string BuildReferenceRankingHtml(RankingSectionModel ranking)
    {
        if (!ranking.Enabled || ranking.Rows.Count == 0)
        {
            return string.Empty;
        }

        var headingHtml = string.IsNullOrWhiteSpace(ranking.Title) ? string.Empty : $"<h2 class=\"ref-h2\">{WebUtility.HtmlEncode(ranking.Title)}</h2>";
        var headerLabels = ranking.HeaderLabels?.ToList() ?? new List<string> { "順位", "決済金額", "品数" };
        var headerHtml = string.Join(string.Empty, headerLabels.Select(label => $"<th>{WebUtility.HtmlEncode(label)}</th>"));
        var rowsHtml = string.Join(string.Empty, ranking.Rows.Take(5).Select(row => $"<tr><td>{WebUtility.HtmlEncode(row.Rank)}</td><td>{WebUtility.HtmlEncode(row.Amount)}</td><td>{WebUtility.HtmlEncode(row.Items)}</td></tr>"));
        var tableHtml = $"<div class=\"ref-ranking\"><table><thead><tr>{headerHtml}</tr></thead><tbody>{rowsHtml}</tbody></table></div>";
        return $"<section class=\"ref-section\" data-section-id=\"ranking\">{headingHtml}{tableHtml}</section>";
    }

    private static string BuildFooterHtml(List<string> footerLines, AiDesignMapping mapping)
    {
        if (footerLines.Count == 0)
        {
            return string.Empty;
        }

        var sectionClass = BuildSectionClass(mapping);
        var headingClass = BuildHeadingClass(mapping);
        var headingHtml = "<div class=\"" + headingClass + "\">お問い合わせ</div>";
        var bodyHtml = $"<div class=\"native-footer\">{string.Join("<br />", footerLines.Select(line => WebUtility.HtmlEncode(line)))}</div>";
        return $@"<section class=""{sectionClass}"" data-section-id=""footer"">{headingHtml}{bodyHtml}</section>";
    }

    private static string BuildSectionClass(AiDesignMapping mapping)
    {
        var baseClass = "native-section";
        var sectionStyle = mapping.Classes.FirstOrDefault(name => name.StartsWith("ai-section-", StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(sectionStyle) ? baseClass : baseClass + " " + sectionStyle.Replace("ai-section-", "native-section-");
    }

    private static string BuildHeadingClass(AiDesignMapping mapping)
    {
        var baseClass = "native-heading";
        var headingStyle = mapping.Classes.FirstOrDefault(name => name.StartsWith("ai-heading-", StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(headingStyle) ? baseClass : baseClass + " " + headingStyle.Replace("ai-heading-", "native-heading-");
    }

    private static string WrapBody(string body)
    {
        return $"<div class=\"native-body\">{body}</div>";
    }

    private static string NormalizeText(string value)
    {
        var normalized = BreakTag.Replace(value, "\n");
        return WebUtility.HtmlEncode(normalized).Replace("\n", "<br />");
    }

    private static string BuildReferenceCss(AiReferenceStyleMapping mapping)
    {
        var vars = string.Join(";", mapping.Variables.Select(pair => $"{pair.Key}:{pair.Value}"));
        return $@"
:root{{{vars};}}
body.ref-preview{{margin:0;font-family:""Noto Sans JP"", ""Yu Gothic"", ""Hiragino Kaku Gothic ProN"", sans-serif;background:var(--ref-bg);color:var(--ref-text);}}
.ref-container{{max-width:960px;margin:0 auto;padding:24px;display:flex;flex-direction:column;gap:var(--ref-section-y);}}
.ref-section{{background:#fff;border:1px solid var(--ref-border);border-radius:var(--ref-radius-card);padding:var(--ref-card-padding);box-shadow:0 10px 24px rgba(15,23,42,0.08);}}
.ref-h1{{font-size:var(--ref-h1);margin:0 0 12px;font-weight:700;}}
.ref-h2{{font-size:var(--ref-h2);margin:0 0 12px;font-weight:700;}}
.ref-body{{font-size:var(--ref-body);line-height:1.7;margin:0 0 12px;color:var(--ref-text);}}
.ref-hero{{display:grid;gap:var(--ref-grid-gap);align-items:center;}}
.ref-hero__image{{width:100%;height:240px;background:linear-gradient(135deg, var(--ref-primary), var(--ref-accent));border-radius:var(--ref-radius-card);}}
.ref-conditions{{margin:0;padding:0;list-style:none;display:grid;gap:8px;}}
.ref-conditions li{{background:var(--ref-bg);border-radius:12px;padding:8px 12px;font-size:var(--ref-small);color:var(--ref-muted);}}
.ref-cta-wrap{{margin-top:8px;}}
.ref-cta{{display:inline-flex;gap:8px;align-items:center;text-decoration:none;background:var(--ref-primary);color:#fff;padding:12px 18px;border-radius:var(--ref-radius-button);font-weight:700;}}
.ref-cta-badge{{background:var(--ref-accent);color:#fff;padding:4px 8px;border-radius:var(--ref-radius-badge);font-size:12px;}}
.ref-steps{{list-style:none;padding:0;margin:0;display:grid;gap:10px;}}
.ref-steps li{{display:flex;gap:10px;align-items:flex-start;}}
.ref-step{{width:26px;height:26px;border-radius:999px;background:var(--ref-accent);color:#fff;display:flex;align-items:center;justify-content:center;font-size:12px;font-weight:700;}}
.ref-notes{{margin:0;padding-left:18px;}}
.ref-notes-details{{margin-top:8px;}}
.ref-ranking table{{width:100%;border-collapse:collapse;font-size:var(--ref-body);}}
.ref-ranking th,.ref-ranking td{{border:1px solid var(--ref-border);padding:10px;text-align:center;}}

body.ref-preview.ref-section-band .ref-section{{background:var(--ref-primary);color:#fff;border:none;}}
body.ref-preview.ref-section-flat .ref-section{{box-shadow:none;}}

body.ref-preview.ref-heading-band .ref-h2{{background:var(--ref-primary);color:#fff;display:inline-block;padding:6px 12px;border-radius:var(--ref-radius-badge);}}
body.ref-preview.ref-heading-pill .ref-h2{{background:var(--ref-accent);color:#fff;display:inline-block;padding:6px 12px;border-radius:999px;}}
body.ref-preview.ref-heading-underline .ref-h2{{border-bottom:3px solid var(--ref-accent);padding-bottom:6px;}}

body.ref-preview.ref-hero-kv-image-top .ref-hero{{grid-template-columns:1fr;}}
body.ref-preview.ref-hero-kv-split .ref-hero{{grid-template-columns:1.1fr 1fr;}}
body.ref-preview.ref-hero-kv-poster .ref-hero{{grid-template-columns:1fr;}}
body.ref-preview.ref-hero-kv-poster .ref-hero__image{{height:320px;}}

body.ref-preview.ref-bg-gradient{{background:linear-gradient(180deg, var(--ref-bg), #ffffff);}}

body.ref-preview.ref-divider-line .ref-section{{position:relative;}}
body.ref-preview.ref-divider-line .ref-section::after{{content:"";display:block;height:1px;background:var(--ref-border);margin-top:16px;opacity:0.6;}}
body.ref-preview.ref-divider-wave .ref-section::after{{content:"";display:block;height:12px;margin-top:16px;background:radial-gradient(circle at 8px -4px, var(--ref-border) 8px, transparent 9px) repeat-x;background-size:20px 12px;opacity:0.6;}}

body.ref-preview.ref-weight-regular .ref-h1, body.ref-preview.ref-weight-regular .ref-h2{{font-weight:600;}}
body.ref-preview.ref-weight-medium .ref-h1, body.ref-preview.ref-weight-medium .ref-h2{{font-weight:700;}}
body.ref-preview.ref-weight-bold .ref-h1, body.ref-preview.ref-weight-bold .ref-h2{{font-weight:800;}}

@media (max-width:960px){{.ref-container{{padding:18px;}}}}
@media (max-width:600px){{
  .ref-container{{padding:14px;}}
  .ref-section{{padding:16px;}}
  .ref-hero__image{{height:200px;}}
  body.ref-preview.ref-hero-kv-split .ref-hero{{grid-template-columns:1fr;}}
}}
";
    }
}
