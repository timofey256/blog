#r "nuget: FSharp.Formatting"

open System
open System.IO
open FSharp.Formatting.Markdown

let contentDir  = "content"
let outputDir   = "output"
let templateDir = "templates"

// Ensure output directory exists
Directory.CreateDirectory(outputDir) |> ignore

// Copy static files
let staticDir = Path.Combine(outputDir, "static")
Directory.CreateDirectory(staticDir) |> ignore
for file in Directory.GetFiles("static") do
    File.Copy(file, Path.Combine(staticDir, Path.GetFileName(file)), true)

// Load HTML templates
let layoutTemplate = File.ReadAllText(Path.Combine(templateDir, "layout.html"))

// Function to convert Markdown to HTML using FSharp.Formatting
let markdownToHtml (md: string) =
    let parsedDoc = Markdown.Parse(md)
    Markdown.ToHtml(parsedDoc)

// Process each Markdown file
for file in Directory.GetFiles(contentDir, "*.md") do
    let mdContent = File.ReadAllText(file)
    let htmlContent = markdownToHtml mdContent

    let outputFileName = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + ".html")
    let finalHtml = layoutTemplate.Replace("{{content}}", htmlContent)

    File.WriteAllText(outputFileName, finalHtml)
    printfn "Generated: %s" outputFileName

printfn "Blog generation complete. Files are in 'output/'"

