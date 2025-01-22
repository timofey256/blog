// StaticSiteGenerator.fsx
#r "nuget: Markdig"
#r "nuget: FSharp.Data"

open System
open System.IO
open Markdig
open FSharp.Data

type Page = {
    Title: string
    Content: string
    Path: string
    Date: DateTime option
}

// Path utilities
let relativePath (currentPath: string) =
    let depth = currentPath.Split('/').Length - 1
    String.replicate depth "../"

// Templates
let baseTemplate (content: string) (title: string) (currentPath: string) =
    let root = relativePath currentPath
    $"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>{title}</title>
        <link rel="stylesheet" href="{root}css/style.css">
    </head>
    <body>
        <div class="container">
            <nav class="sidebar">
                <h1>Your Name</h1>
                <ul>
                    <li><a href="{root}about.html">About</a></li>
                    <li><a href="{root}notes/index.html">Notes</a></li>
                    <li><a href="{root}articles/index.html">Articles</a></li>
                    <li><a href="{root}ideas.html">Ideas</a></li>
                </ul>
            </nav>
            <main class="content">
                {content}
            </main>
        </div>
        <script src="{root}js/main.js"></script>
    </body>
    </html>
    """

// Collection index template
let createCollectionIndex (items: Page list) (title: string) =
    let itemsHtml = 
        items 
        |> List.sortByDescending (fun p -> p.Date)
        |> List.map (fun page ->
            let dateStr = 
                match page.Date with
                | Some d -> d.ToString("yyyy-MM-dd")
                | None -> ""
            $"""
            <div class="post-item">
                <h2><a href="{Path.GetFileName(page.Path)}">{page.Title}</a></h2>
                {(if dateStr <> "" then $"<time>{dateStr}</time>" else "")}
            </div>
            """)
        |> String.concat "\n"
    
    $"""
    <div class="collection">
        <h1>{title}</h1>
        <div class="post-list">
            {itemsHtml}
        </div>
    </div>
    """

// Markdown processing
let processMarkdown (content: string) =
    let pipeline = MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build()
    Markdown.ToHtml(content, pipeline)

// Extract date from markdown frontmatter (if exists)
let extractDate (content: string) =
    try
        if content.StartsWith("---") then
            let endIdx = content.IndexOf("---", 3)
            if endIdx > 0 then
                let frontMatter = content.Substring(3, endIdx - 3)
                let dateMatch = System.Text.RegularExpressions.Regex.Match(frontMatter, @"date:\s*(\d{4}-\d{2}-\d{2})")
                if dateMatch.Success then
                    Some(DateTime.Parse(dateMatch.Groups.[1].Value))
                else None
            else None
        else None
    with _ -> None

// File operations for collection pages
let readMarkdownFiles dir =
    if Directory.Exists(dir) then
        Directory.GetFiles(dir, "*.md")
        |> Array.map (fun path ->
            let content = File.ReadAllText(path)
            let filename = Path.GetFileNameWithoutExtension(path)
            {
                Title = filename
                Content = processMarkdown content
                Path = Path.Combine(Path.GetFileName(dir), filename + ".html")
                Date = extractDate content
            })
        |> Array.toList
    else
        []

// Process single markdown file
let processSinglePage (inputPath: string) (outputDir: string) (outputFilename: string) =
    if File.Exists(inputPath) then
        let content = File.ReadAllText(inputPath)
        let processed = processMarkdown content
        let relativePath = outputFilename
        let html = baseTemplate processed (Path.GetFileNameWithoutExtension(inputPath)) relativePath
        Directory.CreateDirectory(outputDir) |> ignore
        File.WriteAllText(Path.Combine(outputDir, outputFilename), html)

// Generate site
let generateSite () =
    let collectionDirs = ["notes"; "articles"]
    let publicDir = "public"
    
    // Create output directories
    Directory.CreateDirectory(publicDir) |> ignore
    for dir in collectionDirs do
        Directory.CreateDirectory(Path.Combine(publicDir, dir)) |> ignore
    
    // Process collection directories
    for dir in collectionDirs do
        let inputDir = Path.Combine("content", dir)
        let outputDir = Path.Combine(publicDir, dir)
        let pages = readMarkdownFiles inputDir
        
        // Generate index for the collection
        let indexContent = createCollectionIndex pages (dir.Substring(0, 1).ToUpper() + dir.Substring(1))
        let indexHtml = baseTemplate indexContent dir $"{dir}/index.html"
        File.WriteAllText(Path.Combine(publicDir, dir, "index.html"), indexHtml)
        
        // Generate individual pages
        pages |> List.iter (fun page ->
            let html = baseTemplate page.Content page.Title page.Path
            File.WriteAllText(
                Path.Combine(publicDir, page.Path),
                html))
    
    // Process single pages
    processSinglePage 
        (Path.Combine("content", "ideas.md")) 
        publicDir 
        "ideas.html"
        
    processSinglePage 
        (Path.Combine("content", "about.md")) 
        publicDir 
        "about.html"
        
    // Generate index page
    let indexContent = 
        if File.Exists(Path.Combine("content", "about.md")) then
            File.ReadAllText(Path.Combine("content", "about.md"))
        else
            "# Welcome\n\nWelcome to my personal website."
    let processedIndex = processMarkdown indexContent
    let indexHtml = baseTemplate processedIndex "About" "about.html"
    File.WriteAllText(
        Path.Combine(publicDir, "about.html"),
        indexHtml)

// Run generator
generateSite()
