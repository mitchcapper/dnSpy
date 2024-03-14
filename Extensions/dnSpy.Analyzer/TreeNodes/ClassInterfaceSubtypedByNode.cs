/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class ClassInterfaceSubtypedByNode : SearchNode {
		readonly TypeDef analyzedType;

		public ClassInterfaceSubtypedByNode(TypeDef analyzedType) {
			this.analyzedType = analyzedType ?? throw new ArgumentNullException(nameof(analyzedType));
			//isSystemObject = analyzedType.DefinitionAssembly.IsCorLib() && analyzedType.FullName == "System.Object";
			//=> this.analyzedClassOrInterface = analyzedClassOrInterface ?? throw new ArgumentNullException(nameof(analyzedClassOrInterface));
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.SubtypedByTreeNode);

		//protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {


		//	bool includeAllModules = isComType;
		//	var options = ScopedWhereUsedAnalyzerOptions.None;
		//	if (includeAllModules)
		//		options |= ScopedWhereUsedAnalyzerOptions.IncludeAllModules;
		//	if (isComType)
		//		options |= ScopedWhereUsedAnalyzerOptions.ForcePublic;
		//	var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedType, FindReferencesInType, options);
		//	return analyzer.PerformAnalysis(ct);
		//}
		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedType, FindReferencesInType);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			if (analyzedType.IsInterface) {
				if (type.HasInterfaces && type.Interfaces.Any(a => a.Interface == analyzedType))
					yield return new TypeNode(type) { Context = Context };
				yield break;
			}
			if (type.IsEnum || !type.IsClass)
				yield break;


			if (type.BaseType == analyzedType)
				yield return new TypeNode(type) { Context = Context };
		}

		public static bool CanShow(TypeDef type) => (type.IsClass || type.IsInterface) && !type.IsEnum;
	}
}
