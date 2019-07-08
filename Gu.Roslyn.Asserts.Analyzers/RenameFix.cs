namespace Gu.Roslyn.Asserts.Analyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Globalization;
    using System.Threading.Tasks;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameFix))]
[Shared]
public class RenameFix : DocumentEditorCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("CS0103", "CS0234", "CS1739");

    protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            if (TryFindIdentifier(out IdentifierNameSyntax identifierName, out var newText))
            {
                context.RegisterCodeFix(
                    $"Change to: {newText}.",
                    (editor, _) => editor.ReplaceNode(
                        identifierName,
                        x => x.WithIdentifier(SyntaxFactory.Identifier(newText).WithTriviaFrom(x.Identifier))),
                    nameof(RenameFix),
                    diagnostic);
            }

            bool TryFindIdentifier(out IdentifierNameSyntax result, out string text)
            {
                const string AnalyzerAssert = "AnalyzerAssert";
                switch (diagnostic.Id)
                {
                    case "CS0103" when syntaxRoot.TryFindNode(diagnostic, out result):
                        text = "RoslynAssert";
                        return result.Identifier.ValueText == AnalyzerAssert;
                    case "CS0234" when syntaxRoot.TryFindNode(diagnostic, out MemberAccessExpressionSyntax memberAccess):
                        {
                            while (memberAccess.Parent is MemberAccessExpressionSyntax parent)
                            {
                                memberAccess = parent;
                            }

                            result = (memberAccess.Expression as MemberAccessExpressionSyntax)?.Name as IdentifierNameSyntax;
                            text = "RoslynAssert";
                            return result?.Identifier.ValueText == AnalyzerAssert;
                        }

                    case "CS1739" when syntaxRoot.TryFindNode(diagnostic, out result):
                        string message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
                        switch (message)
                        {
                            case "The best overload for 'CodeFix' does not have a parameter named 'code'":
                            case "The best overload for 'CodeFix' does not have a parameter named 'codeWithErrorsIndicated'":
                                text = "before";
                                return true;
                            case "The best overload for 'CodeFix' does not have a parameter named 'fixedCode'":
                            case "The best overload for 'CodeFix' does not have a parameter named 'fixedcode'":
                                text = "after";
                                return true;
                        }

                        break;
                }

                text = null;
                result = null;
                return false;
            }
        }
    }
}
}
