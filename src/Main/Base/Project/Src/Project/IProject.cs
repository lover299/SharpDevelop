﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

using ICSharpCode.Core;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Parser;
using ICSharpCode.SharpDevelop.Refactoring;

namespace ICSharpCode.SharpDevelop.Project
{
	/// <summary>
	/// Base interface for projects.
	/// Thread-safe members lock on the SyncRoot. Non-thread-safe members may only be called from the main thread.
	/// 
	/// When you implement IProject, you should also implement IProjectItemListProvider and IProjectAllowChangeConfigurations
	/// </summary>
	public interface IProject
		: IBuildable, ISolutionFolder, IDisposable, IMementoCapable
	{
		/// <summary>
		/// Gets the list of items in the project. This member is thread-safe.
		/// The returned collection is guaranteed not to change - adding new items or removing existing items
		/// will create a new collection.
		/// </summary>
		IReadOnlyCollection<ProjectItem> Items {
			get;
		}
		
		/// <summary>
		/// Gets all items in the project that have the specified item type.
		/// This member is thread-safe.
		/// </summary>
		IEnumerable<ProjectItem> GetItemsOfType(ItemType type);
		
		/// <summary>
		/// Gets the default item type the specified file should have.
		/// </summary>
		/// <param name="fileName">The full path to the file to determine the item type for</param>
		ItemType GetDefaultItemType(string fileName);
		
		/// <summary>
		/// Gets the list of available file item types. This member is thread-safe.
		/// </summary>
		IReadOnlyCollection<ItemType> AvailableFileItemTypes {
			get;
		}
		
		/// <summary>
		/// Gets a list of project sections stored in the solution file for this project.
		/// </summary>
		List<ProjectSection> ProjectSections {
			get;
		}
		
		/// <summary>
		/// Gets the name of the project file.
		/// (Full file name, example: @"D:\Serralongue\SharpDevelop\samples\CustomPad\CustomPad.csproj")
		/// 
		/// Only the getter is thread-safe.
		/// </summary>
		FileName FileName {
			get;
			set;
		}
		
		/// <summary>
		/// Gets/Sets the name of the project.
		/// 
		/// Only the getter is thread-safe.
		/// </summary>
		/// <remarks>
		/// Name already exists in ISolutionFolder, it's repeated here to prevent
		/// the ambiguity with IBuildable.Name.
		/// </remarks>
		new string Name {
			get;
			set;
		}
		
		/// <summary>
		/// Gets the directory of the project file.
		/// This is equivalent to Path.GetDirectoryName(project.FileName);
		/// (Example: @"D:\Serralongue\SharpDevelop\samples\CustomPad")
		/// 
		/// This member is thread-safe.
		/// </summary>
		string Directory {
			get;
		}
		
		/// <summary>
		/// <para>
		/// True if the project is readonly. For project based files this means
		/// the project file has the readonly attribute set. For solution folder
		/// based projects this means that the sln file containing the project
		/// has the readonly attribute set.
		/// </para>
		/// <para>This member is thread-safe.</para>
		/// </summary>
		bool ReadOnly {
			get;
		}
		
		#region MSBuild properties used inside SharpDevelop base
		/// <summary>
		/// Gets/Sets the assembly name of the assembly created when building this project.
		/// Equivalent to MSBuild property "AssemblyName".
		/// </summary>
		string AssemblyName {
			get;
			set;
		}
		
		/// <summary>
		/// Gets/Sets the root namespace of the project.
		/// </summary>
		string RootNamespace {
			get;
			set;
		}
		
		/// <summary>
		/// Gets the full path of the output assembly.
		/// Returns null when the project does not output any assembly.
		/// </summary>
		string OutputAssemblyFullPath {
			get;
		}
		
		/// <summary>
		/// Gets the name of the language binding used for the project.
		/// </summary>
		string Language {
			get;
		}
		
		/// <summary>
		/// Gets the name of the directory being the "Properties" folder of the application,
		/// relative to the project directory.
		/// This folder gets a node type in the project browser.
		/// Equivalent to MSBuild property "AppDesignerFolder".
		/// </summary>
		string AppDesignerFolder {
			get;
		}
		#endregion
		
		#region Configuration / Platform management
		/// <summary>
		/// Gets/Sets the active configuration.
		/// </summary>
		string ActiveConfiguration {
			get;
			set;
		}
		
		/// <summary>
		/// Gets/Sets the active platform.
		/// </summary>
		string ActivePlatform {
			get;
			set;
		}
		/// <summary>
		/// Gets the list of available configuration names.
		/// </summary>
		IReadOnlyCollection<string> ConfigurationNames { get; }
		
		/// <summary>
		/// Gets the list of available platform names.
		/// </summary>
		IReadOnlyCollection<string> PlatformNames { get; }
		
		/// <summary>
		/// Is raised after the ActiveConfiguration property has changed.
		/// </summary>
		event EventHandler ActiveConfigurationChanged;
		
		/// <summary>
		/// Is raised after the ActivePlatform property has changed.
		/// </summary>
		event EventHandler ActivePlatformChanged;
		#endregion
		
		/// <summary>
		/// Saves the project using its current file name.
		/// </summary>
		void Save();
		
		/// <summary>
		/// Returns true, if a specific file (given by its name) is inside this project.
		/// This member is thread-safe.
		/// </summary>
		/// <param name="fileName">The <b>fully qualified</b> file name of the file</param>
		bool IsFileInProject(string fileName);
		
		/// <summary>
		/// Returns the project item for a specific file; or null if the file is not found in the project.
		/// This member is thread-safe.
		/// </summary>
		/// <param name="fileName">The <b>fully qualified</b> file name of the file</param>
		FileProjectItem FindFile(string fileName);
		
		/// <summary>
		/// Gets if the project can be started.
		/// </summary>
		bool IsStartable { get; }
		
		/// <summary>
		/// Gets project specific properties.
		/// These are saved in as part of the SharpDevelop configuration in the AppData folder.
		/// </summary>
		/// <remarks>This property never returns null.</remarks>
		Properties ProjectSpecificProperties { get; }
		
		/// <summary>
		/// Starts the project.
		/// </summary>
		/// <param name="withDebugging">True, if a debugger should be used for the project.</param>
		void Start(bool withDebugging);
		
		/// <summary>
		/// Creates a new ProjectItem for the passed MSBuild item.
		/// </summary>
		ProjectItem CreateProjectItem(IProjectItemBackendStore item);
		
		/// <summary>
		/// Gets the minimum version the solution must have to support this project type.
		/// </summary>
		int MinimumSolutionVersion { get; }
		
		/// <summary>
		/// Resolves assembly references for this project.
		/// The resulting list of resolved references will include project references.
		/// </summary>
		IEnumerable<ReferenceProjectItem> ResolveAssemblyReferences(CancellationToken cancellationToken);
		
		/// <summary>
		/// Notifies the project that it was succesfully created from a project template.
		/// </summary>
		void ProjectCreationComplete();
		
		/// <summary>
		/// Loads the project extension content with the specified name.
		/// </summary>
		XElement LoadProjectExtensions(string name);
		
		/// <summary>
		/// Saves the project extension content with the specified name.
		/// </summary>
		void SaveProjectExtensions(string name, XElement element);
		
		// TODO:
		bool HasProjectType(Guid projectTypeGuid);
		
		/// <summary>
		/// Gets the project content associated with this project.
		/// </summary>
		/// <remarks>
		/// This property must always return the same value for the same project.
		/// This property may return null.
		/// 
		/// This member is thread-safe.
		/// </remarks>
		IProjectContent ProjectContent { get; }
		
		/// <summary>
		/// Gets the default namespace to use for a file with the specified name.
		/// </summary>
		/// <param name="fileName">Full file name for a new file being added to the project.</param>
		/// <returns>Namespace name to use for the new file</returns>
		string GetDefaultNamespace(string fileName);
		
		/// <summary>
		/// Creates a CodeDomProvider for this project's language.
		/// Returns null when no CodeDomProvider is available for the language.
		/// </summary>
		System.CodeDom.Compiler.CodeDomProvider CreateCodeDomProvider();
		
		/// <summary>
		/// Generates code for a CodeDom compile unit.
		/// This method is used by CustomToolContext.WriteCodeDomToFile.
		/// </summary>
		void GenerateCodeFromCodeDom(System.CodeDom.CodeCompileUnit compileUnit, TextWriter writer);
		
		/// <summary>
		/// Creates a new ambience for this project.
		/// </summary>
		/// <remarks>
		/// This member is thread-safe.
		/// As ambiences are not thread-safe, this method always returns a new ambience instance.
		/// Never returns null.
		/// </remarks>
		IAmbience GetAmbience();
		
		/// <summary>
		/// Returns the ICodeGenerator implementation for this project.
		/// </summary>
		ICodeGenerator CodeGenerator { get; }
		
		/// <summary>
		/// Prepares searching for references to the specified entity.
		/// This method should calculate the amount of work to be done (e.g. using the number of files to search through),
		/// it should not perform the actual search.
		/// </summary>
		/// <returns>
		/// An object that can be used to perform the search; or null if this project does not support symbol searches.
		/// </returns>
		Refactoring.ISymbolSearch PrepareSymbolSearch(IEntity entity);
		
		/// <summary>
		/// Occurs whenever parse information for this project was updated. This event is raised on the main thread.
		/// </summary>
		event EventHandler<ParseInformationEventArgs> ParseInformationUpdated;
		
		/// <summary>
		/// Notifies the project that the parse information was updated.
		/// This method is called by the parser service <b>within a per-file lock</b>.
		/// </summary>
		void OnParseInformationUpdated(ParseInformationEventArgs args);
		
		/// <summary>
		/// Gets the models for the top-level type definitions in this project.
		/// Never returns null, but may return a permanently empty collection if this project does not support such models.
		/// </summary>
		ITypeDefinitionModelCollection TypeDefinitionModels { get; }
		
		/// <summary>
		/// Gets the parent solution.
		/// This property is thread-safe.
		/// </summary>
		Solution ParentSolution { get; }
	}
	
	/// <summary>
	/// Interface for adding and removing items from a project. Not part of the IProject
	/// interface because in nearly all cases, ProjectService.Add/RemoveProjectItem should
	/// be used instead!
	/// So IProject implementors should implement this interface, but only the SharpDevelop methods
	/// ProjectService.AddProjectItem and RemoveProjectItem may call the interface members.
	/// </summary>
	public interface IProjectItemListProvider
	{
		/// <summary>
		/// Gets a list of items in the project.
		/// </summary>
		IReadOnlyCollection<ProjectItem> Items {
			get;
		}
		
		/// <summary>
		/// Adds a new entry to the Items-collection
		/// </summary>
		void AddProjectItem(ProjectItem item);
		
		/// <summary>
		/// Removes an entry from the Items-collection
		/// </summary>
		bool RemoveProjectItem(ProjectItem item);
	}
	
	/// <summary>
	/// Interface for changing project or solution configuration.
	/// IProject implementors should implement this interface, but only the SharpDevelop methods
	/// Solution.RenameProjectPlatform etc. may call the interface members.
	/// </summary>
	public interface IProjectAllowChangeConfigurations
	{
		bool RenameProjectConfiguration(string oldName, string newName);
		bool RenameProjectPlatform(string oldName, string newName);
		bool AddProjectConfiguration(string newName, string copyFrom);
		bool AddProjectPlatform(string newName, string copyFrom);
		bool RemoveProjectConfiguration(string name);
		bool RemoveProjectPlatform(string name);
	}
}
