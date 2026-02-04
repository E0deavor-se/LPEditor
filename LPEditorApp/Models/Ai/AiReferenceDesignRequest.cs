using System.ComponentModel.DataAnnotations;

namespace LPEditorApp.Models.Ai;

public class AiReferenceDesignRequest
{
    [MaxLength(300)]
    public string ReferenceUrl { get; set; } = string.Empty;

    [MaxLength(30)]
    public string CampaignType { get; set; } = "ranking";

    [MaxLength(30)]
    public string BrandColorHint { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Tone { get; set; } = "clean";
}
