using LPEditorApp.Services.Ai;
using System.Linq;

namespace LPEditorApp.Services;

public static class NativePreviewCss
{
    public static string Build(AiDesignMapping mapping, AiDecorationMapping decoration)
    {
        var vars = string.Join(";", mapping.Variables.Select(pair => $"{pair.Key}:{pair.Value}"));
        var decorVars = string.Join(";", decoration.Variables.Select(pair => $"{pair.Key}:{pair.Value}"));
        return $@"
:root{{{vars};{decorVars};}}
body.native-preview{{background:var(--ai-bg);color:var(--ai-text);font-family:var(--ai-font, system);margin:0;padding:24px;position:relative;}}
body.native-preview::before{{content:"";position:fixed;inset:0;opacity:var(--ai-decor-bg-opacity,0);pointer-events:none;z-index:-1;}}
body.native-preview.ai-decor-bg-solid::before{{background:var(--ai-decor-bg-1);}}
body.native-preview.ai-decor-bg-gradient::before{{background:linear-gradient(135deg,var(--ai-decor-bg-1),var(--ai-decor-bg-2));}}
body.native-preview.ai-decor-bg-pattern::before{{background:var(--ai-decor-bg-1);}}
body.native-preview.ai-decor-pattern-dots::before{{background-image:radial-gradient(rgba(15,23,42,0.12) 1px, transparent 1px);background-size:14px 14px;}}
body.native-preview.ai-decor-pattern-waves::before{{background-image:linear-gradient(135deg, rgba(15,23,42,0.08) 25%, transparent 25%, transparent 50%, rgba(15,23,42,0.08) 50%, rgba(15,23,42,0.08) 75%, transparent 75%, transparent);background-size:32px 32px;}}

.native-container{{max-width:1100px;margin:0 auto;display:flex;flex-direction:column;gap:24px;}}
.native-container.native-container-wide{{max-width:1280px;}}
.native-section{{background:#fff;border-radius:var(--ai-radius);padding:20px;box-shadow:0 8px 20px rgba(15,23,42,0.08);position:relative;}}
.native-section.native-section-band{{background:var(--ai-primary);color:#fff;}}
.native-section.native-section-flat{{box-shadow:none;border:1px solid rgba(15,23,42,0.08);}}
.native-heading{{font-size:1.4rem;font-weight:700;margin-bottom:8px;}}
.native-heading.native-heading-pill{{display:inline-block;background:var(--ai-primary);color:#fff;padding:4px 12px;border-radius:999px;}}
.native-heading.native-heading-underline{{border-bottom:2px solid var(--ai-primary);padding-bottom:6px;}}
.native-bullets{{margin:12px 0 0 18px;}}
.native-steps{{margin:12px 0 0 0;list-style:none;padding:0;display:grid;gap:8px;}}
.native-steps li{{display:flex;gap:10px;align-items:flex-start;}}
.native-step-number{{width:28px;height:28px;border-radius:999px;background:var(--ai-primary);color:#fff;display:flex;align-items:center;justify-content:center;font-weight:700;font-size:0.85rem;flex-shrink:0;}}
.native-notes-details{{margin-top:8px;}}
.native-footer{{font-size:0.9rem;color:rgba(15,23,42,0.7);}}
.native-cta{{display:inline-block;margin-top:12px;padding:10px 16px;border-radius:999px;background:var(--ai-primary);color:#fff;text-decoration:none;font-weight:600;}}
.native-cta.native-cta-outline{{background:transparent;border:2px solid var(--ai-primary);color:var(--ai-primary);}}
.native-cta.native-cta-gradient{{background:linear-gradient(135deg,var(--ai-primary),var(--ai-accent));color:#fff;}}

body.native-preview.ai-decor-frame-flat .native-section{{background:#fff;box-shadow:none;border:1px solid rgba(15,23,42,0.08);}}
body.native-preview.ai-decor-frame-band .native-section{{background:var(--ai-primary);color:#fff;box-shadow:none;}}
body.native-preview.ai-decor-shadow-none .native-section{{box-shadow:none;}}
body.native-preview.ai-decor-shadow-medium .native-section{{box-shadow:0 10px 26px rgba(15,23,42,0.16);}}
body.native-preview.ai-decor-border-light .native-section{{border:1px solid rgba(15,23,42,0.12);}}
body.native-preview.ai-decor-border-none .native-section{{border:none;}}
body.native-preview.ai-decor-frame-card .native-section{{border-radius:var(--ai-decor-frame-radius,16px);}}

body.native-preview.ai-decor-heading-accent-line .native-heading{{border-left:var(--ai-decor-heading-thickness,3px) solid var(--ai-decor-heading-color);padding-left:10px;}}
body.native-preview.ai-decor-heading-pill .native-heading{{background:var(--ai-decor-heading-color);color:#fff;border-radius:999px;padding:4px 14px;display:inline-block;}}
body.native-preview.ai-decor-heading-label .native-heading{{display:inline-block;border:1px solid var(--ai-decor-heading-color);color:var(--ai-decor-heading-color);border-radius:6px;padding:2px 10px;font-weight:600;}}

body.native-preview.ai-decor-cta-badge .native-cta{{background:var(--ai-decor-cta-color);box-shadow:0 6px 16px rgba(0,0,0,0.12);}}
body.native-preview.ai-decor-cta-glow .native-cta{{background:var(--ai-decor-cta-color);box-shadow:0 0 0 6px rgba(0,0,0,0.08), 0 10px 24px rgba(0,0,0,0.18);}}

body.native-preview.ai-decor-divider-wave .native-section::after{{content:"";display:block;height:var(--ai-decor-divider-height,0);margin-top:14px;background:radial-gradient(circle at 10px -6px, var(--ai-decor-divider-color) 12px, transparent 13px) repeat-x;background-size:24px 18px;opacity:0.7;}}
body.native-preview.ai-decor-divider-zigzag .native-section::after{{content:"";display:block;height:var(--ai-decor-divider-height,0);margin-top:14px;background:repeating-linear-gradient(135deg, var(--ai-decor-divider-color) 0 6px, transparent 6px 12px);opacity:0.7;}}

@media (max-width:768px){{body.native-preview{{padding:16px;}}.native-section{{padding:16px;}}}}
";
    }
}
