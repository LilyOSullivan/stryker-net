using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Helpers;

namespace Stryker.Core.Instrumentation
{
    /// <summary>
    /// Injects 'return default(...)' statement at the end of a method
    /// </summary>
    internal class EndingReturnEngine: BaseEngine<BlockSyntax>
    {
        public EndingReturnEngine(string markerId) : base(markerId)
        {
        }

        public BlockSyntax InjectReturn(BlockSyntax block, TypeSyntax type, SyntaxTokenList modifiers)
        {
            var returnType = type;
            BlockSyntax ret;
            // if we had no body or the the last statement was a return, no need to add one, or this is an iterator method
            if (block == null
                || returnType == null
                || block.Statements.Count == 0
                || block!.Statements.Last().Kind() == SyntaxKind.ReturnStatement
                || returnType.IsVoid()
                || block.ContainsNodeThatVerifies(x => x.IsKind(SyntaxKind.YieldReturnStatement)|| x.IsKind(SyntaxKind.YieldBreakStatement), false))
            {
                ret = null;
            }
            else
            {
                var genericReturn = returnType.DescendantNodesAndSelf().OfType<GenericNameSyntax>().FirstOrDefault();
                if (modifiers.Any(x => x.IsKind(SyntaxKind.AsyncKeyword)))
                {
                    returnType = genericReturn?.TypeArgumentList.Arguments.First();
                }

                if (returnType != null)
                {
                    ret = block.AddStatements(
                        SyntaxFactory.ReturnStatement(returnType.BuildDefaultExpression()));
                }
                else
                {
                    ret = null;
                }
            }

            return ret == null ? block : ret.WithAdditionalAnnotations(Marker);
        }

        protected override SyntaxNode Revert(BlockSyntax node)
        {
            if (node?.Statements.Last().IsKind(SyntaxKind.ReturnStatement) != true)
            {
                throw new InvalidOperationException($"No return at the end of: {node}");
            }

            return node.WithStatements(node.Statements.Remove(node.Statements.Last())).WithoutAnnotations(Marker);
        }
    }
}
