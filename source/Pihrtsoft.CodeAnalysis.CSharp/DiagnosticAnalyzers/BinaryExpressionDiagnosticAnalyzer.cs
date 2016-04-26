﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Pihrtsoft.CodeAnalysis;

namespace Pihrtsoft.CodeAnalysis.CSharp.DiagnosticAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BinaryExpressionDiagnosticAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DiagnosticDescriptors.FormatBinaryOperatorOnNextLine);

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.RegisterSyntaxNodeAction(f => AnalyzeBinaryExpression(f),
                SyntaxKind.AddExpression,
                SyntaxKind.SubtractExpression,
                SyntaxKind.MultiplyExpression,
                SyntaxKind.DivideExpression,
                SyntaxKind.ModuloExpression,
                SyntaxKind.LeftShiftExpression,
                SyntaxKind.RightShiftExpression,
                SyntaxKind.LogicalOrExpression,
                SyntaxKind.LogicalAndExpression,
                SyntaxKind.BitwiseOrExpression,
                SyntaxKind.BitwiseAndExpression,
                SyntaxKind.ExclusiveOrExpression,
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression,
                SyntaxKind.LessThanExpression,
                SyntaxKind.LessThanOrEqualExpression,
                SyntaxKind.GreaterThanExpression,
                SyntaxKind.GreaterThanOrEqualExpression,
                SyntaxKind.IsExpression,
                SyntaxKind.AsExpression);
        }

        private void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
        {
            if (GeneratedCodeAnalyzer?.IsGeneratedCode(context) == true)
                return;

            var binaryExpression = (BinaryExpressionSyntax)context.Node;

            if (binaryExpression.Left != null
                && binaryExpression.Right != null
                && !IsStringConcatenation(context, binaryExpression)
                && binaryExpression.Left.GetTrailingTrivia().All(f => f.IsKind(SyntaxKind.WhitespaceTrivia))
                && CheckOperatorTrailingTrivia(binaryExpression.OperatorToken.TrailingTrivia)
                && binaryExpression.Right.GetLeadingTrivia().All(f => f.IsWhitespaceOrEndOfLine()))
            {
                context.ReportDiagnostic(
                    DiagnosticDescriptors.FormatBinaryOperatorOnNextLine,
                    binaryExpression.OperatorToken.GetLocation());
            }
        }

        private static bool IsStringConcatenation(SyntaxNodeAnalysisContext context, BinaryExpressionSyntax binaryExpression)
        {
            return binaryExpression.IsKind(SyntaxKind.AddExpression)
                && (IsStringExpression(context, binaryExpression.Left) || IsStringExpression(context, binaryExpression.Right));
        }

        private static bool IsStringExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            if (expression.IsKind(SyntaxKind.StringLiteralExpression)
                || expression.IsKind(SyntaxKind.InterpolatedStringExpression))
            {
                return true;
            }

            ITypeSymbol typeSymbol = context.SemanticModel
                .GetTypeInfo(expression, context.CancellationToken)
                .ConvertedType;

            return typeSymbol.Kind == SymbolKind.NamedType
                && ((INamedTypeSymbol)typeSymbol).SpecialType == SpecialType.System_String;
        }

        private static bool CheckOperatorTrailingTrivia(SyntaxTriviaList triviaList)
        {
            bool result = false;

            foreach (SyntaxTrivia trivia in triviaList)
            {
                switch (trivia.Kind())
                {
                    case SyntaxKind.WhitespaceTrivia:
                        {
                            continue;
                        }
                    case SyntaxKind.EndOfLineTrivia:
                        {
                            result = true;
                            continue;
                        }
                    default:
                        {
                            return false;
                        }
                }
            }

            return result;
        }
    }
}
