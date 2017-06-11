﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    public static class CallExtensionMethodAsInstanceMethodRefactoring
    {
        public static AnalysisResult Analyze(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            ArgumentListSyntax argumentList = invocationExpression.ArgumentList;

            if (argumentList != null)
            {
                SeparatedSyntaxList<ArgumentSyntax> arguments = argumentList.Arguments;

                if (arguments.Any())
                {
                    if (arguments.Count > 1
                        || !(arguments[0].Expression is LiteralExpressionSyntax))
                    {
                        ExtensionMethodInfo extensionMethodInfo = semanticModel.GetExtensionMethodInfo(invocationExpression, ExtensionMethodKind.Ordinary, cancellationToken);

                        if (extensionMethodInfo.IsValid)
                        {
                            InvocationExpressionSyntax newInvocationExpression = GetNewInvocation(invocationExpression);

                            if (semanticModel
                                .GetSpeculativeMethodSymbol(invocationExpression.SpanStart, newInvocationExpression)?
                                .ReducedFromOrSelf()
                                .Equals(extensionMethodInfo.Symbol.ConstructedFrom) == true)
                            {
                                return new AnalysisResult(invocationExpression, newInvocationExpression, extensionMethodInfo);
                            }
                        }
                    }
                }
            }

            return default(AnalysisResult);
        }

        public static SyntaxNodeOrToken GetNodeOrToken(ExpressionSyntax expression)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.IdentifierName:
                    return (SimpleNameSyntax)expression;
                case SyntaxKind.GenericName:
                    return ((GenericNameSyntax)expression).Identifier;
                case SyntaxKind.SimpleMemberAccessExpression:
                    return ((MemberAccessExpressionSyntax)expression).Name;
                case SyntaxKind.MemberBindingExpression:
                    return null;
                default:
                    {
                        Debug.Assert(false, expression.Kind().ToString());
                        return null;
                    }
            }
        }

        public static Task<Document> RefactorAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            InvocationExpressionSyntax newInvocation = GetNewInvocation(invocation);

            return document.ReplaceNodeAsync(invocation, newInvocation, cancellationToken);
        }

        public static Task<Document> RefactorAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            InvocationExpressionSyntax newInvocation,
            CancellationToken cancellationToken)
        {
            return document.ReplaceNodeAsync(invocation, newInvocation, cancellationToken);
        }

        private static InvocationExpressionSyntax GetNewInvocation(InvocationExpressionSyntax invocation)
        {
            ExpressionSyntax expression = invocation.Expression;
            ArgumentListSyntax argumentList = invocation.ArgumentList;
            SeparatedSyntaxList<ArgumentSyntax> arguments = argumentList.Arguments;
            ArgumentSyntax argument = arguments.First();

            MemberAccessExpressionSyntax newMemberAccess = null;

            switch (expression.Kind())
            {
                case SyntaxKind.IdentifierName:
                    {
                        newMemberAccess = SimpleMemberAccessExpression(
                            argument.Expression.WithLeadingTrivia(expression.GetLeadingTrivia()),
                            (SimpleNameSyntax)expression.WithoutLeadingTrivia());

                        break;
                    }
                case SyntaxKind.SimpleMemberAccessExpression:
                    {
                        var memberAccess = (MemberAccessExpressionSyntax)expression;

                        newMemberAccess = memberAccess
                            .WithExpression(argument.Expression.WithTriviaFrom(memberAccess.Expression));

                        break;
                    }
                default:
                    {
                        Debug.Assert(false, expression.Kind().ToString());
                        return invocation;
                    }
            }

            return invocation
                .WithExpression(newMemberAccess)
                .WithArgumentList(argumentList.WithArguments(arguments.Remove(argument)));
        }

        public struct AnalysisResult
        {
            public AnalysisResult(
                InvocationExpressionSyntax invocationExpression,
                InvocationExpressionSyntax newInvocationExpression,
                ExtensionMethodInfo extensionMethodInfo)
            {
                if (invocationExpression == null)
                    throw new ArgumentNullException(nameof(invocationExpression));

                if (newInvocationExpression == null)
                    throw new ArgumentNullException(nameof(newInvocationExpression));

                InvocationExpression = invocationExpression;
                NewInvocationExpression = newInvocationExpression;
                ExtensionMethodInfo = extensionMethodInfo;
            }

            public InvocationExpressionSyntax InvocationExpression { get; }
            public InvocationExpressionSyntax NewInvocationExpression { get; }
            public ExtensionMethodInfo ExtensionMethodInfo { get; }

            public bool Success
            {
                get
                {
                    return InvocationExpression != null
                        && NewInvocationExpression != null;
                }
            }
        }
    }
}