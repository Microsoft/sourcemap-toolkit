﻿using System.Collections.Generic;
using Microsoft.Ajax.Utilities;
using SourcemapToolkit.SourcemapParser;

namespace SourcemapToolkit.CallstackDeminifier
{
	/// <summary>
	/// This will visit all function nodes in the Javascript abstract 
	/// syntax tree and create an entry in the function map that describes
	/// the start and and location of the function.
	/// </summary>
	internal class FunctionFinderVisitor : TreeVisitor
	{
		private class FunctionNameInformation
		{
			public string FunctionName;
			public SourcePosition FunctionNameSourcePosition;
		}

		internal readonly List<FunctionMapEntry> FunctionMap = new List<FunctionMapEntry>();

		public override void Visit(FunctionObject node)
		{
			base.Visit(node);
			FunctionNameInformation functionNameInformation = GetFunctionNameInformation(node);

			if (functionNameInformation != null)
			{
				FunctionMapEntry functionMapEntry = new FunctionMapEntry
				{
					FunctionName = functionNameInformation.FunctionName,
					FunctionNameSourcePosition = functionNameInformation.FunctionNameSourcePosition,
					StartSourcePosition = new SourcePosition
					{
						ZeroBasedLineNumber = node.Body.Context.StartLineNumber - 1, // Souce maps work with zero based line and column numbers, the AST works with one based line numbers. We want to use zero-based everywhere.
						ZeroBasedColumnNumber = node.Body.Context.StartColumn
					},
					EndSourcePosition = new SourcePosition
					{
						ZeroBasedLineNumber = node.Body.Context.EndLineNumber - 1, // Souce maps work with zero based line and column numbers, the AST works with one based line numbers. We want to use zero-based everywhere.
						ZeroBasedColumnNumber = node.Body.Context.EndColumn
					}
				};

				FunctionMap.Add(functionMapEntry);
			}	
		}

		/// <summary>
		/// Gets the name and location information related to the function name binding for a FunctionObject node
		/// </summary>
		private FunctionNameInformation GetFunctionNameInformation(FunctionObject node)
		{
			// Gets the name of an object property that a function is bound to, like the static method foo in the example "object.foo = function () {}"
			BinaryOperator parentBinaryOperator = node.Parent as BinaryOperator;
			if (parentBinaryOperator != null)
			{
				return new FunctionNameInformation
				{
					FunctionName = parentBinaryOperator.Operand1.Context.Code,
					FunctionNameSourcePosition = new SourcePosition
					{
						ZeroBasedLineNumber = parentBinaryOperator.Operand1.Context.StartLineNumber - 1,
						ZeroBasedColumnNumber = parentBinaryOperator.Operand1.Context.StartColumn
					}
				};
			}

			// Gets the name of an object property that a function is bound to against the prototype, like the instance method foo in the example "object.prototype = {foo: function () {}}"
			ObjectLiteralProperty parentObjectLiteralProperty = node.Parent as ObjectLiteralProperty;
			if (parentObjectLiteralProperty != null)
			{
				return new FunctionNameInformation
				{
					FunctionName = parentObjectLiteralProperty.Name.Name,
					FunctionNameSourcePosition = new SourcePosition
					{
						ZeroBasedLineNumber = parentObjectLiteralProperty.Context.StartLineNumber - 1,
						ZeroBasedColumnNumber = parentObjectLiteralProperty.Context.StartColumn
					}
				};
			}

			BindingIdentifier bindingIdentifier = null;

			// Gets the name of a variable that a function is bound to, like foo in the example "var foo = function () {}"
			VariableDeclaration parentVariableDeclaration = node.Parent as VariableDeclaration;
			if (parentVariableDeclaration != null)
			{
				bindingIdentifier = parentVariableDeclaration.Binding as BindingIdentifier;
			}
			// Gets the name bound to the function, like foo in the example "function foo() {}
			else
			{
				bindingIdentifier = node.Binding;
			}

			if (bindingIdentifier != null)
			{
				return new FunctionNameInformation
				{
					FunctionName = bindingIdentifier.Name,
					FunctionNameSourcePosition = new SourcePosition
					{
						ZeroBasedLineNumber = bindingIdentifier.Context.StartLineNumber - 1,
						// Souce maps work with zero based line and column numbers, the AST works with one based line numbers. We want to use zero-based everywhere.
						ZeroBasedColumnNumber = bindingIdentifier.Context.StartColumn
					}
				};
			}

			return null;
		}
	}
}
