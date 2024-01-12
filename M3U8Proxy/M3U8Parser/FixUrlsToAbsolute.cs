﻿using System.Text.RegularExpressions;

namespace M3U8Proxy.M3U8Parser;

public partial class M3U8Paser
{
    [GeneratedRegex(@"\?.+", RegexOptions.Compiled)]
    private static partial Regex GetParamsRegex();

    public static string FixAllUrls(string[] lines, string url, string prefix, string suffix,bool encrypted,bool isPlaylist)
    {
        var parameters = GetParamsRegex().Match(url).Value;
        var uri = new Uri(url);
        const string uriPattern = @"URI=""([^""]+)""";
        if (encrypted&&!isPlaylist)
        {
            lines = InsertIntro(lines);
        }
        for (var i = 0; i < lines.Length; i++)
        {
            var isUri = lines[i].Contains("URI");
            if (!isUri && (lines[i].StartsWith("#") || string.IsNullOrWhiteSpace(lines[i]))) continue;
            var uriContent = isUri ? Regex.Match(lines[i], uriPattern).Groups[1].Value : lines[i];
            if (!Uri.TryCreate(uriContent, UriKind.RelativeOrAbsolute, out var uriExtracted)) continue;
            var newUri = !uriExtracted.IsAbsoluteUri ? new Uri(uri, uriExtracted) : uriExtracted;
            var substitutedUri = $"{prefix}{EncodeUrl(newUri + parameters,encrypted)}{suffix}";
            var test = Regex.Replace(lines[i], uriPattern, m => $"URI=\"{substitutedUri}\"");
            lines[i] = isUri ? test : substitutedUri;
        }

        return string.Join(Environment.NewLine, lines);
    }
    static string EncodeUrl(string url,bool encrypted)
    {
        return Uri.EscapeDataString(encrypted ? AES.Encrypt(url) : url);
    }
    private static string[] InsertIntro(string[] lines)
    {
        var lastIndex = 0;
        for (var i =0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("#")) continue;
            lastIndex = i-1;
            break;

        }
        var lastLineText = lines[lastIndex];
        var testToInsert = new [] {
            "#EXTINF:6.266667,",
            "http://localhost:5196/video/intro.ts",
            "#EXT-X-DISCONTINUITY",
            lastLineText
        };
        
        lines[lastIndex] = string.Join(Environment.NewLine, testToInsert);
        return lines;
    }
}