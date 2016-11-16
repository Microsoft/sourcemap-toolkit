﻿using System;
using SourcemapToolkit.SourcemapParser;

namespace SourcemapToolkit.CallstackDeminifier
{
	/// <summary>
	/// Class responsible for deminifying a single stack frame in a minified stack trace.
	/// This method of deminification relies on a source map being available at runtime.
	/// Since source maps take up a large amount of memory, this class consumes considerably 
	/// more memory than SimpleStackFrame Deminifier during runtime.
	/// </summary>
	internal class StackFrameDeminifier : SimpleStackFrameDeminifier
	{
		private readonly ISourceMapStore _sourceMapStore;

		public StackFrameDeminifier(ISourceMapStore sourceMapStore, IFunctionMapStore functionMapStore, IFunctionMapConsumer functionMapConsumer) : base (functionMapStore, functionMapConsumer)
		{
			_sourceMapStore = sourceMapStore;
		}

		/// <summary>
		/// This method will deminify a single stack from from a minified stack trace.
		/// </summary>
		/// <returns>Returns a stack trace that has been translated to a best guess of the original source code. Any of the fields in the stack frame may be null</returns>
		public override StackFrameDeminificationResult DeminifyStackFrame(StackFrame stackFrame)
		{
			if (stackFrame == null)
			{
				throw new ArgumentNullException(nameof(stackFrame));
			}

			SourceMap sourceMap = _sourceMapStore.GetSourceMapForUrl(stackFrame.FilePath);
			SourcePosition generatedSourcePosition = stackFrame.SourcePosition;

			StackFrameDeminificationResult result = base.DeminifyStackFrame(stackFrame);

			MappingEntry generatedSourcePositionMappingEntry = sourceMap?.GetMappingEntryForGeneratedSourcePosition(generatedSourcePosition);

			if (generatedSourcePositionMappingEntry == null)
			{
				if (sourceMap == null)
				{
					result.DeminificationError |= DeminificationError.NoSourceMap;
				}
				else if (sourceMap.ParsedMappings == null)
				{
					result.DeminificationError |= DeminificationError.SourceMapFailedToParse;
				}
				else
				{
					result.DeminificationError |= DeminificationError.NoMatchingMapingInSourceMap;
				}
			}

			result.DeminifiedStackFrame.FilePath = generatedSourcePositionMappingEntry?.OriginalFileName;
			result.DeminifiedStackFrame.SourcePosition = generatedSourcePositionMappingEntry?.OriginalSourcePosition;

			return result;
		}
	}
}
