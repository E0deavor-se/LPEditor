using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using LPEditorApp.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace LPEditorApp.Tests;

public class AiGenerateLpServiceTests
{
    [Fact]
    public async Task GenerateBlueprintAsync_RetriesWithErrorsInPrompt()
    {
        var messages = new List<List<OpenAiMessage>>();
        var client = new FakeChatClient(messages, new[]
        {
            InvalidJsonMissingSections,
            ValidJson
        });

        var options = Options.Create(new AiOptions
        {
            ApiKey = "test",
            MaxRetryCount = 1,
            StrictJsonOnly = true
        });
        var validator = new AiLpBlueprintValidator(options);
        var logger = new ConsoleLogger();
        var service = new AiGenerateLpService(client, validator, options, logger);

        var outcome = await service.GenerateBlueprintAsync(new AiGenerateLpRequest { Industry = "小売" }, CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(2, messages.Count);
        var secondPrompt = messages[1].FirstOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        Assert.Contains("section 'hero' is required", secondPrompt);
    }

    private const string InvalidJsonMissingSections = """
    {
      "meta": {
        "language": "ja",
        "title": "テスト",
        "tone": "casual",
        "goal": "acquisition",
        "industry": "小売",
        "brand": { "name": "テスト", "colorHint": null }
      },
      "sections": []
    }
    """;

    private const string ValidJson = """
    {
      "meta": {
        "language": "ja",
        "title": "テストLP",
        "tone": "casual",
        "goal": "acquisition",
        "industry": "小売",
        "brand": { "name": "テスト", "colorHint": null }
      },
      "sections": [
        {
          "type": "hero",
          "id": "hero-1",
          "props": {
            "heading": "見出し",
            "subheading": "サブ",
            "body": "本文",
            "bullets": ["箇条書き1", "箇条書き2", "箇条書き3"],
            "ctaText": "詳しく見る",
            "disclaimer": null,
            "items": []
          }
        },
        {
          "type": "offer",
          "id": "offer-1",
          "props": {
            "heading": "オファー",
            "subheading": null,
            "body": null,
            "bullets": ["要点1", "要点2", "要点3"],
            "ctaText": null,
            "disclaimer": null,
            "items": [
              { "title": "特典内容", "text": "内容", "badge": null },
              { "title": "利用条件", "text": "条件", "badge": null },
              { "title": "期間", "text": "期間", "badge": null }
            ]
          }
        },
        {
          "type": "howto",
          "id": "howto-1",
          "props": {
            "heading": "手順",
            "subheading": "流れ",
            "body": null,
            "bullets": ["手順1", "手順2", "手順3"],
            "ctaText": "申し込む",
            "disclaimer": null,
            "items": []
          }
        },
        {
          "type": "notes",
          "id": "notes-1",
          "props": {
            "heading": "注意事項",
            "subheading": null,
            "body": null,
            "bullets": ["注意1", "注意2", "注意3", "注意4", "注意5"],
            "ctaText": null,
            "disclaimer": null,
            "items": []
          }
        },
        {
          "type": "footer",
          "id": "footer-1",
          "props": {
            "heading": null,
            "subheading": null,
            "body": "お問い合わせは窓口まで",
            "bullets": ["会社情報は公式サイトでご確認ください。", "受付時間は変更となる場合があります。", "最新情報をご確認ください。"],
            "ctaText": null,
            "disclaimer": "※記載内容は予告なく変更となる場合があります。",
            "items": []
          }
        }
      ]
    }
    """;

    private sealed class FakeChatClient : IAiChatClient
    {
        private readonly List<List<OpenAiMessage>> _messages;
        private readonly Queue<string> _responses;

        public FakeChatClient(List<List<OpenAiMessage>> messages, IEnumerable<string> responses)
        {
            _messages = messages;
            _responses = new Queue<string>(responses);
        }

        public Task<string> CreateChatCompletionAsync(string model, List<OpenAiMessage> messages, bool strictJsonOnly, CancellationToken cancellationToken)
        {
            _messages.Add(messages.Select(m => new OpenAiMessage { Role = m.Role, Content = m.Content }).ToList());
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
