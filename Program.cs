using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

foreach (var file in Directory.GetFiles("Markdowns", "*.md"))
{
    var p = ParseMardownWithFrontMatter(file);
    Console.WriteLine($"File: {file}");
    Console.WriteLine($" * Title = {p.Title}");
    Console.WriteLine($" * Date = {p.Date:yyyy-MM-dd}");
    Console.WriteLine($" * Html = {p.Html}");
}

PostEntry ParseMardownWithFrontMatter(string path)
{
    // 建立能識別 YAML 的 Pipeline
    var pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter().Build();
    // 建立 TextWriter 及 HtmlRender
    using var sw = new StringWriter();
    var render = new HtmlRenderer(sw);
    pipeline.Setup(render);
    // 預設由檔案資訊決定標題跟時間
    var postEntry = new PostEntry
    {
        Title = path,
        Date = new FileInfo(path).LastWriteTime
    };
    try
    {
        var markdown = File.ReadAllText(path);
        // 套用先前建立的 Pipeline 
        var doc = Markdown.Parse(markdown, pipeline);
        // 取出 Markdown 第一個 YAML
        var yamlBlock = doc.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        // 若有包含 YAML
        if (yamlBlock != null)
        {
            // 依 YAML 在 Markdown 內容的起始位置及長度取出 YAML 字串
            var yaml = markdown.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
            using (var input = new StringReader(yaml))
            {
                // 使用 YAML Parser 開始解析
                var yamlParser = new Parser(input);
                yamlParser.Consume<StreamStart>();
                yamlParser.Consume<DocumentStart>();
                // 建立 YAML 反序列化工具
                var yamlDes = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                // 解析 YAML 內容，對映到資料型別的屬性上
                var frontMatter = yamlDes.Deserialize<PostEntry>(yamlParser);
                yamlParser.Consume<DocumentEnd>();
                postEntry.Title = frontMatter.Title;
                postEntry.Date = frontMatter.Date;
            }
        }
        // 使用 HtmlRenderer 產生 HTML 內容
        render.Render(doc);
        sw.Flush();
        postEntry.Html = sw.ToString();
    }
    catch (Exception ex) {
        postEntry.Html = ex.ToString();
    }
    return postEntry;
}

public class PostEntry
{
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Html { get; set; } = string.Empty;
}