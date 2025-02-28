﻿namespace Gu.Roslyn.Asserts.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [DebuggerDisplay("{Expression}")]
    internal readonly struct StringArgument : IEquatable<StringArgument>
    {
        internal readonly ExpressionSyntax Expression;
        internal readonly ISymbol? Symbol;
        internal readonly SyntaxToken SymbolIdentifier;
        internal readonly ExpressionSyntax? Value;
        internal readonly LiteralExpressionSyntax? StringLiteral;

        private StringArgument(ExpressionSyntax expression, ISymbol? symbol, SyntaxToken symbolIdentifier, ExpressionSyntax? value)
        {
            this.Expression = expression;
            this.Symbol = symbol;
            this.SymbolIdentifier = symbolIdentifier;
            this.Value = value;
            this.StringLiteral = value switch
            {
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal,
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: LiteralExpressionSyntax literal } }
                    when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal,
                _ => null,
            };
        }

        internal bool? HasPosition => this.Value switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token.ValueText.Contains("↓"),
            InvocationExpressionSyntax { Expression: LiteralExpressionSyntax { Token.ValueText: { } valueText } } => valueText.Contains("↓"),
            _ => null,
        };

        public static bool operator ==(StringArgument left, StringArgument right) => left.Equals(right);

        public static bool operator !=(StringArgument left, StringArgument right) => !left.Equals(right);

        public bool Equals(StringArgument other) => this.Expression.Equals(other.Expression);

        public override bool Equals(object? obj) => obj is StringArgument other && this.Equals(other);

        public override int GetHashCode() => this.Expression.GetHashCode();

        internal static bool TrySingle(InvocationExpressionSyntax invocation, IParameterSymbol parameter, SemanticModel semanticModel, CancellationToken cancellationToken, out StringArgument result)
        {
            if (invocation.TryFindArgument(parameter, out var argument))
            {
                switch (argument.Expression.Kind())
                {
                    case SyntaxKind.ImplicitArrayCreationExpression:
                    case SyntaxKind.ArrayCreationExpression:
                    case SyntaxKind.ObjectCreationExpression:
                        break;
                    default:
                        result = Create(argument.Expression, semanticModel, cancellationToken);
                        return true;
                }
            }

            result = default;
            return false;
        }

        internal static bool TryMany(InvocationExpressionSyntax invocation, IParameterSymbol parameter, SemanticModel semanticModel, CancellationToken cancellationToken, out ImmutableArray<StringArgument> results)
        {
            if (TryGetCollectionInitializer() is { } initializer)
            {
                var builder = ImmutableArray.CreateBuilder<StringArgument>(initializer!.Expressions.Count);
                foreach (var expression in initializer.Expressions)
                {
                    builder.Add(Create(expression, semanticModel, cancellationToken));
                }

                results = builder.MoveToImmutable();
                return true;
            }

            if (parameter.IsParams &&
                invocation.ArgumentList is { Arguments: { } arguments })
            {
                var builder = ImmutableArray.CreateBuilder<StringArgument>(arguments.Count - parameter.Ordinal);
                for (var i = parameter.Ordinal; i < arguments.Count; i++)
                {
                    builder.Add(Create(arguments[i].Expression, semanticModel, cancellationToken));
                }

                results = builder.MoveToImmutable();
                return true;
            }

            results = default;
            return false;

            InitializerExpressionSyntax? TryGetCollectionInitializer()
            {
                if (invocation.TryFindArgument(parameter, out var argument))
                {
                    switch (argument.Expression)
                    {
                        case ImplicitArrayCreationExpressionSyntax { Initializer: { } i }:
                            return i;
                        case ArrayCreationExpressionSyntax { Initializer: { } i }:
                            return i;
                        case ObjectCreationExpressionSyntax { Initializer: { } i }:
                            return i;
                    }
                }

                return null;
            }
        }

        internal static StringArgument Create(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (expression is IdentifierNameSyntax candidate &&
                semanticModel.TryGetSymbol(candidate, cancellationToken, out var candidateSymbol))
            {
                _ = TryGetValue(out var symbolIdentifier, out var value);
                return new StringArgument(expression, candidateSymbol, symbolIdentifier, value);
            }

            if (expression.Kind() == SyntaxKind.StringLiteralExpression)
            {
                return new StringArgument(expression, null, default, expression);
            }

            return new StringArgument(expression, null, default, null);

            bool TryGetValue(out SyntaxToken identifier, out ExpressionSyntax? result)
            {
                if (candidateSymbol!.TrySingleDeclaration(cancellationToken, out LocalDeclarationStatementSyntax? localDeclaration) &&
                    localDeclaration.Declaration is { Variables: { Count: 1 } localVariables } &&
                    localVariables.TrySingle(out var localVariable) &&
                    localVariable.Initializer is { } localInitializer)
                {
                    identifier = localVariable.Identifier;
                    result = localInitializer.Value;
                    return true;
                }

                if (candidateSymbol!.TrySingleDeclaration(cancellationToken, out FieldDeclarationSyntax? fieldDeclaration) &&
                    fieldDeclaration.Declaration is { Variables: { Count: 1 } fieldVariables } &&
                    fieldVariables.TrySingle(out var fieldVariable) &&
                    fieldVariable.Initializer is { } fieldInitializer)
                {
                    identifier = fieldVariable.Identifier;
                    result = fieldInitializer.Value;
                    return true;
                }

                identifier = default;
                result = null;
                return false;
            }
        }

        internal bool TryGetNameFromCode([NotNullWhen(true)] out string? codeName)
        {
            codeName = null;
            return this.StringLiteral is { Token.ValueText: { } valueText } &&
                   (TryGetName(valueText, "class ", out codeName) ||
                    TryGetName(valueText, "struct ", out codeName) ||
                    TryGetName(valueText, "interface ", out codeName) ||
                    TryGetName(valueText, "enum ", out codeName));

            static bool TryGetName(string text, string prefix, out string? name)
            {
                var index = text.IndexOf(prefix, StringComparison.Ordinal);
                while (index > 0 &&
                       text.LastIndexOf('/', index) > text.LastIndexOf('\n', index))
                {
                    index = text.IndexOf(prefix, index + 1, StringComparison.Ordinal);
                }

                if (index >= 0 &&
                    text.LastIndexOf("partial", index, StringComparison.Ordinal) < 0)
                {
                    var start = index + prefix.Length;
                    var end = text.IndexOfAny(new[] { ':', '{', '\r', '\n' }, start);
                    if (end > start)
                    {
                        name = text.Substring(start, end - start).Replace("<", "Of")
                                                                 .Replace(">", string.Empty)
                                                                 .Replace(">", string.Empty)
                                                                 .Replace(",", string.Empty)
                                                                 .Replace(" ", string.Empty);
                        return SyntaxFacts.IsValidIdentifier(name);
                    }
                }

                name = null;
                return false;
            }
        }
    }
}
